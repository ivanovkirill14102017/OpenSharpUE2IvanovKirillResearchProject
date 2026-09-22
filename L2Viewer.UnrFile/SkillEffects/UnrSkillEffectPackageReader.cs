namespace L2Viewer.UnrFile;

public static class UnrSkillEffectPackageReader
{
    private static readonly string[] OrderedStageKeys = ["pr", "ca", "cs", "co", "fl", "ta", "to", "0", "1", "2", "3"];

    public static IReadOnlyList<UnrSkillEffectStageObject> ReadStages(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Skill effect package was not found.", path);
        }

        var stages = new List<UnrSkillEffectStageObject>();
        foreach (var emitterClass in UnrClassEffectPackageReader.ReadEmitterClasses(path))
        {
            if (!TryExtractStageMetadata(emitterClass.ObjectName, out var stageKey, out var stageOrder))
            {
                continue;
            }

            var childLayers = emitterClass.Layers
                .Select(TryAdaptLayer)
                .Where(static x => x is not null)
                .Cast<UnrSkillEffectLayerObject>()
                .ToArray();

            if (childLayers.Length == 0)
            {
                continue;
            }

            stages.Add(new UnrSkillEffectStageObject
            {
                ObjectName = emitterClass.ObjectName,
                DeclaredClassName = nameof(UnrEmitterClassObject),
                SuperClassName = emitterClass.SuperClassName,
                StageKey = stageKey,
                StageOrder = stageOrder,
                Layers = childLayers
            });
        }

        return stages
            .OrderBy(x => x.StageOrder)
            .ThenBy(x => x.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static UnrSkillEffectLayerObject? TryAdaptLayer(UnrFileObject layer)
    {
        return layer switch
        {
            UnrSpriteEmitterObject sprite => FromSpriteEmitter(sprite),
            UnrMeshEmitterObject mesh => FromMeshEmitter(mesh),
            UnrBeamEmitterObject beam => FromBeamEmitter(beam),
            UnrVertMeshEmitterObject vertMesh => FromVertMeshEmitter(vertMesh),
            _ => null
        };
    }

    private static UnrSkillEffectLayerObject FromSpriteEmitter(UnrSpriteEmitterObject layer)
    {
        return new UnrSkillEffectLayerObject
        {
            ExportIndex = layer.ExportIndex,
            ObjectName = layer.ObjectName,
            ClassName = layer.ClassName,
            LayerName = layer.NameValue,
            StaticMeshReference = null,
            TextureReference = layer.TextureReference,
            DrawStyle = layer.DrawStyle,
            TextureUSubdivisions = layer.TextureUSubdivisions,
            TextureVSubdivisions = layer.TextureVSubdivisions,
            SubdivisionStart = layer.SubdivisionStart,
            SubdivisionEnd = layer.SubdivisionEnd,
            UseRandomSubdivision = layer.UseRandomSubdivision,
            BlendBetweenSubdivisions = layer.BlendBetweenSubdivisions,
            Opacity = layer.Opacity,
            FadeOutStartTime = layer.FadeOutStartTime,
            FadeOut = layer.FadeOut,
            FadeInEndTime = null,
            FadeIn = false,
            MaxParticles = layer.MaxParticles,
            LifetimeRange = layer.LifetimeRange,
            Acceleration = layer.Acceleration,
            StartLocationRange = null,
            StartSizeRange = layer.StartSizeRange,
            StartVelocityRange = layer.StartVelocityRange,
            StartSpinRange = layer.StartSpinRange,
            SpinsPerSecondRange = layer.SpinsPerSecondRange,
            ColorScale = layer.ColorScale,
            SizeScale = layer.SizeScale
        };
    }

    private static UnrSkillEffectLayerObject FromMeshEmitter(UnrMeshEmitterObject layer)
    {
        return new UnrSkillEffectLayerObject
        {
            ExportIndex = layer.ExportIndex,
            ObjectName = layer.ObjectName,
            ClassName = layer.ClassName,
            LayerName = layer.NameValue,
            StaticMeshReference = layer.StaticMeshReference,
            TextureReference = null,
            DrawStyle = layer.DrawStyle,
            UseMeshBlendMode = layer.UseMeshBlendMode,
            Opacity = layer.Opacity,
            FadeOutStartTime = layer.FadeOutStartTime,
            FadeOut = layer.FadeOut,
            FadeInEndTime = layer.FadeInEndTime,
            FadeIn = layer.FadeIn,
            MaxParticles = layer.MaxParticles,
            LifetimeRange = layer.LifetimeRange,
            Acceleration = null,
            StartLocationRange = null,
            StartSizeRange = layer.StartSizeRange,
            StartVelocityRange = layer.StartVelocityRange,
            StartSpinRange = layer.StartSpinRange,
            SpinsPerSecondRange = layer.SpinsPerSecondRange,
            ColorScale = layer.ColorScale,
            SizeScale = layer.UseSizeScale ? layer.SizeScale : []
        };
    }

    private static UnrSkillEffectLayerObject FromBeamEmitter(UnrBeamEmitterObject layer)
    {
        return new UnrSkillEffectLayerObject
        {
            ExportIndex = layer.ExportIndex,
            ObjectName = layer.ObjectName,
            ClassName = layer.ClassName,
            LayerName = layer.NameValue,
            StaticMeshReference = layer.StaticMeshReference,
            TextureReference = layer.TextureReference,
            Opacity = layer.Opacity,
            FadeOutStartTime = layer.FadeOutStartTime,
            FadeOut = layer.FadeOut,
            FadeInEndTime = layer.FadeInEndTime,
            FadeIn = layer.FadeIn,
            MaxParticles = layer.MaxParticles,
            LifetimeRange = layer.LifetimeRange,
            Acceleration = null,
            StartLocationRange = layer.StartLocationRange,
            StartSizeRange = layer.StartSizeRange,
            StartVelocityRange = null,
            StartSpinRange = null,
            SpinsPerSecondRange = null,
            ColorScale = layer.ColorScale,
            SizeScale = []
        };
    }

    private static UnrSkillEffectLayerObject FromVertMeshEmitter(UnrVertMeshEmitterObject layer)
    {
        return new UnrSkillEffectLayerObject
        {
            ExportIndex = layer.ExportIndex,
            ObjectName = layer.ObjectName,
            ClassName = layer.ClassName,
            LayerName = layer.NameValue,
            StaticMeshReference = layer.StaticMeshReference,
            TextureReference = layer.TextureReference,
            DrawStyle = layer.DrawStyle,
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
            SpinsPerSecondRange = null,
            ColorScale = layer.ColorScale,
            SizeScale = []
        };
    }

    private static bool TryExtractStageMetadata(string objectName, out string stageKey, out int stageOrder)
    {
        stageKey = string.Empty;
        stageOrder = int.MaxValue;
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        var suffixIndex = objectName.LastIndexOf('_');
        if (suffixIndex <= 0 || suffixIndex >= objectName.Length - 1)
        {
            return false;
        }

        stageKey = objectName[(suffixIndex + 1)..];
        stageOrder = GetStageOrder(stageKey);
        return stageOrder != int.MaxValue;
    }

    private static int GetStageOrder(string stageKey)
    {
        for (var i = 0; i < OrderedStageKeys.Length; i++)
        {
            if (OrderedStageKeys[i].Is(stageKey))
            {
                return i;
            }
        }

        return int.TryParse(stageKey, out var numericStage)
            ? OrderedStageKeys.Length + numericStage
            : int.MaxValue;
    }
}
