namespace L2Viewer.UnrFile;

internal static class UnrPolysObjectReader
{
    public static UnrPolysObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        var native = UnrFileNativeStrictReaders.TryDecodePolysStrict(package, exportIndex);
        if (native is null)
        {
            return new UnrPolysObject
            {
                ExportIndex = exportIndex,
                ClassName = className,
                ObjectName = objectName,
                Polys = []
            };
        }

        var polys = native.Polys.Select(ConvertPoly).ToArray();

        return new UnrPolysObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            Polys = polys
        };
    }

    private static UnrFilePoly ConvertPoly(PolyNativeData poly)
    {
        return new UnrFilePoly
        {
            Index = poly.Index,
            NumVertices = poly.NumVertices,
            Base = poly.Base,
            Normal = poly.Normal,
            TextureU = poly.TextureU,
            TextureV = poly.TextureV,
            Vertices = poly.Vertices.ToArray(),
            PolyFlags = poly.PolyFlags,
            ActorReference = ConvertReference(poly.ActorReference),
            TextureReference = ConvertReference(poly.TextureReference),
            ItemName = poly.ItemName,
            ILink = poly.ILink,
            IBrushPoly = poly.IBrushPoly,
            LightMapScale = poly.LightMapScale,
            SavePolyIndex = poly.SavePolyIndex
        };
    }

    private static UnrFileObjectReference? ConvertReference(NativeObjectReferenceInfo? nativeReference)
    {
        if (nativeReference is null)
        {
            return null;
        }

        return new UnrFileObjectReference
        {
            RawReference = nativeReference.RawReference,
            Kind = nativeReference.RawReference < 0 ? UnrFileReferenceKind.Import : UnrFileReferenceKind.Export,
            ImportIndex = nativeReference.RawReference < 0 ? -nativeReference.RawReference - 1 : null,
            ExportIndex = nativeReference.RawReference > 0 ? nativeReference.RawReference - 1 : null,
            ClassName = nativeReference.ClassName,
            ObjectName = nativeReference.ObjectName,
            PackageName = nativeReference.PackageName
        };
    }
}
