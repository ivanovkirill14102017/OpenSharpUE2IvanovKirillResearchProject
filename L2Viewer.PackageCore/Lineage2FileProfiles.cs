using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Text;

namespace L2Viewer.PackageCore;

public static class Lineage2FileProfiles
{
    private const int RsaBlockSize = 128;
    private const int RsaBlockBodySize = 124;
    private const int Ver413TailLength = 20;
    public static readonly Lineage2FileProfile Ver111 = new(
        111,
        "Lineage2Ver111",
        "XOR",
        "Known single-byte XOR profile used by older Lineage II client files.",
        XorKeyHex: "AC");

    public static readonly Lineage2FileProfile Ver120 = new(
        120,
        "Lineage2Ver120",
        "XOR_POSITION",
        "Position-dependent XOR profile documented by open-l2encdec.",
        XorStartPositionHex: "E6");

    public static readonly Lineage2FileProfile Ver121 = new(
        121,
        "Lineage2Ver121",
        "XOR_FILENAME",
        "Filename-derived XOR profile documented by open-l2encdec.");

    public static readonly Lineage2FileProfile Ver211 = new(
        211,
        "Lineage2Ver211",
        "BLOWFISH",
        "Blowfish profile documented by open-l2encdec.",
        BlowfishKey: "31==-%&@!^+][;'.]94-");

    public static readonly Lineage2FileProfile Ver212 = new(
        212,
        "Lineage2Ver212",
        "BLOWFISH",
        "Blowfish profile documented by open-l2encdec.",
        BlowfishKey: "[;'.]94-&@%!^+]-31==");

    public static readonly Lineage2FileProfile Ver411Legacy = new(
        411,
        "Lineage2Ver411",
        "RSA_LEGACY",
        "Legacy RSA profile documented by open-l2encdec / l2encdec.",
        RsaModulusHex: "8c9d5da87b30f5d7cd9dc88c746eaac5bb180267fa11737358c4c95d9adf59dd37689f9befb251508759555d6fe0eca87bebe0a10712cf0ec245af84cd22eb4cb675e98eaf5799fca62a20a2baa4801d5d70718dcd43283b8428f1387aec6600f937bfc7bb72404d187d3a9c438f1ffce9ce365dccf754232ff6def038a41385",
        RsaPrivateExponentHex: "1d");

    public static readonly Lineage2FileProfile Ver412Legacy = new(
        412,
        "Lineage2Ver412",
        "RSA_LEGACY",
        "Legacy RSA profile documented by open-l2encdec / l2encdec.",
        RsaModulusHex: "a465134799cf2c45087093e7d0f0f144e6d528110c08f674730d436e40827330eccea46e70acf10cdda7d8f710e3b44dcca931812d76cd7494289bca8b73823f57efc0515b97e4a2a02612ccfa719cf7885104b06f2e7e2cc967b62e3d3b1aadb925db94cbc8cd3070a4bb13f7e202c7733a67b1b94c1ebc0afcbe1a63b448cf",
        RsaPrivateExponentHex: "25");

    public static readonly Lineage2FileProfile Ver413Legacy = new(
        413,
        "Lineage2Ver413",
        "RSA_LEGACY",
        "Legacy RSA profile documented by open-l2encdec / l2encdec.",
        RsaModulusHex: "97df398472ddf737ef0a0cd17e8d172f0fef1661a38a8ae1d6e829bc1c6e4c3cfc19292dda9ef90175e46e7394a18850b6417d03be6eea274d3ed1dde5b5d7bde72cc0a0b71d03608655633881793a02c9a67d9ef2b45eb7c08d4be329083ce450e68f7867b6749314d40511d09bc5744551baa86a89dc38123dc1668fd72d83",
        RsaPrivateExponentHex: "35");

    public static readonly Lineage2FileProfile Ver414Legacy = new(
        414,
        "Lineage2Ver414",
        "RSA_LEGACY",
        "Legacy RSA profile documented by open-l2encdec / l2encdec.",
        RsaModulusHex: "ad70257b2316ce09dfaf2ebc3f63b3d673b0c98a403950e26bb87379b11e17aed0e45af23e7171e5ec1fbc8d1ae32ffb7801b31266eef9c334b53469d4b7cbe83284273d35a9aab49b453e7012f374496c65f8089f5d134b0eb3d1e3b22051ed5977a6dd68c4f85785dfcc9f4412c81681944fc4b8ce27caf0242deaa5762e8d",
        RsaPrivateExponentHex: "25");

