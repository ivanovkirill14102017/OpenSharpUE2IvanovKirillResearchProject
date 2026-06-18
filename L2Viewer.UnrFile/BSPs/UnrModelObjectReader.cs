namespace L2Viewer.UnrFile;

internal static class UnrModelObjectReader
{
    public static UnrModelObject Read(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        var blob = PackageReader.ReadExportBlob(package, export);
        using var reader = PackageReader.OpenExportReader(package, export);
        reader.ReadStateFrameIfPresent(export.ObjectFlags);

        var tag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
        if (!tag.IsTerminator)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) expected no tagged properties before native payload.");
        }

        var boundsMin = reader.ReadVector3();
        var boundsMax = reader.ReadVector3();
        var boundsValid = reader.ReadByte() != 0;
        var boundingSphereCenter = reader.ReadVector3();
        var boundingSphereRadius = reader.ReadSingle();

        var vectors = ReadVectorArray(reader, className, exportIndex, objectName, "Vectors");
        var points = ReadVectorArray(reader, className, exportIndex, objectName, "Points");
        var nodes = ReadNodeArray(reader, className, exportIndex, objectName);
        var surfaces = ReadSurfaceArray(package, reader, className, exportIndex, objectName);
        var vertices = ReadVertexArray(reader, className, exportIndex, objectName);
        var (unknownTailHeaderInt0, unknownTailHeaderInt1, polysReference) = ReadTailHeader(reader, package, className, exportIndex, objectName);
        var nativeTailSize = checked((int)(reader.BaseStream.Length - reader.BaseStream.Position));

        return new UnrModelObject
        {
            ExportIndex = exportIndex,
            ClassName = className,
            ObjectName = objectName,
            BoundsMin = boundsMin,
            BoundsMax = boundsMax,
            BoundsValid = boundsValid,
            BoundingSphereCenter = boundingSphereCenter,
            BoundingSphereRadius = boundingSphereRadius,
            UnknownTailHeaderInt0 = unknownTailHeaderInt0,
            UnknownTailHeaderInt1 = unknownTailHeaderInt1,
            PolysReference = polysReference,
            Vectors = vectors,
            Points = points,
            Nodes = nodes,
            Surfaces = surfaces,
            Vertices = vertices,
            NativeTailSize = nativeTailSize
        };
    }

    private static Vector3[] ReadVectorArray(
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName,
        string fieldName)
    {
        var count = ReadNonNegativeCompactIndex(reader, className, exportIndex, objectName, fieldName);
        var values = new Vector3[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = reader.ReadVector3();
        }

        return values;
    }

    private static UnrModelNode[] ReadNodeArray(
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName)
    {
        var count = ReadNonNegativeCompactIndex(reader, className, exportIndex, objectName, "Nodes");
        var values = new UnrModelNode[count];
        for (var i = 0; i < count; i++)
        {
            var plane = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var zoneMask = reader.ReadUInt64();
            var nodeFlags = reader.ReadByte();
            var vertexPoolIndex = PackageReader.ReadCompactIndex(reader);
            var surfaceIndex = PackageReader.ReadCompactIndex(reader);
            var backNodeIndex = PackageReader.ReadCompactIndex(reader);
            var frontNodeIndex = PackageReader.ReadCompactIndex(reader);
            var planeNodeIndex = PackageReader.ReadCompactIndex(reader);
            var collisionBoundIndex = PackageReader.ReadCompactIndex(reader);
            var renderBoundIndex = PackageReader.ReadCompactIndex(reader);
            var unknownStruct0 = ReadUnknownVector3FloatStruct(reader);
            var unknownStruct1 = ReadUnknownVector3FloatStruct(reader);

            var zone0 = reader.ReadByte();
            var zone1 = reader.ReadByte();
            var vertexCount = reader.ReadByte();
            var leafIndex0 = reader.ReadInt32();
            var leafIndex1 = reader.ReadInt32();
            var unknownInt1 = reader.ReadInt32();
            var unknownInt2 = reader.ReadInt32();
            var unknownInt3 = reader.ReadInt32();

            values[i] = new UnrModelNode
            {
                Plane = plane,
                ZoneMask = zoneMask,
                NodeFlags = nodeFlags,
                VertexPoolIndex = vertexPoolIndex,
                SurfaceIndex = surfaceIndex,
                BackNodeIndex = backNodeIndex,
                FrontNodeIndex = frontNodeIndex,
                PlaneNodeIndex = planeNodeIndex,
                CollisionBoundIndex = collisionBoundIndex,
                RenderBoundIndex = renderBoundIndex,
                UnknownStruct0 = unknownStruct0,
                UnknownStruct1 = unknownStruct1,
                Zone0 = zone0,
                Zone1 = zone1,
                VertexCount = vertexCount,
                LeafIndex0 = leafIndex0,
                LeafIndex1 = leafIndex1,
                UnknownInt1 = unknownInt1,
                UnknownInt2 = unknownInt2,
                UnknownInt3 = unknownInt3
            };
        }

        return values;
    }

    private static UnrModelSurface[] ReadSurfaceArray(
        PackageData package,
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName)
    {
        var count = ReadNonNegativeCompactIndex(reader, className, exportIndex, objectName, "Surfaces");
        var values = new UnrModelSurface[count];
        for (var i = 0; i < count; i++)
        {
            var materialRawReference = PackageReader.ReadCompactIndex(reader);
            var materialReference = TryResolveObjectReference(package, materialRawReference);
            var polyFlags = reader.ReadUInt32();
            var baseIndex = PackageReader.ReadCompactIndex(reader);
            var normalIndex = PackageReader.ReadCompactIndex(reader);
            var textureUIndex = PackageReader.ReadCompactIndex(reader);
            var textureVIndex = PackageReader.ReadCompactIndex(reader);
            var brushPolyIndex = PackageReader.ReadCompactIndex(reader);
            var actorIndex = PackageReader.ReadCompactIndex(reader);
            var plane = new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var lightMapScale = reader.ReadSingle();
            var lightMapIndex = reader.ReadInt32();

            values[i] = new UnrModelSurface
            {
                MaterialRawReference = materialRawReference,
                MaterialReference = materialReference,
                PolyFlags = polyFlags,
                BaseIndex = baseIndex,
                NormalIndex = normalIndex,
                TextureUIndex = textureUIndex,
                TextureVIndex = textureVIndex,
                BrushPolyIndex = brushPolyIndex,
                ActorIndex = actorIndex,
                Plane = plane,
                LightMapScale = lightMapScale,
                LightMapIndex = lightMapIndex
            };
        }

        return values;
    }

    private static UnrModelVertex[] ReadVertexArray(
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName)
    {
        var count = PackageReader.ReadCompactIndex(reader);
        if (count < 0)
        {
            return Array.Empty<UnrModelVertex>();
        }
        
        var values = new UnrModelVertex[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = new UnrModelVertex
            {
                PointIndex = PackageReader.ReadCompactIndex(reader),
                SideIndex = PackageReader.ReadCompactIndex(reader)
            };
        }

        return values;
    }

    private static (int? UnknownTailHeaderInt0, int? UnknownTailHeaderInt1, UnrFileObjectReference? PolysReference) ReadTailHeader(
        BinaryReader reader,
        PackageData package,
        string className,
        int exportIndex,
        string objectName)
    {
        if (reader.BaseStream.Position == reader.BaseStream.Length)
        {
            return (null, null, null);
        }

        if (reader.BaseStream.Length - reader.BaseStream.Position < sizeof(int) * 2)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has truncated native Model tail before Polys reference.");
        }

        var unknownTailHeaderInt0 = reader.ReadInt32();
        var unknownTailHeaderInt1 = reader.ReadInt32();
        var rawPolysReference = PackageReader.ReadCompactIndex(reader);
        return (unknownTailHeaderInt0, unknownTailHeaderInt1, ResolveOptionalObjectReference(package, rawPolysReference));
    }

    private static UnrUnknownVector3FloatStruct ReadUnknownVector3FloatStruct(BinaryReader reader)
    {
        return new UnrUnknownVector3FloatStruct
        {
            Center = reader.ReadVector3(),
            Radius = reader.ReadSingle()
        };
    }

    private static int ReadNonNegativeCompactIndex(
        BinaryReader reader,
        string className,
        int exportIndex,
        string objectName,
        string fieldName)
    {
        var value = PackageReader.ReadCompactIndex(reader);
        if (value < 0)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has negative count {value} in '{fieldName}'.");
        }

        return value;
    }

    private static UnrFileObjectReference? ResolveOptionalObjectReference(PackageData package, int rawReference)
    {
        if (rawReference == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            throw new PackageReadException($"Model contains unresolved object reference {rawReference}.");
        }

        return rawReference < 0
            ? new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Import,
                ImportIndex = -rawReference - 1,
                ExportIndex = null,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            }
            : new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Export,
                ImportIndex = null,
                ExportIndex = rawReference - 1,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            };
    }

    private static UnrFileObjectReference? TryResolveObjectReference(PackageData package, int rawReference)
    {
        if (rawReference == 0)
        {
            return null;
        }

        var resolved = UnrPackageHelpers.ResolveObjectRef(package, rawReference);
        if (resolved is null)
        {
            return null;
        }

        return rawReference < 0
            ? new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Import,
                ImportIndex = -rawReference - 1,
                ExportIndex = null,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            }
            : new UnrFileObjectReference
            {
                RawReference = rawReference,
                Kind = UnrFileReferenceKind.Export,
                ImportIndex = null,
                ExportIndex = rawReference - 1,
                ClassName = resolved.Value.ClassName,
                ObjectName = resolved.Value.ObjectName,
                PackageName = resolved.Value.PackageName
            };
    }
}
