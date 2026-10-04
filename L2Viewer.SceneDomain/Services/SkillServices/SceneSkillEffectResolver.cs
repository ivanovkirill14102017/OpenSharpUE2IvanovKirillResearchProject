using L2Viewer.SceneDomain.Models;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Services.MaterialServices;
using L2Viewer.SceneDomain.Services.Utility;
using L2Viewer.UnrFile;

namespace L2Viewer.SceneDomain.Services.SkillServices;

internal static class SceneSkillEffectResolver
{
    public static IReadOnlyList<SceneSkillVisualEffectData> ResolveEffects(
        string clientRoot,
        string lineageEffectPath,
        string skillVisualPath,
        IReadOnlyList<SceneSkillLevelData> levels,
        ICollection<string> warnings)
    {
        var rawStages = UnrSkillEffectPackageReader.ReadStages(lineageEffectPath);
        var stagesByClass = rawStages.ToDictionary(x => x.ObjectName, StringComparer.OrdinalIgnoreCase);
        var classSupers = UnrClassEffectPackageReader.ReadScriptClasses(lineageEffectPath)
            .Concat(UnrClassEffectPackageReader.ReadScriptClasses(Path.Combine(clientRoot, "system", "Engine.u")))
            .ToDictionary(x => x.ObjectName, x => x.SuperClassName, StringComparer.OrdinalIgnoreCase);
        var actionSets = UnrSkillVisualActionPackageReader.Read(skillVisualPath)
            .ToDictionary(x => (x.GroupName.ToLowerInvariant(), x.ObjectName.ToLowerInvariant()));
        var resourcePackageIndex = ScenePackageIndexer.BuildResourcePackageIndex(clientRoot);
        var staticMeshResolver = new SceneStaticMeshResolver(clientRoot, new BspTextureManager(clientRoot));

        var effects = new List<SceneSkillVisualEffectData>();
        var nextStageOrder = 0;
        foreach (var token in levels.Select(x => x.DescriptionToken).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var parts = token!.Split('.');
            if (!parts[0].Is("skill"))
            {
                continue;
            }

            if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
            {
                throw new PackageReadException($"Invalid skill visual reference '{token}' in skillgrp.dat.");
            }

            if (!actionSets.TryGetValue((parts[1].ToLowerInvariant(), parts[2].ToLowerInvariant()), out var actionSet))
            {
                throw new PackageReadException($"Skill visual '{token}' was not found in '{skillVisualPath}'.");
            }

            var stages = new List<SceneSkillVisualStageData>();
            foreach (var action in actionSet.Actions.OrderBy(x => ResolvePhase(x.Phase)).ThenBy(x => x.PhaseIndex))
            {
                stages.Add(ResolveActionStage(clientRoot, lineageEffectPath, token, action, actionSet.FlyingTime, nextStageOrder++, stagesByClass, classSupers, resourcePackageIndex, staticMeshResolver, warnings));
            }

            effects.Add(new SceneSkillVisualEffectData
            {
                Stem = $"{actionSet.GroupName}.{actionSet.ObjectName}",
                Stages = stages
            });
        }

        if (effects.Count == 0)
        {
            throw new PackageReadException("No skill.<group>.<object> visual reference was present in skillgrp.dat for the requested skill.");
        }

        return effects;
    }

    private static SceneSkillVisualStageData ResolveActionStage(
        string clientRoot,
        string lineageEffectPath,
        string visualReference,
        UnrSkillVisualAction action,
        float? flyingTime,
        int stageOrder,
        IReadOnlyDictionary<string, UnrSkillEffectStageObject> stagesByClass,
        IReadOnlyDictionary<string, string?> classSupers,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        SceneStaticMeshResolver staticMeshResolver,
        ICollection<string> warnings)
    {
        if (!action.ActionClassName.Is("SkillAction_LocateEffect"))
        {
            throw new PackageReadException($"Unsupported skill action '{action.ActionClassName}' in '{visualReference}'.");
        }

        if (string.IsNullOrWhiteSpace(action.EffectClassName))
        {
            throw new PackageReadException($"Action {action.ExportIndex} in '{visualReference}' has no EffectClass.");
        }

        if (!action.EffectPackageName.Is("LineageEffect"))
        {
            throw new PackageReadException($"Effect class '{action.EffectPackageName}.{action.EffectClassName}' in '{visualReference}' is outside LineageEffect.u.");
        }

        if (!stagesByClass.TryGetValue(action.EffectClassName, out var rawStage))
        {
            throw new PackageReadException($"Effect class '{action.EffectClassName}' referenced by '{visualReference}' was not parsed from LineageEffect.u.");
        }

        return AdaptStage(clientRoot, lineageEffectPath, visualReference, rawStage, action, stageOrder, IsProjectile(rawStage.ObjectName, classSupers), flyingTime, resourcePackageIndex, staticMeshResolver, warnings);
    }

