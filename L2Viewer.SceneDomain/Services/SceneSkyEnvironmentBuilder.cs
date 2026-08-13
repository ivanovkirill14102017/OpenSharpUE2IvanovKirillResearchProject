using L2Viewer.SceneDomain.Models;
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
        var surfaceMaterials = BuildSkySurfaceMaterials(unr);

        return new SceneSkyEnvironmentData
        {
            SkyZones = skyZones,
            Suns = suns,
            Moons = moons,
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

    private static SceneSkySurfaceMaterialData[] BuildSkySurfaceMaterials(L2Viewer.UnrFile.UnrFile unr)
    {
        var surfaceGroups = new Dictionary<string, SkySurfaceMaterialAccumulator>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in unr.ExportObjects.Select(x => x.Object).OfType<UnrModelObject>())
        {
            foreach (var surface in model.Surfaces)
            {
                var materialReference = BuildMapReference(unr, surface.MaterialReference);
                if (materialReference is null)
                {
                    continue;
                }

                var knownFlags = surface.KnownPolyFlags;
                var environment = knownFlags.HasFlag(UnrPolyFlags.Environment);
                var fakeBackdrop = knownFlags.HasFlag(UnrPolyFlags.FakeBackdrop);
                var unlit = knownFlags.HasFlag(UnrPolyFlags.Unlit);
                if (!environment && !fakeBackdrop && !IsKnownClientSkyMaterialReference(materialReference))
                {
                    continue;
                }

                var key = $"{model.ExportIndex}|{materialReference}|{surface.PolyFlags}";
                if (!surfaceGroups.TryGetValue(key, out var accumulator))
                {
                    accumulator = new SkySurfaceMaterialAccumulator(
                        model.ExportIndex,
                        model.ObjectName,
                        materialReference,
                        surface.PolyFlags,
                        surface.PolyFlagNames,
                        environment,
                        fakeBackdrop,
                        unlit);
                    surfaceGroups.Add(key, accumulator);
                }

                accumulator.SurfaceCount++;
            }
        }

        return surfaceGroups.Values
            .Select(x => new SceneSkySurfaceMaterialData
            {
                ModelExportIndex = x.ModelExportIndex,
                ModelName = x.ModelName,
                MaterialReference = x.MaterialReference,
                PolyFlags = x.PolyFlags,
                PolyFlagNames = x.PolyFlagNames,
                SurfaceCount = x.SurfaceCount,
                Environment = x.Environment,
                FakeBackdrop = x.FakeBackdrop,
                Unlit = x.Unlit
            })
            .OrderBy(x => x.ModelExportIndex)
            .ThenBy(x => x.MaterialReference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PolyFlags)
            .ToArray();
    }

    private static SceneSkySourceReferenceData[] BuildSourceReferences(
        SceneSkyZoneData[] skyZones,
        SceneSunData[] suns,
        SceneMoonData[] moons,
        SceneSkySurfaceMaterialData[] surfaceMaterials)
    {
        var references = new List<SkyReferenceCandidate>();
        references.AddRange(suns.SelectMany(x => x.SkinReferences.Select(reference => new SkyReferenceCandidate("SunSkin", reference, "Texture"))));
        references.AddRange(moons.SelectMany(x => x.SkinReferences.Select(reference => new SkyReferenceCandidate("MoonSkin", reference, "Texture"))));
        references.AddRange(skyZones.SelectMany(x => x.LensFlareReferences.Select(reference => new SkyReferenceCandidate("LensFlare", reference, "Texture"))));
        references.AddRange(skyZones.Where(x => !string.IsNullOrWhiteSpace(x.TextureReference)).Select(x => new SkyReferenceCandidate("SkyZoneTexture", x.TextureReference!, "Texture")));
        references.AddRange(skyZones.Where(x => !string.IsNullOrWhiteSpace(x.MeshReference)).Select(x => new SkyReferenceCandidate("SkyZoneMesh", x.MeshReference!, "Mesh")));
        references.AddRange(skyZones.Where(x => !string.IsNullOrWhiteSpace(x.StaticMeshReference)).Select(x => new SkyReferenceCandidate("SkyZoneStaticMesh", x.StaticMeshReference!, "StaticMesh")));
        references.AddRange(surfaceMaterials.Select(x => new SkyReferenceCandidate("SkySurfaceMaterial", x.MaterialReference, "Texture")));

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

    private static bool IsKnownClientSkyMaterialReference(string materialReference)
    {
        var dotIndex = materialReference.IndexOf('.');
        if (dotIndex <= 0 || dotIndex >= materialReference.Length - 1)
        {
            return false;
        }

        var packageName = materialReference[..dotIndex];
        if (!string.Equals(packageName, "L2_Skies", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var objectName = materialReference[(dotIndex + 1)..];
        return string.Equals(objectName, "Cloud_Final", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "HazeRing_Final", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "SkybackgroundColor", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "StarField_Final01", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "StarField_Final02", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "WhiteCloud", StringComparison.OrdinalIgnoreCase);
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

    private sealed class SkySurfaceMaterialAccumulator
    {
        public SkySurfaceMaterialAccumulator(
            int modelExportIndex,
            string modelName,
            string materialReference,
            uint polyFlags,
            string[] polyFlagNames,
            bool environment,
            bool fakeBackdrop,
            bool unlit)
        {
            ModelExportIndex = modelExportIndex;
            ModelName = modelName;
            MaterialReference = materialReference;
            PolyFlags = polyFlags;
            PolyFlagNames = polyFlagNames;
            Environment = environment;
            FakeBackdrop = fakeBackdrop;
            Unlit = unlit;
        }

        public int ModelExportIndex { get; }
        public string ModelName { get; }
        public string MaterialReference { get; }
        public uint PolyFlags { get; }
        public string[] PolyFlagNames { get; }
        public bool Environment { get; }
        public bool FakeBackdrop { get; }
        public bool Unlit { get; }
        public int SurfaceCount { get; set; }
    }
}
