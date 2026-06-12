using System.Buffers.Binary;

namespace L2Viewer.UtxFile;

public static class TextureCodec
{
    private static readonly Dictionary<string, PackageData?> PackageCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, TextureData?> TextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object CacheLock = new();
    public sealed record TextureLoadResult(bool ExportFound, TextureData? Texture, string? FailureReason);
    private sealed record ParsedTextureProperties(
        int EndOffset,
        int? Format,
        int? Width,
        int? Height,
        (string ClassName, string ObjectName, string? PackageName)? PaletteRef);
    private sealed record ParsedMip(byte[] Data, int Width, int Height, byte UBits, byte VBits);

    public static TextureMeta ExtractTextureMeta(byte[] blob, IReadOnlyList<string> names)
    {
        int? format = null;
        int? width = null;
        int? height = null;
        var pos = 0;
        try
        {
            for (var i = 0; i < 64; i++)
            {
                if (!PackageReader.TryReadPropertyTag(blob, names, ref pos, out var tag))
                {
                    break;
                }

                if (tag.IsEnd)
                {
                    break;
                }

                if (pos + tag.DataSize > blob.Length)
                {
                    break;
                }

                var payload = blob.AsSpan(pos, tag.DataSize);
                pos += tag.DataSize;

                if (tag.Name == "Format" && tag.DataSize >= 1)
                {
                    format = tag.DataSize >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(payload[..4]) : payload[0];
                }
                else if (tag.Name == "USize" && tag.DataSize == 4)
                {
                    width = BinaryPrimitives.ReadInt32LittleEndian(payload);
                }
                else if (tag.Name == "VSize" && tag.DataSize == 4)
                {
                    height = BinaryPrimitives.ReadInt32LittleEndian(payload);
                }
            }
        }
        catch
        {
        }

        return new TextureMeta(format, width, height);
    }

