namespace L2Viewer.UnrFile;

public static class UnrFileReader
{
    public static UnrFile Read(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("UNR file was not found.", path);
        }

        var package = PackageReader.LoadPackage(path);
        var names = new UnrFileNameEntry[package.Names.Count];
        var imports = new UnrFileImportEntry[package.Imports.Count];
        var exportObjects = new List<UnrFileExportObjectEntry>(package.Exports.Count);

        for (var i = 0; i < package.Names.Count; i++)
        {
            names[i] = new UnrFileNameEntry
            {
                Index = i,
                Name = package.Names[i]
            };
        }

        for (var i = 0; i < package.Imports.Count; i++)
        {
            var import = package.Imports[i];
            imports[i] = new UnrFileImportEntry
            {
                Index = i,
                ClassPackage = import.ClassPackage,
                ClassName = import.ClassName,
                PackageIndex = import.PackageIndex,
                ObjectName = import.ObjectName
            };
        }

        for (var i = 0; i < package.Exports.Count; i++)
        {
            var export = package.Exports[i];
            var className = PackageReader.ExportClassName(package, export);
            var classKind = ParseExportClassKind(className);

            var objectName = PackageReader.SafeName(package.Names, export.ObjectName);

            var exportModel = new UnrFileExportEntry
            {
                Index = i,
                ClassIndex = export.ClassIndex,
                SuperIndex = export.SuperIndex,
                PackageIndex = export.PackageIndex,
                ObjectName = export.ObjectName,
                ObjectFlags = export.ObjectFlags,
                SerialSize = export.SerialSize,
                SerialOffset = export.SerialOffset
            };

            var objectModel = ReadObject(package, export, i, classKind, className, objectName);
            exportObjects.Add(new UnrFileExportObjectEntry
            {
                Export = exportModel,
                Object = objectModel
            });
        }