    private static SceneSkillVisualStageData AdaptStage(
        string clientRoot,
        string lineageEffectPath,
        string visualReference,
        UnrSkillEffectStageObject stage,
        UnrSkillVisualAction action,
        int stageOrder,
        bool isProjectile,
        float? flyingTime,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        SceneStaticMeshResolver staticMeshResolver,
        ICollection<string> warnings)
    {
        var packageName = Path.GetFileNameWithoutExtension(lineageEffectPath);
        var stageClassName = stage.SuperClassName ?? stage.DeclaredClassName;
        var stageReference = new SceneResourceReference
        {
            Reference = $"{packageName}.{stage.ObjectName}",
            ClassName = stageClassName,
            PackageName = packageName,
            ObjectName = stage.ObjectName
        };
        var stageResource = SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            lineageEffectPath,
            packageName,
            stage.ObjectName,
            stageClassName);

        var layers = stage.Layers
            .Select(x => AdaptLayer(clientRoot, lineageEffectPath, x, resourcePackageIndex, staticMeshResolver, warnings))
            .ToArray();

        return new SceneSkillVisualStageData
        {
            StageOrder = stageOrder,
            ObjectName = stage.ObjectName,
            SuperClassName = stage.SuperClassName,
            IsProjectile = isProjectile,
            Placement = new SceneSkillVisualPlacementData
            {
                VisualReference = visualReference,
                Phase = ResolvePhase(action.Phase),
                SpecificStage = action.SpecificStage,
                AttachOn = ResolveAttachMethod(action.AttachOn),
                AttachBoneName = action.AttachBoneName,
                Offset = action.Offset ?? default,
                SpawnOnTarget = action.SpawnOnTarget ?? false,
                RelativeToCylinder = action.RelativeToCylinder ?? false,
                UseCharacterRotation = action.UseCharacterRotation ?? false,
                Absolute = action.Absolute ?? false,
                OnMultiTarget = action.OnMultiTarget ?? false,
                SizeScale = action.SizeScale ?? false,
                SpawnDelay = action.SpawnDelay ?? 0f,
                FlyingTime = flyingTime
            },
            StageReference = stageReference,
            StageResource = stageResource,
            EmitterReferences = layers.Select(x => x.LayerReference).ToArray(),
            EmitterResources = layers.Select(x => x.LayerResource).ToArray(),
            Layers = layers
        };
    }

    private static bool IsProjectile(string className, IReadOnlyDictionary<string, string?> classSupers)
    {
        var current = className;
        for (var i = 0; i < 64; i++)
        {
            if (current.Equals("NSkillProjectile", StringComparison.OrdinalIgnoreCase) ||
                current.Equals("NProjectile", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!classSupers.TryGetValue(current, out var parent) || string.IsNullOrWhiteSpace(parent))
            {
                return false;
            }

            current = parent;
        }

        throw new PackageReadException($"Skill class inheritance cycle near '{className}'.");
    }

    private static SceneSkillVisualPhase ResolvePhase(UnrSkillVisualActionPhase phase)
    {
        return phase switch
        {
            UnrSkillVisualActionPhase.CastingActions => SceneSkillVisualPhase.Casting,
            UnrSkillVisualActionPhase.ChannelingActions => SceneSkillVisualPhase.Channeling,
            UnrSkillVisualActionPhase.PreshotActions => SceneSkillVisualPhase.Preshot,
            UnrSkillVisualActionPhase.ShotActions => SceneSkillVisualPhase.Shot,
            UnrSkillVisualActionPhase.ExplosionActions => SceneSkillVisualPhase.Explosion,
            _ => throw new PackageReadException($"Unsupported skill visual action phase '{phase}'.")
        };
    }

    private static SceneSkillEffectAttachMethod ResolveAttachMethod(byte? raw)
    {
        var value = raw ?? 0;
        if (value > (byte)SceneSkillEffectAttachMethod.LeftFoot)
        {
            throw new PackageReadException($"Unsupported SkillAction AttachOn={value}.");
        }

        return (SceneSkillEffectAttachMethod)value;
    }

    private static SceneSkillVisualLayerData AdaptLayer(
        string clientRoot,
        string lineageEffectPath,
        UnrSkillEffectLayerObject layer,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        SceneStaticMeshResolver staticMeshResolver,
        ICollection<string> warnings)
    {
        var packageName = Path.GetFileNameWithoutExtension(lineageEffectPath);
        var layerReference = new SceneResourceReference
        {
            Reference = $"{packageName}.{layer.ObjectName}",
            ClassName = layer.ClassName,
            PackageName = packageName,
            ObjectName = layer.ObjectName
        };
        var layerResource = SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            lineageEffectPath,
            packageName,
            layer.ObjectName,
            layer.ClassName);

        var staticMeshReference = ToReferenceText(layer.StaticMeshReference);
        var textureReference = ToReferenceText(layer.TextureReference);
        var staticMeshResourceReference = TryBuildResourceReference(staticMeshReference, UnrealClassNames.StaticMesh);
        var meshParts = BuildMeshParts(lineageEffectPath, layer.StaticMeshReference, staticMeshResolver);
        var textureResourceReference = TryBuildResourceReference(textureReference, UnrealClassNames.Texture);

        return new SceneSkillVisualLayerData
        {
            ExportIndex = layer.ExportIndex,
            ObjectName = layer.ObjectName,
            ClassName = layer.ClassName,
            LayerName = layer.LayerName,
            LayerReference = layerReference,
            LayerResource = layerResource,
            StaticMeshReference = staticMeshReference,
            StaticMeshResourceReference = staticMeshResourceReference,
            StaticMeshResource = TryResolveResourceLocation(staticMeshResourceReference, resourcePackageIndex, clientRoot, warnings),
            MeshParts = meshParts,
            TextureReference = textureReference,
            DrawStyle = layer.DrawStyle ?? 3,
            UseMeshBlendMode = layer.UseMeshBlendMode,
            TextureUSubdivisions = layer.TextureUSubdivisions,
            TextureVSubdivisions = layer.TextureVSubdivisions,
            SubdivisionStart = layer.SubdivisionStart,
            SubdivisionEnd = layer.SubdivisionEnd,
            UseRandomSubdivision = layer.UseRandomSubdivision,
            BlendBetweenSubdivisions = layer.BlendBetweenSubdivisions,
            TextureResourceReference = textureResourceReference,
            TextureResource = TryResolveResourceLocation(textureResourceReference, resourcePackageIndex, clientRoot, warnings),
            Opacity = layer.Opacity,
            FadeOutStartTime = layer.FadeOutStartTime,
            FadeOut = layer.FadeOut,
            FadeInEndTime = layer.FadeInEndTime,
            FadeIn = layer.FadeIn,
            MaxParticles = layer.MaxParticles,
            LifetimeRange = layer.LifetimeRange,
            Acceleration = layer.Acceleration,
            StartLocationRange = layer.StartLocationRange,
            StartSizeRange = layer.StartSizeRange,
            StartVelocityRange = layer.StartVelocityRange,
            StartSpinRange = layer.StartSpinRange,
            SpinsPerSecondRange = layer.SpinsPerSecondRange,
            ColorScale = layer.ColorScale,
            SizeScale = layer.SizeScale
        };
    }

    private static IReadOnlyList<SceneSkillVisualMeshPartData> BuildMeshParts(
        string lineageEffectPath,
        UnrFileObjectReference? staticMeshReference,
        SceneStaticMeshResolver staticMeshResolver)
    {
        if (staticMeshReference is null)
        {
            return [];
        }

        var meshReferenceKey = SceneReferenceUtilities.BuildReference(lineageEffectPath, staticMeshReference);
        var resolved = staticMeshResolver.ResolveMany(lineageEffectPath, [staticMeshReference]);
        if (!resolved.TryGetValue(meshReferenceKey, out var meshDefinition))
        {
            throw new PackageReadException($"Static mesh '{meshReferenceKey}' was not resolved for skill mesh emitter.");
        }

        return meshDefinition.SubMeshes
            .OrderBy(x => x.SubMeshIndex)
            .Select(x => new SceneSkillVisualMeshPartData
            {
                SubMeshIndex = x.SubMeshIndex,
                MaterialId = x.MaterialId,
                TriangleCount = x.TriangleCount,
                MaterialReference = x.MaterialReference,
                MaterialResource = x.MaterialResource,
                PrimaryTextureReference = x.PrimaryTextureReference,
                PrimaryTextureResource = x.PrimaryTextureResource
            })
            .ToArray();
    }
    private static string? ToReferenceText(UnrFileObjectReference? reference)
    {
        if (reference is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(reference.PackageName)
            ? reference.ObjectName
            : $"{reference.PackageName}.{reference.ObjectName}";
    }

    private static SceneResourceReference? TryBuildResourceReference(string? reference, string className)
    {
        return string.IsNullOrWhiteSpace(reference)
            ? null
            : SceneReferenceUtilities.BuildFromDbResourceReference(reference, className);
    }

    private static SceneResourceLocation? TryResolveResourceLocation(
        SceneResourceReference? reference,
        IReadOnlyDictionary<string, string> resourcePackageIndex,
        string clientRoot,
        ICollection<string> warnings)
    {
        if (reference is null)
        {
            return null;
        }

        if (!resourcePackageIndex.TryGetValue(reference.PackageName, out var packagePath))
        {
            warnings.Add($"Package '{reference.PackageName}' was not found while resolving '{reference.Reference}'.");
            return null;
        }

        return SceneReferenceUtilities.BuildResourceLocation(
            clientRoot,
            packagePath,
            reference.PackageName,
            reference.ObjectName,
            reference.ClassName);
    }
}

