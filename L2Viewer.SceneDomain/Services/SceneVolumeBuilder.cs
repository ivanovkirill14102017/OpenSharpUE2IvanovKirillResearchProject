using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneVolumeBuilder
{
    public SceneVolumeData[] Build(L2Viewer.UnrFile.UnrFile unr, int maxActors)
    {
        if (maxActors <= 0)
        {
            return [];
        }

        var modelsByExportIndex = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrModelObject>()
            .ToDictionary(x => x.ExportIndex);
        var polysByExportIndex = unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrPolysObject>()
            .ToDictionary(x => x.ExportIndex);

        var placements = new List<SceneVolumeData>(Math.Min(maxActors, 32));
        foreach (var actor in unr.ExportObjects.Select(x => x.Object).OfType<UnrVolumeBaseObject>().OrderBy(x => x.ExportIndex))
        {
            if (placements.Count >= maxActors)
            {
                break;
            }

            var geometry = SceneBrushActorGeometryBuilder.Build(
                unr.FilePath,
                modelsByExportIndex,
                polysByExportIndex,
                actor,
                actor.ObjectName);

            placements.Add(CreateVolumeData(unr.FilePath, actor, geometry));
        }

        return placements.ToArray();
    }

    private static SceneVolumeData CreateVolumeData(
        string mapPath,
        UnrVolumeBaseObject actor,
        SceneBrushActorGeometryData geometry)
    {
        return actor switch
        {
            UnrWaterVolumeObject water => new SceneWaterVolumeData
            {
                ExportIndex = water.ExportIndex,
                Name = water.ObjectName,
                ClassName = water.ClassName,
                MainScale = water.MainScale,
                PostScale = water.PostScale,
                TempScale = water.TempScale,
                WorldLocation = water.Location,
                WorldRotationUnrealRaw = water.Rotation,
                WorldRotationEulerDegrees = water.Rotation is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(water.Rotation.Value),
                DrawScale = water.DrawScale,
                DrawScale3D = water.DrawScale3D,
                PrePivot = water.PrePivot,
                Group = water.Group,
                Tag = water.Tag,
                Event = water.Event,
                BrushReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.BrushReference),
                BaseReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.BaseReference),
                LevelReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.LevelReference),
                OwnerReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.OwnerReference),
                MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.MeshReference),
                TextureReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.TextureReference),
                PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.PhysicsVolumeReference),
                StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.StaticMeshReference),
                BrushPolysReference = geometry.BrushPolysReference,
                BrushModelBoundsMin = geometry.BrushModelBoundsMin,
                BrushModelBoundsMax = geometry.BrushModelBoundsMax,
                BrushModelBoundsValid = geometry.BrushModelBoundsValid,
                RenderGeometry = geometry.RenderGeometry,
                WorldBoundsMin = geometry.WorldBoundsMin,
                WorldBoundsMax = geometry.WorldBoundsMax,
                UnknownProperties = water.UnknownProperties,
                Region = water.Region,
                TexModifyInfo = water.TexModifyInfo,
                SunAffect = water.SunAffect,
                NextPhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, water.NextPhysicsVolumeReference),
                LocationName = water.LocationName,
                DynamicActorFilterState = water.DynamicActorFilterState,
                LightChanged = water.LightChanged,
                DeleteMe = water.DeleteMe,
                HiddenEd = water.HiddenEd,
                PendingDelete = water.PendingDelete,
                Selected = water.Selected,
                TouchingReferences = water.TouchingReferences.Select(x => SceneReferenceUtilities.BuildReference(mapPath, x)).ToArray(),
                ColLocation = water.ColLocation,
                DistanceFogColor = water.DistanceFogColor,
                CellophaneColor = water.CellophaneColor,
                UseDistanceFogColor = water.UseDistanceFogColor,
                UseCellophane = water.UseCellophane,
                IgnoredRange = water.IgnoredRange
            },
            UnrPhysicsVolumeObject physics => CreatePhysicsVolume(mapPath, physics, geometry),
            UnrBlockingVolumeObject blocking => CreateBlockingVolume(mapPath, blocking, geometry),
            UnrConvexVolumeObject convex => CreateConvexVolume(mapPath, convex, geometry),
            UnrDefaultPhysicsVolumeObject defaultPhysics => CreateDefaultPhysicsVolume(mapPath, defaultPhysics, geometry),
            UnrMusicVolumeObject music => CreateMusicVolume(mapPath, music, geometry),
            _ => throw new PackageReadException($"Unsupported volume actor type '{actor.GetType().Name}' for class '{actor.ClassName}'.")
        };
    }

    private static ScenePhysicsVolumeData CreatePhysicsVolume(
        string mapPath,
        UnrPhysicsVolumeObject actor,
        SceneBrushActorGeometryData geometry)
    {
        return new ScenePhysicsVolumeData
        {
            ExportIndex = actor.ExportIndex,
            Name = actor.ObjectName,
            ClassName = actor.ClassName,
            MainScale = actor.MainScale,
            PostScale = actor.PostScale,
            TempScale = actor.TempScale,
            WorldLocation = actor.Location,
            WorldRotationUnrealRaw = actor.Rotation,
            WorldRotationEulerDegrees = actor.Rotation is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(actor.Rotation.Value),
            DrawScale = actor.DrawScale,
            DrawScale3D = actor.DrawScale3D,
            PrePivot = actor.PrePivot,
            Group = actor.Group,
            Tag = actor.Tag,
            Event = actor.Event,
            BrushReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BrushReference),
            BaseReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BaseReference),
            LevelReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.LevelReference),
            OwnerReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.OwnerReference),
            MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.MeshReference),
            TextureReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.TextureReference),
            PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.PhysicsVolumeReference),
            StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.StaticMeshReference),
            BrushPolysReference = geometry.BrushPolysReference,
            BrushModelBoundsMin = geometry.BrushModelBoundsMin,
            BrushModelBoundsMax = geometry.BrushModelBoundsMax,
            BrushModelBoundsValid = geometry.BrushModelBoundsValid,
            RenderGeometry = geometry.RenderGeometry,
            WorldBoundsMin = geometry.WorldBoundsMin,
            WorldBoundsMax = geometry.WorldBoundsMax,
            UnknownProperties = actor.UnknownProperties,
            Region = actor.Region,
            TexModifyInfo = actor.TexModifyInfo
        };
    }

    private static SceneBlockingVolumeData CreateBlockingVolume(
        string mapPath,
        UnrBlockingVolumeObject actor,
        SceneBrushActorGeometryData geometry)
    {
        return new SceneBlockingVolumeData
        {
            ExportIndex = actor.ExportIndex,
            Name = actor.ObjectName,
            ClassName = actor.ClassName,
            MainScale = actor.MainScale,
            PostScale = actor.PostScale,
            TempScale = actor.TempScale,
            WorldLocation = actor.Location,
            WorldRotationUnrealRaw = actor.Rotation,
            WorldRotationEulerDegrees = actor.Rotation is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(actor.Rotation.Value),
            DrawScale = actor.DrawScale,
            DrawScale3D = actor.DrawScale3D,
            PrePivot = actor.PrePivot,
            Group = actor.Group,
            Tag = actor.Tag,
            Event = actor.Event,
            BrushReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BrushReference),
            BaseReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BaseReference),
            LevelReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.LevelReference),
            OwnerReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.OwnerReference),
            MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.MeshReference),
            TextureReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.TextureReference),
            PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.PhysicsVolumeReference),
            StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.StaticMeshReference),
            BrushPolysReference = geometry.BrushPolysReference,
            BrushModelBoundsMin = geometry.BrushModelBoundsMin,
            BrushModelBoundsMax = geometry.BrushModelBoundsMax,
            BrushModelBoundsValid = geometry.BrushModelBoundsValid,
            RenderGeometry = geometry.RenderGeometry,
            WorldBoundsMin = geometry.WorldBoundsMin,
            WorldBoundsMax = geometry.WorldBoundsMax,
            UnknownProperties = actor.UnknownProperties,
            Region = actor.Region,
            TexModifyInfo = actor.TexModifyInfo
        };
    }

    private static SceneConvexVolumeData CreateConvexVolume(
        string mapPath,
        UnrConvexVolumeObject actor,
        SceneBrushActorGeometryData geometry)
    {
        return new SceneConvexVolumeData
        {
            ExportIndex = actor.ExportIndex,
            Name = actor.ObjectName,
            ClassName = actor.ClassName,
            MainScale = actor.MainScale,
            PostScale = actor.PostScale,
            TempScale = actor.TempScale,
            WorldLocation = actor.Location,
            WorldRotationUnrealRaw = actor.Rotation,
            WorldRotationEulerDegrees = actor.Rotation is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(actor.Rotation.Value),
            DrawScale = actor.DrawScale,
            DrawScale3D = actor.DrawScale3D,
            PrePivot = actor.PrePivot,
            Group = actor.Group,
            Tag = actor.Tag,
            Event = actor.Event,
            BrushReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BrushReference),
            BaseReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BaseReference),
            LevelReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.LevelReference),
            OwnerReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.OwnerReference),
            MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.MeshReference),
            TextureReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.TextureReference),
            PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.PhysicsVolumeReference),
            StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.StaticMeshReference),
            BrushPolysReference = geometry.BrushPolysReference,
            BrushModelBoundsMin = geometry.BrushModelBoundsMin,
            BrushModelBoundsMax = geometry.BrushModelBoundsMax,
            BrushModelBoundsValid = geometry.BrushModelBoundsValid,
            RenderGeometry = geometry.RenderGeometry,
            WorldBoundsMin = geometry.WorldBoundsMin,
            WorldBoundsMax = geometry.WorldBoundsMax,
            UnknownProperties = actor.UnknownProperties,
            Region = actor.Region,
            TexModifyInfo = actor.TexModifyInfo
        };
    }

    private static SceneDefaultPhysicsVolumeData CreateDefaultPhysicsVolume(
        string mapPath,
        UnrDefaultPhysicsVolumeObject actor,
        SceneBrushActorGeometryData geometry)
    {
        return new SceneDefaultPhysicsVolumeData
        {
            ExportIndex = actor.ExportIndex,
            Name = actor.ObjectName,
            ClassName = actor.ClassName,
            MainScale = actor.MainScale,
            PostScale = actor.PostScale,
            TempScale = actor.TempScale,
            WorldLocation = actor.Location,
            WorldRotationUnrealRaw = actor.Rotation,
            WorldRotationEulerDegrees = actor.Rotation is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(actor.Rotation.Value),
            DrawScale = actor.DrawScale,
            DrawScale3D = actor.DrawScale3D,
            PrePivot = actor.PrePivot,
            Group = actor.Group,
            Tag = actor.Tag,
            Event = actor.Event,
            BrushReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BrushReference),
            BaseReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BaseReference),
            LevelReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.LevelReference),
            OwnerReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.OwnerReference),
            MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.MeshReference),
            TextureReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.TextureReference),
            PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.PhysicsVolumeReference),
            StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.StaticMeshReference),
            BrushPolysReference = geometry.BrushPolysReference,
            BrushModelBoundsMin = geometry.BrushModelBoundsMin,
            BrushModelBoundsMax = geometry.BrushModelBoundsMax,
            BrushModelBoundsValid = geometry.BrushModelBoundsValid,
            RenderGeometry = geometry.RenderGeometry,
            WorldBoundsMin = geometry.WorldBoundsMin,
            WorldBoundsMax = geometry.WorldBoundsMax,
            UnknownProperties = actor.UnknownProperties,
            Region = actor.Region,
            TexModifyInfo = actor.TexModifyInfo
        };
    }

    private static SceneMusicVolumeData CreateMusicVolume(
        string mapPath,
        UnrMusicVolumeObject actor,
        SceneBrushActorGeometryData geometry)
    {
        return new SceneMusicVolumeData
        {
            ExportIndex = actor.ExportIndex,
            Name = actor.ObjectName,
            ClassName = actor.ClassName,
            MainScale = actor.MainScale,
            PostScale = actor.PostScale,
            TempScale = actor.TempScale,
            WorldLocation = actor.Location,
            WorldRotationUnrealRaw = actor.Rotation,
            WorldRotationEulerDegrees = actor.Rotation is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(actor.Rotation.Value),
            DrawScale = actor.DrawScale,
            DrawScale3D = actor.DrawScale3D,
            PrePivot = actor.PrePivot,
            Group = actor.Group,
            Tag = actor.Tag,
            Event = actor.Event,
            BrushReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BrushReference),
            BaseReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.BaseReference),
            LevelReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.LevelReference),
            OwnerReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.OwnerReference),
            MeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.MeshReference),
            TextureReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.TextureReference),
            PhysicsVolumeReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.PhysicsVolumeReference),
            StaticMeshReference = SceneReferenceUtilities.BuildOptionalReference(mapPath, actor.StaticMeshReference),
            BrushPolysReference = geometry.BrushPolysReference,
            BrushModelBoundsMin = geometry.BrushModelBoundsMin,
            BrushModelBoundsMax = geometry.BrushModelBoundsMax,
            BrushModelBoundsValid = geometry.BrushModelBoundsValid,
            RenderGeometry = geometry.RenderGeometry,
            WorldBoundsMin = geometry.WorldBoundsMin,
            WorldBoundsMax = geometry.WorldBoundsMax,
            UnknownProperties = actor.UnknownProperties,
            Region = actor.Region,
            TexModifyInfo = actor.TexModifyInfo
        };
    }

}