    public static TextureData? DecodeTextureExport(PackageData package, int exportIndex)
    {
        if (exportIndex < 0 || exportIndex >= package.Exports.Count)
        {
            return null;
        }

        var exportEntry = package.Exports[exportIndex];
        if (!string.Equals(PackageReader.ExportClassName(package, exportEntry), "Texture", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var blob = PackageReader.ReadExportBlob(package, exportEntry);
        var parsed = ParseTextureExportStrict(package, blob);
        if (parsed is null)
        {
            return null;
        }

        var palette = ResolvePalette(package, parsed.Value.PaletteRef);
        var pixels = DecodeStrictTexturePixels(parsed.Value.Format, parsed.Value.TopMip, palette);
        if (pixels is null)
        {
            return null;
        }

        return new TextureData(
            PackageReader.SafeName(package.Names, exportEntry.ObjectName),
            parsed.Value.TopMip.Width,
            parsed.Value.TopMip.Height,
            pixels,
            package.Path,
            $"strict mip parser ({parsed.Value.FormatName})");
    }

    public static TextureData? LoadTextureFromPackage(string packagePath, string textureName)
    {
        var cacheKey = $"{packagePath}|{textureName}";
        lock (CacheLock)
        {
            if (TextureCache.TryGetValue(cacheKey, out var cachedTexture))
            {
                return cachedTexture;
            }
        }

        try
        {
            var probe = ProbeTextureFromPackage(packagePath, textureName);
            lock (CacheLock)
            {
                TextureCache[cacheKey] = probe.Texture;
            }

            return probe.Texture;
        }
        catch
        {
        }

        lock (CacheLock)
        {
            TextureCache[cacheKey] = null;
        }

        return null;
    }

    public static TextureLoadResult ProbeTextureFromPackage(string packagePath, string textureName)
    {
        var package = LoadPackageCached(packagePath);
        var exportIndex = FindTextureExportIndex(package, textureName);
        if (exportIndex < 0)
        {
            return new TextureLoadResult(
                ExportFound: false,
                Texture: null,
                FailureReason: $"Texture export '{textureName}' was not found.");
        }

        var decoded = DecodeTextureExport(package, exportIndex);
        if (decoded is null)
        {
            return new TextureLoadResult(
                ExportFound: true,
                Texture: null,
                FailureReason: $"Texture export '{textureName}' exists but could not be decoded.");
        }

        return new TextureLoadResult(ExportFound: true, Texture: decoded, FailureReason: null);
    }

    private static (ParsedMip TopMip, int Format, string FormatName, (string ClassName, string ObjectName, string? PackageName)? PaletteRef)? ParseTextureExportStrict(
        PackageData package,
        byte[] blob)
    {
        var props = ParseTextureProperties(package, blob);
        if (props is null)
        {
            return null;
        }

        var format = ResolveTextureFormat(props);
        if (format is null)
        {
            return null;
        }

        var textureDataOffset = AdjustTextureDataOffset(package, blob, props.EndOffset);
        var mips = ParseTextureMips(package, blob, textureDataOffset);
        if (mips.Count == 0)
        {
            return null;
        }

        ParsedMip? topMip = null;
        foreach (var mip in mips)
        {
            if (mip.Data.Length == 0)
            {
                continue;
            }

            topMip = mip;
            break;
        }

        if (topMip is null)
        {
            return null;
        }

        if (props.Width is > 0 && topMip.Width != props.Width.Value)
        {
            return null;
        }

        if (props.Height is > 0 && topMip.Height != props.Height.Value)
        {
            return null;
        }

        return (topMip, format.Value.Code, format.Value.Name, props.PaletteRef);
    }

    private static int AdjustTextureDataOffset(PackageData package, byte[] blob, int offset)
    {
        var pos = offset;

        // Lineage 2 packages include extra UMaterial::Serialize data before UTexture::Mips.
        if (package.IsVersionAtLeast(L2PackageVersion.Ver123) &&
            package.IsLicenseeVersionAtLeast(L2LicenseeVersion.Ver16) &&
            package.Header.LicenseeVersion < 37)
        {
            pos += 4;
        }

        return Math.Min(pos, blob.Length);
    }

    private static ParsedTextureProperties? ParseTextureProperties(PackageData package, byte[] blob)
    {
        var pos = 0;
        int? format = null;
        int? width = null;
        int? height = null;
        (string ClassName, string ObjectName, string? PackageName)? paletteRef = null;

        try
        {
            for (var i = 0; i < 128; i++)
            {
                if (!PackageReader.TryReadPropertyTag(blob, package.Names, ref pos, out var tag))
                {
                    return null;
                }

                if (tag.IsEnd)
                {
                    return new ParsedTextureProperties(pos, format, width, height, paletteRef);
                }

                if (pos + tag.DataSize > blob.Length)
                {
                    return null;
                }

                var payload = blob.AsSpan(pos, tag.DataSize);
                pos += tag.DataSize;
                if (tag.Name == "Format" && tag.DataSize >= 1)
                {
                    format = tag.DataSize >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(payload[..4]) : payload[0];
                }
                else if (tag.Name == "Palette")
                {
                    var payloadPos = 0;
                    try
                    {
                        if (PackageReader.TryReadCompactIndex(payload, ref payloadPos, out var objectRef))
                        {
                            paletteRef = UnrPackageHelpers.ResolveObjectRef(package, objectRef);
                        }
                    }
                    catch
                    {
                    }
                }
                else if (tag.Name == "USize" && tag.DataSize == 4)
                {
                    width = BinaryPrimitives.ReadInt32LittleEndian(payload);
                }
                else if (tag.Name == "VSize" && tag.DataSize == 4)
                {
                    height = BinaryPrimitives.ReadInt32LittleEndian(payload);
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static List<ParsedMip> ParseTextureMips(PackageData package, byte[] blob, int offset)
    {
        var pos = offset;
        var mips = new List<ParsedMip>();
        try
        {
            if (!PackageReader.TryReadCompactIndex(blob, ref pos, out var mipCount) || mipCount < 0 || mipCount > 64)
            {
                return mips;
            }

            int prevWidth = int.MaxValue;
            int prevHeight = int.MaxValue;
            for (var i = 0; i < mipCount; i++)
            {
                if (package.Header.Version > (ushort)L2PackageVersion.Ver61)
                {
                    _ = ReadInt32(blob, ref pos);
                }

                if (!PackageReader.TryReadCompactIndex(blob, ref pos, out var dataCount) || dataCount < 0 || pos + dataCount > blob.Length)
                {
                    return [];
                }

                var data = blob.AsSpan(pos, dataCount).ToArray();
                pos += dataCount;
                var width = ReadInt32(blob, ref pos);
                var height = ReadInt32(blob, ref pos);
                var uBits = ReadByte(blob, ref pos);
                var vBits = ReadByte(blob, ref pos);

                if (!IsPowerOfTwo(width) || !IsPowerOfTwo(height) || width < 1 || height < 1 || width > 4096 || height > 4096)
                {
                    return [];
                }

                if (uBits != IntegerLog2(width) || vBits != IntegerLog2(height))
                {
                    return [];
                }

                if (i > 0)
                {
                    if (width > prevWidth || height > prevHeight)
                    {
                        return [];
                    }
                }

                prevWidth = width;
                prevHeight = height;
                mips.Add(new ParsedMip(data, width, height, uBits, vBits));
            }
        }
        catch
        {
            return [];
        }

        return mips;
    }

    private static byte[]? DecodeStrictTexturePixels(int format, ParsedMip mip, IReadOnlyList<byte[]>? palette)
    {
        return format switch
        {
            0 => DecodeP8(mip.Data, mip.Width, mip.Height, palette),
            3 => DecodeDxt1(mip.Data, mip.Width, mip.Height),
            7 => DecodeDxt3(mip.Data, mip.Width, mip.Height),
            8 => DecodeDxt5(mip.Data, mip.Width, mip.Height),
            5 => DecodeBgra8(mip.Data, mip.Width, mip.Height),
            9 => DecodeG8(mip.Data, mip.Width, mip.Height),
            10 => DecodeG16(mip.Data, mip.Width, mip.Height),
            4 => DecodeRgb8(mip.Data, mip.Width, mip.Height),
            _ => null
        };
    }

    private static (int Code, string Name)? MapTextureFormat(int? format)
    {
        return format switch
        {
            0 => (0, "P8"),
            3 => (3, "DXT1"),
            5 => (5, "BGRA8"),
            7 => (7, "DXT3"),
            8 => (8, "DXT5"),
            9 => (9, "G8"),
            10 => (10, "G16"),
            4 => (4, "RGB8"),
            _ => null
        };
    }

    private static (int Code, string Name)? ResolveTextureFormat(ParsedTextureProperties props)
    {
        var explicitFormat = MapTextureFormat(props.Format);
        if (explicitFormat is not null)
        {
            return explicitFormat;
        }

        if (props.Format is null && props.PaletteRef is not null)
        {
            return (0, "P8");
        }

        return null;
    }

    private static int FindTextureExportIndex(PackageData package, string textureName)
    {
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var exportEntry = package.Exports[i];
            if (!string.Equals(PackageReader.ExportClassName(package, exportEntry), "Texture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(PackageReader.SafeName(package.Names, exportEntry.ObjectName), textureName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static byte[] DecodeDxt1(ReadOnlySpan<byte> data, int width, int height)
    {
        var bw = Math.Max(1, (width + 3) / 4);
        var bh = Math.Max(1, (height + 3) / 4);
        var need = bw * bh * 8;
        if (data.Length < need)
        {
            throw new PackageReadException("DXT1 payload is too short.");
        }

        var output = new byte[width * height * 4];
        var p = 0;
        for (var by = 0; by < bh; by++)
        {
            for (var bx = 0; bx < bw; bx++)
            {
                var c0 = BinaryPrimitives.ReadUInt16LittleEndian(data[p..]);
                var c1 = BinaryPrimitives.ReadUInt16LittleEndian(data[(p + 2)..]);
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(data[(p + 4)..]);
                p += 8;

                var c0Rgba = Rgb565ToRgba(c0);
                var c1Rgba = Rgb565ToRgba(c1);
                var palette = new byte[16];
                CopyColor(c0Rgba, palette, 0);
                CopyColor(c1Rgba, palette, 4);
                if (c0 > c1)
                {
                    WriteColor(palette, 8,
                        (byte)((2 * c0Rgba[0] + c1Rgba[0]) / 3),
                        (byte)((2 * c0Rgba[1] + c1Rgba[1]) / 3),
                        (byte)((2 * c0Rgba[2] + c1Rgba[2]) / 3),
                        255);
                    WriteColor(palette, 12,
                        (byte)((c0Rgba[0] + 2 * c1Rgba[0]) / 3),
                        (byte)((c0Rgba[1] + 2 * c1Rgba[1]) / 3),
                        (byte)((c0Rgba[2] + 2 * c1Rgba[2]) / 3),
                        255);
                }
                else
                {
                    WriteColor(palette, 8,
                        (byte)((c0Rgba[0] + c1Rgba[0]) / 2),
                        (byte)((c0Rgba[1] + c1Rgba[1]) / 2),
                        (byte)((c0Rgba[2] + c1Rgba[2]) / 2),
                        255);
                    WriteColor(palette, 12, 0, 0, 0, 0);
                }

                for (var py = 0; py < 4; py++)
                {
                    var y = by * 4 + py;
                    if (y >= height)
                    {
                        continue;
                    }

                    for (var px = 0; px < 4; px++)
                    {
                        var x = bx * 4 + px;
                        if (x >= width)
                        {
                            continue;
                        }

                        var idx = (int)((bits >> (2 * (py * 4 + px))) & 0x03);
                        var dst = (y * width + x) * 4;
                        var src = idx * 4;
                        Buffer.BlockCopy(palette, src, output, dst, 4);
                    }
                }
            }
        }

        return output;
    }

    private static byte[] DecodeP8(ReadOnlySpan<byte> data, int width, int height, IReadOnlyList<byte[]>? palette)
    {
        var need = width * height;
        if (data.Length < need)
        {
            throw new PackageReadException("P8 payload is too short.");
        }

        if (palette is null || palette.Count < 256)
        {
            throw new PackageReadException("P8 texture palette is missing.");
        }

        var output = new byte[width * height * 4];
        for (var i = 0; i < need; i++)
        {
            var color = palette[data[i]];
            var dst = i * 4;
            output[dst + 0] = color[0];
            output[dst + 1] = color[1];
            output[dst + 2] = color[2];
            output[dst + 3] = color[3];
        }

        return output;
    }

    private static byte[] DecodeDxt5(ReadOnlySpan<byte> data, int width, int height)
    {
        var bw = Math.Max(1, (width + 3) / 4);
        var bh = Math.Max(1, (height + 3) / 4);
        var need = bw * bh * 16;
        if (data.Length < need)
        {
            throw new PackageReadException("DXT5 payload is too short.");
        }

        var output = new byte[width * height * 4];
        var p = 0;
        for (var by = 0; by < bh; by++)
        {
            for (var bx = 0; bx < bw; bx++)
            {
                var a0 = data[p];
                var a1 = data[p + 1];
                ulong alphaBits = 0;
                for (var i = 0; i < 6; i++)
                {
                    alphaBits |= (ulong)data[p + 2 + i] << (8 * i);
                }
                p += 8;

                var alphaTable = BuildAlphaTable(a0, a1);
                var c0 = BinaryPrimitives.ReadUInt16LittleEndian(data[p..]);
                var c1 = BinaryPrimitives.ReadUInt16LittleEndian(data[(p + 2)..]);
                var colorBits = BinaryPrimitives.ReadUInt32LittleEndian(data[(p + 4)..]);
                p += 8;

                var c0Rgb = Rgb565ToRgb(c0);
                var c1Rgb = Rgb565ToRgb(c1);
                var palette = new (byte R, byte G, byte B)[4];
                palette[0] = c0Rgb;
                palette[1] = c1Rgb;
                palette[2] = ((byte)((2 * c0Rgb.R + c1Rgb.R) / 3), (byte)((2 * c0Rgb.G + c1Rgb.G) / 3), (byte)((2 * c0Rgb.B + c1Rgb.B) / 3));
                palette[3] = ((byte)((c0Rgb.R + 2 * c1Rgb.R) / 3), (byte)((c0Rgb.G + 2 * c1Rgb.G) / 3), (byte)((c0Rgb.B + 2 * c1Rgb.B) / 3));

                for (var py = 0; py < 4; py++)
                {
                    var y = by * 4 + py;
                    if (y >= height)
                    {
                        continue;
                    }

                    for (var px = 0; px < 4; px++)
                    {
                        var x = bx * 4 + px;
                        if (x >= width)
                        {
                            continue;
                        }

                        var i = py * 4 + px;
                        var cidx = (int)((colorBits >> (2 * i)) & 0x03);
                        var aidx = (int)((alphaBits >> (3 * i)) & 0x07);
                        var dst = (y * width + x) * 4;
                        output[dst + 0] = palette[cidx].R;
                        output[dst + 1] = palette[cidx].G;
                        output[dst + 2] = palette[cidx].B;
                        output[dst + 3] = alphaTable[aidx];
                    }
                }
            }
        }

        return output;
    }

    private static byte[] DecodeDxt3(ReadOnlySpan<byte> data, int width, int height)
    {
        var bw = Math.Max(1, (width + 3) / 4);
        var bh = Math.Max(1, (height + 3) / 4);
        var need = bw * bh * 16;
        if (data.Length < need)
        {
            throw new PackageReadException("DXT3 payload is too short.");
        }

        var output = new byte[width * height * 4];
        var p = 0;
        for (var by = 0; by < bh; by++)
        {
            for (var bx = 0; bx < bw; bx++)
            {
                ulong alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(data[p..]);
                p += 8;
                var c0 = BinaryPrimitives.ReadUInt16LittleEndian(data[p..]);
                var c1 = BinaryPrimitives.ReadUInt16LittleEndian(data[(p + 2)..]);
                var colorBits = BinaryPrimitives.ReadUInt32LittleEndian(data[(p + 4)..]);
                p += 8;

                var c0Rgb = Rgb565ToRgb(c0);
                var c1Rgb = Rgb565ToRgb(c1);
                var palette = new (byte R, byte G, byte B)[4];
                palette[0] = c0Rgb;
                palette[1] = c1Rgb;
                palette[2] = ((byte)((2 * c0Rgb.R + c1Rgb.R) / 3), (byte)((2 * c0Rgb.G + c1Rgb.G) / 3), (byte)((2 * c0Rgb.B + c1Rgb.B) / 3));
                palette[3] = ((byte)((c0Rgb.R + 2 * c1Rgb.R) / 3), (byte)((c0Rgb.G + 2 * c1Rgb.G) / 3), (byte)((c0Rgb.B + 2 * c1Rgb.B) / 3));

                for (var py = 0; py < 4; py++)
                {
                    var y = by * 4 + py;
                    if (y >= height)
                    {
                        continue;
                    }

                    for (var px = 0; px < 4; px++)
                    {
                        var x = bx * 4 + px;
                        if (x >= width)
                        {
                            continue;
                        }

                        var i = py * 4 + px;
                        var cidx = (int)((colorBits >> (2 * i)) & 0x03);
                        var alpha4 = (byte)((alphaBits >> (4 * i)) & 0x0F);
                        var alpha8 = (byte)(alpha4 * 17);
                        var dst = (y * width + x) * 4;
                        output[dst + 0] = palette[cidx].R;
                        output[dst + 1] = palette[cidx].G;
                        output[dst + 2] = palette[cidx].B;
                        output[dst + 3] = alpha8;
                    }
                }
            }
        }

        return output;
    }

    private static byte[] DecodeBgra8(ReadOnlySpan<byte> data, int width, int height)
    {
        var need = width * height * 4;
        if (data.Length < need)
        {
            throw new PackageReadException("BGRA8 payload is too short.");
        }

        var output = new byte[need];
        for (var i = 0; i < width * height; i++)
        {
            var src = i * 4;
            output[src + 0] = data[src + 2];
            output[src + 1] = data[src + 1];
            output[src + 2] = data[src + 0];
            output[src + 3] = data[src + 3];
        }

        return output;
    }

    private static byte[] DecodeRgb8(ReadOnlySpan<byte> data, int width, int height)
    {
        var need = width * height * 3;
        if (data.Length < need)
        {
            throw new PackageReadException("RGB8 payload is too short.");
        }

        var output = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            var src = i * 3;
            var dst = i * 4;
            output[dst + 0] = data[src + 2];
            output[dst + 1] = data[src + 1];
            output[dst + 2] = data[src + 0];
            output[dst + 3] = 255;
        }

        return output;
    }

    private static byte[] DecodeG8(ReadOnlySpan<byte> data, int width, int height)
    {
        var need = width * height;
        if (data.Length < need)
        {
            throw new PackageReadException("G8 payload is too short.");
        }

        var output = new byte[width * height * 4];
        for (var i = 0; i < need; i++)
        {
            var value = data[i];
            var dst = i * 4;
            output[dst + 0] = value;
            output[dst + 1] = value;
            output[dst + 2] = value;
            output[dst + 3] = 255;
        }

        return output;
    }

    private static byte[] DecodeG16(ReadOnlySpan<byte> data, int width, int height)
    {
        var need = width * height * 2;
        if (data.Length < need)
        {
            throw new PackageReadException("G16 payload is too short.");
        }

        var output = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            var src = i * 2;
            var dst = i * 4;
            
            // In G16, the data is 16-bit. Since we currently render the preview and use ChannelValue
            // from 8-bit R/G/B/A channels, let's map the high byte (or full range) into RGB
            // so that luma extraction uses the high 8-bits as the primary heightmap detail.
            // Using the high byte ensures we keep the macroscopic terrain shape.
            var high = data[src + 1];

            output[dst + 0] = high;
            output[dst + 1] = high;
            output[dst + 2] = high;
            output[dst + 3] = 255;
        }

        return output;
    }

    private static byte[] BuildAlphaTable(byte a0, byte a1)
    {
        var alphas = new byte[8];
        alphas[0] = a0;
        alphas[1] = a1;
        if (a0 > a1)
        {
            alphas[2] = (byte)((6 * a0 + a1) / 7);
            alphas[3] = (byte)((5 * a0 + 2 * a1) / 7);
            alphas[4] = (byte)((4 * a0 + 3 * a1) / 7);
            alphas[5] = (byte)((3 * a0 + 4 * a1) / 7);
            alphas[6] = (byte)((2 * a0 + 5 * a1) / 7);
            alphas[7] = (byte)((a0 + 6 * a1) / 7);
        }
        else
        {
            alphas[2] = (byte)((4 * a0 + a1) / 5);
            alphas[3] = (byte)((3 * a0 + 2 * a1) / 5);
            alphas[4] = (byte)((2 * a0 + 3 * a1) / 5);
            alphas[5] = (byte)((a0 + 4 * a1) / 5);
            alphas[6] = 0;
            alphas[7] = 255;
        }

        return alphas;
    }

    private static byte[] Rgb565ToRgba(ushort c)
    {
        var rgb = Rgb565ToRgb(c);
        return new[] { rgb.R, rgb.G, rgb.B, (byte)255 };
    }

    private static (byte R, byte G, byte B) Rgb565ToRgb(ushort c)
    {
        return (
            (byte)(((c >> 11) & 0x1F) * 255 / 31),
            (byte)(((c >> 5) & 0x3F) * 255 / 63),
            (byte)((c & 0x1F) * 255 / 31));
    }

    private static void CopyColor(byte[] color, byte[] palette, int offset)
    {
        Buffer.BlockCopy(color, 0, palette, offset, 4);
    }

    private static void WriteColor(byte[] palette, int offset, byte r, byte g, byte b, byte a)
    {
        palette[offset + 0] = r;
        palette[offset + 1] = g;
        palette[offset + 2] = b;
        palette[offset + 3] = a;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    private static IReadOnlyList<byte[]>? ResolvePalette(
        PackageData package,
        (string ClassName, string ObjectName, string? PackageName)? paletteRef)
    {
        if (paletteRef is null || !string.Equals(paletteRef.Value.ClassName, "Palette", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var targetPackage = package;
        if (!string.IsNullOrWhiteSpace(paletteRef.Value.PackageName) &&
            !string.Equals(paletteRef.Value.PackageName, Path.GetFileNameWithoutExtension(package.Path), StringComparison.OrdinalIgnoreCase))
        {
            var packagePath = FindSiblingPackage(package.Path, paletteRef.Value.PackageName, ".utx");
            if (packagePath is null)
            {
                return null;
            }

            targetPackage = LoadPackageCached(packagePath);
        }

        return DecodePaletteExport(targetPackage, paletteRef.Value.ObjectName);
    }

    private static IReadOnlyList<byte[]>? DecodePaletteExport(PackageData package, string paletteName)
    {
        for (var i = 0; i < package.Exports.Count; i++)
        {
            var exportEntry = package.Exports[i];
            if (!string.Equals(PackageReader.ExportClassName(package, exportEntry), "Palette", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(PackageReader.SafeName(package.Names, exportEntry.ObjectName), paletteName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var blob = PackageReader.ReadExportBlob(package, exportEntry);
            var props = ParsePropertiesEnd(package, blob);
            if (props is null)
            {
                return null;
            }

            var pos = props.Value;
            if (!PackageReader.TryReadCompactIndex(blob, ref pos, out var count) || count != 256)
            {
                return null;
            }

            var colors = new List<byte[]>(count);
            for (var c = 0; c < count; c++)
            {
                if (pos + 4 > blob.Length)
                {
                    return null;
                }

                var b = blob[pos++];
                var g = blob[pos++];
                var r = blob[pos++];
                var a = blob[pos++];
                if (c == 0)
                {
                    a = 0;
                }

                colors.Add([r, g, b, a]);
            }

            return colors;
        }

        return null;
    }

    private static int? ParsePropertiesEnd(PackageData package, byte[] blob)
    {
        return PackageReader.ParseTaggedPropertiesEnd(blob, package.Names, 0);
    }

    private static PackageData LoadPackageCached(string packagePath)
    {
        lock (CacheLock)
        {
            if (!PackageCache.TryGetValue(packagePath, out var package))
            {
                package = PackageReader.LoadPackage(packagePath);
                PackageCache[packagePath] = package;
            }

            return package!;
        }
    }

    private static string? FindSiblingPackage(string currentPackagePath, string packageName, string extension)
    {
        var directory = Path.GetDirectoryName(currentPackagePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        var candidate = Path.Combine(directory, packageName + extension);
        return File.Exists(candidate) ? candidate : null;
    }


    private static int ReadInt32(ReadOnlySpan<byte> buffer, ref int position)
    {
        if (position + 4 > buffer.Length)
        {
            throw new PackageReadException("Unexpected EOF while reading Int32.");
        }

        var value = BinaryPrimitives.ReadInt32LittleEndian(buffer[position..]);
        position += 4;
        return value;
    }

    private static byte ReadByte(ReadOnlySpan<byte> buffer, ref int position)
    {
        if (position >= buffer.Length)
        {
            throw new PackageReadException("Unexpected EOF while reading byte.");
        }

        return buffer[position++];
    }

    private static byte IntegerLog2(int value)
    {
        byte result = 0;
        while (value > 1)
        {
            value >>= 1;
            result++;
        }

        return result;
    }
}