    public static readonly Lineage2FileProfile ModernRsa = new(
        413,
        "Lineage2Ver413",
        "RSA_MODERN",
        "Modern RSA profile used by open-l2encdec by default for 411-414 encoding/decoding.",
        RsaModulusHex: "75b4d6de5c016544068a1acf125869f43d2e09fc55b8b1e289556daf9b8757635593446288b3653da1ce91c87bb1a5c18f16323495c55d7d72c0890a83f69bfd1fd9434eb1c02f3e4679edfa43309319070129c267c85604d87bb65bae205de3707af1d2108881abb567c3b3d069ae67c3a4c6a3aa93d26413d4c66094ae2039",
        RsaPublicExponentHex: "30b4c2d798d47086145c75063c8e841e719776e400291d7838d3e6c4405b504c6a07f8fca27f32b86643d2649d1d5f124cdd0bf272f0909dd7352fe10a77b34d831043d9ae541f8263c6fe3d1c14c2f04e43a7253a6dda9a8c1562cbd493c1b631a1957618ad5dfe5ca28553f746e2fc6f2db816c7db223ec91e955081c1de65",
        RsaPrivateExponentHex: "1d");

    private static readonly byte[] Prefix111Bytes = Encoding.Unicode.GetBytes(Ver111.Wrapper);
    private static readonly byte[] Prefix121Bytes = Encoding.Unicode.GetBytes(Ver121.Wrapper);
    private static readonly byte[] Prefix413Bytes = Encoding.Unicode.GetBytes(Ver413Legacy.Wrapper);
    private static readonly BigInteger ModernRsaModulus = ParseHexBigInteger(ModernRsa.RsaModulusHex!);
    private static readonly BigInteger ModernRsaPrivateExponent = ParseHexBigInteger(ModernRsa.RsaPrivateExponentHex!);

    public static IReadOnlyList<Lineage2FileProfile> All { get; } =
    [
        Ver111,
        Ver120,
        Ver121,
        Ver211,
        Ver212,
        Ver411Legacy,
        Ver412Legacy,
        Ver413Legacy,
        Ver414Legacy,
        ModernRsa
    ];

    public static bool TryGetByWrapper(string wrapper, out Lineage2FileProfile profile)
    {
        profile = All.FirstOrDefault(x => string.Equals(x.Wrapper, wrapper, StringComparison.Ordinal))!;
        return profile is not null;
    }