        return new UnrFile
        {
            FilePath = path,
            Wrapper = package.Wrapper,
            Header = new UnrFileHeader
            {
                Version = package.Header.Version,
                LicenseeVersion = package.Header.LicenseeVersion,
                Flags = package.Header.Flags,
                NameCount = package.Header.NameCount,
                NameOffset = package.Header.NameOffset,
                ExportCount = package.Header.ExportCount,
                ExportOffset = package.Header.ExportOffset,
                ImportCount = package.Header.ImportCount,
                ImportOffset = package.Header.ImportOffset
            },
            Names = names,
            Imports = imports,
            ExportObjects = exportObjects.ToArray()
        };
    }

    private static UnrFileObject ReadObject(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        UnrExportClassKind classKind,
        string className,
        string objectName)
    {
        return CreateObject(package, export, exportIndex, classKind, className, objectName);
    }

    private static UnrFileObject CreateObject(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        UnrExportClassKind classKind,
        string className,
        string objectName)
    {
        if (classKind == UnrExportClassKind.Brush)
        {
            return UnrBrushObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.NMovableSunLight)
        {
            return UnrNMovableSunLightObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.LevelInfo)
        {
            return UnrLevelInfoObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Level)
        {
            return UnrLevelObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.ZoneInfo)
        {
            return UnrZoneInfoObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Light)
        {
            return UnrLightObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Spotlight)
        {
            return UnrLightObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.TerrainInfo)
        {
            return UnrTerrainInfoObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.MusicVolume)
        {
            return UnrSimpleActorObjectReader.ReadMusicVolume(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.AmbientSound)
        {
            return UnrSimpleActorObjectReader.ReadAmbientSound(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.AmbientSoundObject)
        {
            return UnrSimpleActorObjectReader.ReadAmbientSound(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Emitter)
        {
            return UnrEmitterObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.MeshEmitter)
        {
            return UnrMeshEmitterObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.SpriteEmitter)
        {
            return UnrSpriteEmitterObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.PlayerStart)
        {
            return UnrSimpleActorObjectReader.ReadPlayerStart(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.PathNode)
        {
            return UnrSimpleActorObjectReader.ReadPathNode(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.SkyZoneInfo)
        {
            return UnrSkyZoneInfoObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.StaticMeshActor)
        {
            return UnrStaticMeshActorObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.MovableStaticMeshActor)
        {
            return UnrMovableStaticMeshActorObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.BlockingVolume)
        {
            return UnrSimpleActorObjectReader.ReadBlockingVolume(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.PhysicsVolume)
        {
            return UnrSimpleActorObjectReader.ReadPhysicsVolume(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.WaterVolume)
        {
            return UnrWaterVolumeObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Camera)
        {
            return UnrSimpleActorObjectReader.ReadCamera(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.NMoon)
        {
            return UnrNMoonObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.L2FogInfo)
        {
            return UnrL2FogInfoObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.NSun)
        {
            return UnrNSunObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.TerrainSector)
        {
            return UnrTerrainSectorObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.StaticMeshInstance)
        {
            return UnrStaticMeshInstanceObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Polys)
        {
            return UnrPolysObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Model)
        {
            return UnrModelObjectReader.Read(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.LevelSummary)
        {
            return UnrSimpleActorObjectReader.ReadLevelSummary(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Mover)
        {
            return UnrSimpleActorObjectReader.ReadMover(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.Projector)
        {
            return UnrSimpleActorObjectReader.ReadProjector(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.ReachSpec)
        {
            return UnrSimpleActorObjectReader.ReadReachSpec(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.ConvexVolume)
        {
            return UnrSimpleActorObjectReader.ReadConvexVolume(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.AntiPortalActor)
        {
            return UnrSimpleActorObjectReader.ReadAntiPortalActor(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.BeamEmitter)
        {
            return UnrSimpleActorObjectReader.ReadBeamEmitter(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.VertMeshEmitter)
        {
            return UnrSimpleActorObjectReader.ReadVertMeshEmitter(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.SceneManager)
        {
            return UnrSimpleActorObjectReader.ReadSceneManager(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.L2SeamlessInfo)
        {
            return UnrSimpleActorObjectReader.ReadL2SeamlessInfo(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.LineagePlayerController)
        {
            return UnrSimpleActorObjectReader.ReadLineagePlayerController(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.InterpolationPoint)
        {
            return UnrSimpleActorObjectReader.ReadInterpolationPoint(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.ActionWarp)
        {
            return UnrSimpleActorObjectReader.ReadActionWarp(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.ActionMoveCamera)
        {
            return UnrSimpleActorObjectReader.ReadActionMoveCamera(package, export, exportIndex, className, objectName);
        }

        if (classKind == UnrExportClassKind.DefaultPhysicsVolume)
        {
            return UnrSimpleActorObjectReader.ReadDefaultPhysicsVolume(package, export, exportIndex, className, objectName);
        }

        throw new PackageReadException(
            $"L2Viewer.UnrFile is temporarily limited to explicitly supported classes. Unsupported class '{className}' at export index {exportIndex} ({objectName}).");
    }

    private static bool TryParseExportClassKind(string className, out UnrExportClassKind classKind)
    {
        return Enum.TryParse(className, ignoreCase: true, out classKind);
    }

    private static UnrExportClassKind ParseExportClassKind(string className)
    {
        if (TryParseExportClassKind(className, out var classKind))
        {
            return classKind;
        }

        throw new PackageReadException(
            $"L2Viewer.UnrFile is temporarily limited to explicitly supported classes. Unsupported class '{className}'.");
    }

    private enum UnrExportClassKind
    {
        Brush,
        NMovableSunLight,
        LevelInfo,
        Level,
        ZoneInfo,
        Light,
        Spotlight,
        TerrainInfo,
        MusicVolume,
        AmbientSound,
        AmbientSoundObject,
        Emitter,
        MeshEmitter,
        SpriteEmitter,
        PlayerStart,
        PathNode,
        SkyZoneInfo,
        StaticMeshActor,
        MovableStaticMeshActor,
        BlockingVolume,
        PhysicsVolume,
        WaterVolume,
        Camera,
        NMoon,
        L2FogInfo,
        NSun,
        TerrainSector,
        StaticMeshInstance,
        Polys,
        Model,
        LevelSummary,
        Mover,
        Projector,
        ReachSpec,
        ConvexVolume,
        AntiPortalActor,
        BeamEmitter,
        VertMeshEmitter,
        SceneManager,
        L2SeamlessInfo,
        LineagePlayerController,
        InterpolationPoint,
        ActionWarp,
        ActionMoveCamera,
        DefaultPhysicsVolume
    }
}
