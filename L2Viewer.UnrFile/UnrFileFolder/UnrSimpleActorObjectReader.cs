namespace L2Viewer.UnrFile;

internal static class UnrSimpleActorObjectReader
{
    public static UnrMusicVolumeObject ReadMusicVolume(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrMusicVolumeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            Region = data.Region,
            TexModifyInfo = data.TexModifyInfo,
            UnknownProperties = data.UnknownProperties
        }, parseRegionAndTexModify: true);
    }

    public static UnrAmbientSoundObject ReadAmbientSound(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrAmbientSoundObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrPlayerStartObject ReadPlayerStart(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrPlayerStartObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrPathNodeObject ReadPathNode(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrPathNodeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrSkyZoneInfoObject ReadSkyZoneInfo(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrSkyZoneInfoObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrStaticMeshActorObject ReadStaticMeshActor(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrStaticMeshActorObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrBlockingVolumeObject ReadBlockingVolume(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrBlockingVolumeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            Region = data.Region,
            TexModifyInfo = data.TexModifyInfo,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true, parseRegionAndTexModify: true);
    }

    public static UnrPhysicsVolumeObject ReadPhysicsVolume(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrPhysicsVolumeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            Region = data.Region,
            TexModifyInfo = data.TexModifyInfo,
            UnknownProperties = data.UnknownProperties
        }, parseRegionAndTexModify: true);
    }

    public static UnrWaterVolumeObject ReadWaterVolume(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrWaterVolumeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrCameraObject ReadCamera(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrCameraObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrNMoonObject ReadNMoon(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrNMoonObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrNSunObject ReadNSun(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrNSunObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrTerrainSectorObject ReadTerrainSector(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrTerrainSectorObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrStaticMeshInstanceObject ReadStaticMeshInstance(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrStaticMeshInstanceObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrLevelSummaryObject ReadLevelSummary(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrLevelSummaryObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrMoverObject ReadMover(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrMoverObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        });
    }

    public static UnrProjectorObject ReadProjector(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrProjectorObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrReachSpecObject ReadReachSpec(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrReachSpecObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrConvexVolumeObject ReadConvexVolume(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrConvexVolumeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            Region = data.Region,
            TexModifyInfo = data.TexModifyInfo,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true, parseRegionAndTexModify: true);
    }

    public static UnrAntiPortalActorObject ReadAntiPortalActor(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrAntiPortalActorObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrBeamEmitterObject ReadBeamEmitter(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrBeamEmitterObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrVertMeshEmitterObject ReadVertMeshEmitter(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrVertMeshEmitterObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrSceneManagerObject ReadSceneManager(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrSceneManagerObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrL2SeamlessInfoObject ReadL2SeamlessInfo(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrL2SeamlessInfoObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrLineagePlayerControllerObject ReadLineagePlayerController(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrLineagePlayerControllerObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrInterpolationPointObject ReadInterpolationPoint(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrInterpolationPointObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrActionWarpObject ReadActionWarp(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrActionWarpObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrActionMoveCameraObject ReadActionMoveCamera(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrActionMoveCameraObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            UnknownProperties = data.UnknownProperties
        }, allowUnparsedTail: true);
    }

    public static UnrDefaultPhysicsVolumeObject ReadDefaultPhysicsVolume(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName)
    {
        return ReadActor(package, export, exportIndex, className, objectName, static data => new UnrDefaultPhysicsVolumeObject
        {
            ExportIndex = data.ExportIndex,
            ClassName = data.ClassName,
            ObjectName = data.ObjectName,
            MainScale = data.MainScale,
            PostScale = data.PostScale,
            TempScale = data.TempScale,
            Location = data.Location,
            Rotation = data.Rotation,
            DrawScale = data.DrawScale,
            DrawScale3D = data.DrawScale3D,
            PrePivot = data.PrePivot,
            Group = data.Group,
            Tag = data.Tag,
            Event = data.Event,
            BrushReference = data.BrushReference,
            BaseReference = data.BaseReference,
            LevelReference = data.LevelReference,
            OwnerReference = data.OwnerReference,
            MeshReference = data.MeshReference,
            TextureReference = data.TextureReference,
            PhysicsVolumeReference = data.PhysicsVolumeReference,
            StaticMeshReference = data.StaticMeshReference,
            Region = data.Region,
            TexModifyInfo = data.TexModifyInfo,
            UnknownProperties = data.UnknownProperties
        }, parseRegionAndTexModify: true);
    }

    private static TActor ReadActor<TActor>(
        PackageData package,
        ExportEntry export,
        int exportIndex,
        string className,
        string objectName,
        Func<ActorReadData, TActor> factory,
        bool allowUnparsedTail = false,
        bool parseRegionAndTexModify = false)
    {
        using var reader = PackageReader.OpenExportReader(package, export);
        Vector3? location = null;
        Vector3? rotation = null;
        var drawScale = 1f;
        var drawScale3D = Vector3.One;
        var prePivot = Vector3.Zero;
        UnrFileScale? mainScale = null;
        UnrFileScale? postScale = null;
        UnrFileScale? tempScale = null;
        string? tagName = null;
        string? eventName = null;
        string? groupName = null;
        UnrFileObjectReference? brushReference = null;
        UnrFileObjectReference? baseReference = null;
        UnrFileObjectReference? levelReference = null;
        UnrFileObjectReference? ownerReference = null;
        UnrFileObjectReference? meshReference = null;
        UnrFileObjectReference? textureReference = null;
        UnrFileObjectReference? physicsVolumeReference = null;
        UnrFileObjectReference? staticMeshReference = null;
        UnrPointRegion? region = null;
        UnrTextureModifyInfo? texModifyInfo = null;
        var unknownProperties = new List<UnrFileUnknownProperty>();

        void AddUnknownProperty(StreamPropertyTag tag)
        {
            switch (tag.Type)
            {
                case PackageReader.PropertyTypeBool when tag.DataSize == 0:
                    unknownProperties.Add(CreateUnknownProperty(tag, unknownProperties.Count));
                    return;
                case PackageReader.PropertyTypeFloat:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        floatValue: reader.ReadFloatProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeInt:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        intValue: reader.ReadIntProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeByte:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        byteValue: reader.ReadByteProperty(tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeName:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        nameValue: reader.ReadNameProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeObject:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        objectReference: reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName)));
                    return;
                case PackageReader.PropertyTypeStruct when tag.StructName.Is("Scale"):
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        scaleValue: reader.ReadScaleProperty(package, tag, className, exportIndex, objectName)));
                    return;
                default:
                    unknownProperties.Add(CreateUnknownProperty(
                        tag,
                        unknownProperties.Count,
                        rawHex: UnrCompat.ToHexString(reader.SkipPropertyPayload(tag, className, exportIndex, objectName, package.Names))));
                    return;
            }
        }

        void ReadProperty(StreamPropertyTag tag)
        {
            if (tag.IsFullyEncodedInTag())
            {
                AddUnknownProperty(tag);
                return;
            }

            if (!Enum.TryParse<UnrSimpleActorPropertyKind>(tag.Name, ignoreCase: true, out var kind))
            {
                AddUnknownProperty(tag);
                return;
            }

            switch (kind)
            {
                case UnrSimpleActorPropertyKind.Location:
                    location = reader.ReadVectorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Rotation:
                    rotation = reader.ReadRotatorProperty(tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.DrawScale:
                    drawScale = reader.ReadFloatProperty(tag, className, exportIndex, objectName) ?? drawScale;
                    return;
                case UnrSimpleActorPropertyKind.DrawScale3D:
                    drawScale3D = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? drawScale3D;
                    return;
                case UnrSimpleActorPropertyKind.PrePivot:
                    prePivot = reader.ReadVectorProperty(tag, className, exportIndex, objectName) ?? prePivot;
                    return;
                case UnrSimpleActorPropertyKind.MainScale:
                    mainScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.PostScale:
                    postScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.TempScale:
                    tempScale = reader.ReadScaleProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Tag:
                    tagName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Event:
                    eventName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Group:
                    groupName = reader.ReadNameProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Brush:
                    brushReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Base:
                    baseReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Level:
                    levelReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Owner:
                    ownerReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Mesh:
                    meshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Texture:
                    textureReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.PhysicsVolume:
                    physicsVolumeReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.StaticMesh:
                    staticMeshReference = reader.ReadOptionalObjectReferenceProperty(package, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.Region:
                    if (!parseRegionAndTexModify)
                    {
                        AddUnknownProperty(tag);
                        return;
                    }

                    region = UnrStructPropertyReader.ReadPointRegionProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
                case UnrSimpleActorPropertyKind.TexModifyInfo:
                    if (!parseRegionAndTexModify)
                    {
                        AddUnknownProperty(tag);
                        return;
                    }

                    texModifyInfo = UnrStructPropertyReader.ReadTextureModifyInfoProperty(package, reader, tag, className, exportIndex, objectName);
                    return;
            }
        }

        reader.ReadStateFrameIfPresent(export.ObjectFlags);
        var hasTerminatingNone = false;

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = reader.ReadPropertyTag(package.Names, className, exportIndex, objectName);
            if (tag.IsTerminator)
            {
                hasTerminatingNone = true;
                break;
            }

            if (tag.DataSize < 0)
            {
                throw new PackageReadException(
                    $"{className} export {exportIndex} ({objectName}) has invalid negative property size for '{tag.Name}'.");
            }

            var payloadStart = reader.BaseStream.Position;
            ReadProperty(tag);
            var consumed = reader.BaseStream.Position - payloadStart;
            tag.EnsurePropertyFullyConsumed(consumed, className, exportIndex, objectName);
        }

        if (!hasTerminatingNone)
        {
            throw new PackageReadException(
                $"{className} export {exportIndex} ({objectName}) has no terminating None property before EOF.");
        }

        if (!allowUnparsedTail)
        {
            reader.EnsureExportFullyConsumed(className, exportIndex, objectName);
        }

        return factory(new ActorReadData(
            exportIndex,
            className,
            objectName,
            mainScale,
            postScale,
            tempScale,
            location,
            rotation,
            drawScale,
            drawScale3D,
            prePivot,
            groupName,
            tagName,
            eventName,
            brushReference,
            baseReference,
            levelReference,
            ownerReference,
            meshReference,
            textureReference,
            physicsVolumeReference,
            staticMeshReference,
            region,
            texModifyInfo,
            unknownProperties.ToArray()));
    }

    private static UnrFileUnknownProperty CreateUnknownProperty(
        StreamPropertyTag tag,
        int order,
        byte? byteValue = null,
        int? intValue = null,
        float? floatValue = null,
        string? nameValue = null,
        UnrFileObjectReference? objectReference = null,
        UnrFileScale? scaleValue = null,
        string? rawHex = null)
    {
        return new UnrFileUnknownProperty
        {
            Order = order,
            Name = tag.Name,
            Type = tag.Type,
            DataSize = tag.DataSize,
            StructName = tag.StructName,
            BoolValue = tag.BoolValue,
            ByteValue = byteValue,
            IntValue = intValue,
            FloatValue = floatValue,
            NameValue = nameValue,
            ObjectReference = objectReference,
            ScaleValue = scaleValue,
            RawHex = rawHex
        };
    }

    private enum UnrSimpleActorPropertyKind
    {
        Location,
        Rotation,
        DrawScale,
        DrawScale3D,
        PrePivot,
        MainScale,
        PostScale,
        TempScale,
        Tag,
        Event,
        Group,
        Brush,
        Base,
        Level,
        Region,
        Owner,
        Mesh,
        Texture,
        PhysicsVolume,
        StaticMesh,
        TexModifyInfo
    }

    private sealed record ActorReadData(
        int ExportIndex,
        string ClassName,
        string ObjectName,
        UnrFileScale? MainScale,
        UnrFileScale? PostScale,
        UnrFileScale? TempScale,
        Vector3? Location,
        Vector3? Rotation,
        float DrawScale,
        Vector3 DrawScale3D,
        Vector3 PrePivot,
        string? Group,
        string? Tag,
        string? Event,
        UnrFileObjectReference? BrushReference,
        UnrFileObjectReference? BaseReference,
        UnrFileObjectReference? LevelReference,
        UnrFileObjectReference? OwnerReference,
        UnrFileObjectReference? MeshReference,
        UnrFileObjectReference? TextureReference,
        UnrFileObjectReference? PhysicsVolumeReference,
        UnrFileObjectReference? StaticMeshReference,
        UnrPointRegion? Region,
        UnrTextureModifyInfo? TexModifyInfo,
        UnrFileUnknownProperty[] UnknownProperties);
}