    public static (string Wrapper, int PrefixLength) DetectWrapper(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Prefix111Bytes))
        {
            return (Ver111.Wrapper, Prefix111Bytes.Length);
        }

        if (bytes.StartsWith(Prefix121Bytes))
        {
            return (Ver121.Wrapper, Prefix121Bytes.Length);
        }

        if (bytes.StartsWith(Prefix413Bytes))
        {
            return (Ver413Legacy.Wrapper, Prefix413Bytes.Length);
        }

        return ("raw", 0);
    }

    public static byte[] DecodeWrappedFile(byte[] bytes, string wrapper, int prefixLength)
    {
        return wrapper switch
        {
            "raw" => bytes,
            "Lineage2Ver413" => DecodeVer413(bytes, prefixLength),
            _ => throw new NotSupportedException($"Wrapper '{wrapper}' is not supported for generic decoding.")
        };
    }

    private static byte[] DecodeVer413(byte[] bytes, int prefixLength)
    {
        if (bytes.Length < prefixLength + Ver413TailLength)
        {
            throw new InvalidDataException("Lineage2Ver413 file is too small.");
        }

        var encryptedBodyLength = bytes.Length - prefixLength - Ver413TailLength;
        if (encryptedBodyLength <= 0 || encryptedBodyLength % RsaBlockSize != 0)
        {
            throw new InvalidDataException("Lineage2Ver413 body length is not aligned to RSA blocks.");
        }

        var decryptedBlocks = new byte[encryptedBodyLength];
        var encryptedBody = bytes.AsSpan(prefixLength, encryptedBodyLength);
        for (var offset = 0; offset < encryptedBody.Length; offset += RsaBlockSize)
        {
            DecryptRsaBlock(
                encryptedBody.Slice(offset, RsaBlockSize),
                decryptedBlocks.AsSpan(offset, RsaBlockSize));
        }

        var unpadded = RemoveVer413Padding(decryptedBlocks);
        if (unpadded.Length < sizeof(int))
        {
            throw new InvalidDataException("Lineage2Ver413 payload is smaller than the zlib size header.");
        }

        var expectedLength = BitConverter.ToInt32(unpadded, 0);
        if (expectedLength < 0)
        {
            throw new InvalidDataException("Lineage2Ver413 payload has a negative decoded size.");
        }

        return DecompressVer413Payload(unpadded.AsSpan(sizeof(int)).ToArray(), expectedLength);
    }

    private static byte[] RemoveVer413Padding(byte[] decryptedBlocks)
    {
        using var buffer = new MemoryStream(decryptedBlocks.Length);
        for (var offset = 0; offset < decryptedBlocks.Length; offset += RsaBlockSize)
        {
            var chunkSize = Math.Min((int)decryptedBlocks[offset + 3], RsaBlockBodySize);
            var alignedChunkSize = Align4(chunkSize);
            var chunkOffset = offset + RsaBlockSize - alignedChunkSize;
            buffer.Write(decryptedBlocks, chunkOffset, chunkSize);
        }

        return buffer.ToArray();
    }

    private static byte[] DecompressVer413Payload(byte[] compressedPayload, int expectedLength)
    {
        var deflatePayload = ExtractZlibDeflatePayload(compressedPayload);
        using var input = new MemoryStream(deflatePayload);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream(Math.Max(expectedLength, 0));
        deflate.CopyTo(output);
        var result = output.ToArray();
        if (result.Length != expectedLength)
        {
            throw new InvalidDataException($"Lineage2Ver413 decompressed size mismatch. Expected {expectedLength}, got {result.Length}.");
        }

        return result;
    }

    private static byte[] ExtractZlibDeflatePayload(byte[] compressedPayload)
    {
        if (compressedPayload.Length < 6)
        {
            throw new InvalidDataException("Lineage2Ver413 compressed payload is too small to contain a zlib header and checksum.");
        }

        var cmf = compressedPayload[0];
        var flg = compressedPayload[1];
        if ((cmf & 0x0F) != 8)
        {
            throw new InvalidDataException($"Unsupported zlib compression method in Lineage2Ver413 payload: {(cmf & 0x0F)}.");
        }

        if ((((cmf << 8) + flg) % 31) != 0)
        {
            throw new InvalidDataException("Lineage2Ver413 payload has an invalid zlib header checksum.");
        }

        if ((flg & 0x20) != 0)
        {
            throw new InvalidDataException("Lineage2Ver413 payload uses a preset zlib dictionary, which is not supported.");
        }

        var deflateLength = compressedPayload.Length - 2 - 4;
        var result = new byte[deflateLength];
        Buffer.BlockCopy(compressedPayload, 2, result, 0, deflateLength);
        return result;
    }

    private static void DecryptRsaBlock(ReadOnlySpan<byte> encryptedBlock, Span<byte> destination)
    {
        var encryptedInteger = ReadUnsignedBigEndian(encryptedBlock);
        var decryptedInteger = BigInteger.ModPow(encryptedInteger, ModernRsaPrivateExponent, ModernRsaModulus);
        WriteUnsignedBigEndian(decryptedInteger, destination);
    }

    private static BigInteger ParseHexBigInteger(string value)
    {
        return BigInteger.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }

    private static BigInteger ReadUnsignedBigEndian(ReadOnlySpan<byte> bytes)
    {
        var littleEndian = new byte[bytes.Length + 1];
        for (var i = 0; i < bytes.Length; i++)
        {
            littleEndian[i] = bytes[bytes.Length - 1 - i];
        }

        return new BigInteger(littleEndian);
    }

    private static void WriteUnsignedBigEndian(BigInteger value, Span<byte> destination)
    {
        destination.Clear();
        var littleEndian = value.ToByteArray();
        var bytesToCopy = littleEndian.Length;
        if (bytesToCopy > 0 && littleEndian[^1] == 0)
        {
            bytesToCopy--;
        }

        if (bytesToCopy > destination.Length)
        {
            throw new InvalidDataException("Decrypted RSA block exceeded the expected size.");
        }

        for (var i = 0; i < bytesToCopy; i++)
        {
            destination[destination.Length - 1 - i] = littleEndian[i];
        }
    }

    private static int Align4(int value)
    {
        return (value + 3) & ~3;
    }
}

public sealed record Lineage2FileProfile(
    int Protocol,
    string Wrapper,
    string Algorithm,
    string Notes,
    string? XorKeyHex = null,
    string? XorStartPositionHex = null,
    string? BlowfishKey = null,
    string? RsaModulusHex = null,
    string? RsaPublicExponentHex = null,
    string? RsaPrivateExponentHex = null);
