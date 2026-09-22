using L2Viewer.SceneDomain.Models;
using L2Viewer.SceneDomain.Services.BSPServices;
using L2Viewer.SceneDomain.Services.Utility;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneSkyEnvironmentBuilder
{
    public SceneSkyEnvironmentData Build(L2Viewer.UnrFile.UnrFile unr)
    {
        var lightingBuilder = new SceneLightingBuilder();
        var skyZones = BuildSkyZones(unr);
        var suns = lightingBuilder.BuildSuns(unr);
        var moons = lightingBuilder.BuildMoons(unr);
        var geometry = new SceneBspBuilder().BuildSky(unr);
        var layers = BuildLayers(geometry);
        var surfaceMaterials = BuildSkySurfaceMaterials(geometry);

        return new SceneSkyEnvironmentData
        {
            SkyZones = skyZones,
            Suns = suns,
            Moons = moons,
            Geometry = geometry,
            Layers = layers,
            SurfaceMaterials = surfaceMaterials,
            SourceReferences = BuildSourceReferences(skyZones, suns, moons, surfaceMaterials)
        };
    }

    public SceneSkyZoneData[] BuildSkyZones(L2Viewer.UnrFile.UnrFile unr)
    {
        return unr.ExportObjects
            .Select(x => x.Object)
            .OfType<UnrSkyZoneInfoObject>()
            .Select(x =>
            {
                var rotationRaw = x.Rotation;
                return new SceneSkyZoneData
                {
                    ExportIndex = x.ExportIndex,
                    StableName = SceneStableNameUtility.BuildActorStableName(unr, x),
                    Name = x.ObjectName,
                    ClassName = x.ClassName,
                    Tag = x.Tag,
                    ZoneNumber = x.Region?.ZoneNumber,
                    LeafIndex = x.Region?.LeafIndex,
                    WorldLocation = x.Location,
                    WorldRotationUnrealRaw = rotationRaw,
                    WorldRotationEulerDegrees = rotationRaw is null ? null : SceneTransformUtilities.UnrealRotatorToEulerDegrees(rotationRaw.Value),
                    StaticMeshReference = BuildMapReference(unr, x.StaticMeshReference),
                    MeshReference = BuildMapReference(unr, x.MeshReference),
                    TextureReference = BuildMapReference(unr, x.TextureReference),
                    TexUPanSpeed = x.TexUPanSpeed,
                    TexVPanSpeed = x.TexVPanSpeed,
                    LensFlareReferences = x.LensFlare.Select(reference => BuildMapReference(unr, reference)).Where(x => x is not null).Cast<string>().ToArray(),
                    LensFlareOffset = x.LensFlareOffset,
                    LensFlareScale = x.LensFlareScale
                };
            })
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SceneSkyLayerData[] BuildLayers(SceneBspScene geometry)
    {
        return geometry.Models
            .SelectMany(model => model.Chunks.SelectMany(chunk => chunk.MeshSections.Select(section => new SceneSkyLayerData
            {
                StableName = $"{model.StableName}_{chunk.StableName}_{section.StableName}",
                Kind = ClassifyLayer(section.MaterialObjectName ?? section.MaterialReference),
                ModelExportIndex = model.ExportIndex,
                ModelStableName = model.StableName,
                ChunkIndex = chunk.ChunkIndex,
                ChunkStableName = chunk.StableName,
                MaterialReference = section.MaterialReference,
                Geometry = section
            })))
            .OrderBy(x => x.ModelExportIndex)
            .ThenBy(x => x.ChunkIndex)
            .ThenBy(x => x.StableName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static SceneSkySurfaceMaterialData[] BuildSkySurfaceMaterials(SceneBspScene geometry)
    {
        return geometry.Models
            .SelectMany(model => model.Chunks.SelectMany(chunk => chunk.MeshSections.Select(section => new
            {
                Model = model,
                Section = section
            })))
            .GroupBy(
                x => $"{x.Model.ExportIndex}|{x.Section.MaterialReference}|{x.Section.PolyFlags}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new SceneSkySurfaceMaterialData
                {
                    ModelExportIndex = first.Model.ExportIndex,
                    ModelName = first.Model.Name,
                    MaterialReference = first.Section.MaterialReference,
                    PolyFlags = first.Section.PolyFlags,
                    PolyFlagNames = first.Section.PolyFlagNames,
                    SurfaceCount = group.Sum(x => x.Section.SurfaceCount),
                    Environment = first.Section.KnownPolyFlags.HasFlag(UnrPolyFlags.Environment),
                    FakeBackdrop = first.Section.KnownPolyFlags.HasFlag(UnrPolyFlags.FakeBackdrop),
                    Unlit = first.Section.KnownPolyFlags.HasFlag(UnrPolyFlags.Unlit)
                };
            })
            .OrderBy(x => x.ModelExportIndex)
            .ThenBy(x => x.MaterialReference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PolyFlags)
            .ToArray();
    }

    private static SceneSkyLayerKind ClassifyLayer(string? materialName)
    {
        if (string.IsNullOrWhiteSpace(materialName))
        {
            return SceneSkyLayerKind.Other;
        }

        if (materialName.Contains("cloud", StringComparison.OrdinalIgnoreCase)) return SceneSkyLayerKind.Cloud;
        if (materialName.Contains("haze", StringComparison.OrdinalIgnoreCase)) return SceneSkyLayerKind.Haze;
        if (materialName.Contains("star", StringComparison.OrdinalIgnoreCase)) return SceneSkyLayerKind.Stars;
        if (materialName.Contains("sky", StringComparison.OrdinalIgnoreCase) ||
            materialName.Contains("background", StringComparison.OrdinalIgnoreCase)) return SceneSkyLayerKind.Background;
        return SceneSkyLayerKind.Other;
    }

    private static SceneSkySourceReferenceData[] BuildSourceReferences(
        SceneSkyZoneData[] skyZones,
        SceneSunData[] suns,
        SceneMoonData[] moons,
        SceneSkySurfaceMaterialData[] surfaceMaterials)
    {
        var references = new List<SkyReferenceCandidate>();
        references.AddRange(suns.SelectMany(x => x.SkinReferences.Select(reference => new SkyReferenceCandidate("SunSkin", reference, "Material"))));
        references.AddRange(moons.SelectMany(x => x.SkinReferences.Select(reference => new SkyReferenceCandidate("MoonSkin", reference, "Material"))));
        references.AddRange(moons.SelectMany(x => x.FlameReferences.Select(reference => new SkyReferenceCandidate("MoonFlame", reference, "Texture"))));
        references.AddRange(skyZones.SelectMany(x => x.LensFlareReferences.Select(reference => new SkyReferenceCandidate("LensFlare", reference, "Texture"))));
        references.AddRange(skyZones.Where(x => !string.IsNullOrWhiteSpace(x.TextureReference)).Select(x => new SkyReferenceCandidate("SkyZoneTexture", x.TextureReference!, "Texture")));
        references.AddRange(skyZones.Where(x => !string.IsNullOrWhiteSpace(x.MeshReference)).Select(x => new SkyReferenceCandidate("SkyZoneMesh", x.MeshReference!, "Mesh")));
        references.AddRange(skyZones.Where(x => !string.IsNullOrWhiteSpace(x.StaticMeshReference)).Select(x => new SkyReferenceCandidate("SkyZoneStaticMesh", x.StaticMeshReference!, "StaticMesh")));
        references.AddRange(surfaceMaterials.Select(x => new SkyReferenceCandidate("SkySurfaceMaterial", x.MaterialReference, "Material")));

        var result = new Dictionary<string, SceneSkySourceReferenceData>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in references)
        {
            if (!TrySplitPackageObjectReference(candidate.Reference, out var packageName, out var objectName))
            {
                continue;
            }

            var key = $"{candidate.Role}|{candidate.Reference}|{candidate.ClassHint}";
            result.TryAdd(key, new SceneSkySourceReferenceData
            {
                Role = candidate.Role,
                Reference = candidate.Reference,
                PackageName = packageName,
                ObjectName = objectName,
                ClassName = candidate.ClassHint,
                PackagePath = null,
                ClientRelativePath = null,
                Uri = null
            });
        }

        return result.Values
            .OrderBy(x => x.Role, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Reference, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? BuildMapReference(L2Viewer.UnrFile.UnrFile unr, UnrFileObjectReference? reference)
    {
        return reference is null ? null : SceneReferenceUtilities.BuildReference(unr.FilePath, reference);
    }

    private sealed record SkyReferenceCandidate(string Role, string Reference, string ClassHint);

    private static bool TrySplitPackageObjectReference(string reference, out string packageName, out string objectName)
    {
        packageName = string.Empty;
        objectName = string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var dot = reference.IndexOf('.');
        if (dot <= 0 || dot >= reference.Length - 1)
        {
            return false;
        }

        packageName = reference[..dot].Trim();
        objectName = reference[(dot + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(packageName) && !string.IsNullOrWhiteSpace(objectName);
    }

}
