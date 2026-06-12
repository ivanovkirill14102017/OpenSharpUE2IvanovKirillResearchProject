using System.IO;
using HelixToolkit.Maths;
using HelixToolkit.Wpf.SharpDX;
using L2Viewer.PackageCore;
using L2Viewer.SceneDomain.Models;

namespace L2Viewer.Wpf;

internal sealed class SceneMaterialFactory
{
    private readonly Dictionary<string, Stream> _textureStreams = new(StringComparer.OrdinalIgnoreCase);

    public PhongMaterial DefaultMaterial { get; } = new()
    {
        DiffuseColor = Color4.White,
        SpecularColor = new Color4(0.08f, 0.08f, 0.08f, 1f),
        AmbientColor = new Color4(0.18f, 0.18f, 0.18f, 1f)
    };

    public PhongMaterial PropMaterial { get; } = new()
    {
        DiffuseColor = new Color4(0.96f, 0.5f, 0.18f, 1f),
        SpecularColor = new Color4(0.08f, 0.08f, 0.08f, 1f),
        AmbientColor = new Color4(0.28f, 0.18f, 0.10f, 1f)
    };

    public PhongMaterial PolyMaterial { get; } = new()
    {
        DiffuseColor = new Color4(0.8f, 0.8f, 0.8f, 1f),
        SpecularColor = new Color4(0.1f, 0.1f, 0.1f, 1f),
        AmbientColor = new Color4(0.2f, 0.2f, 0.2f, 1f)
    };

    public PhongMaterial CollisionMaterial { get; } = new()
    {
        DiffuseColor = new Color4(0.92f, 0.18f, 0.18f, 0.45f),
        AmbientColor = new Color4(0.35f, 0.08f, 0.08f, 0.45f),
        SpecularColor = new Color4(0.04f, 0.04f, 0.04f, 0.45f),
        RenderShadowMap = false
    };

    public PhongMaterial WaterMaterial { get; } = new()
    {
        DiffuseColor = new Color4(0.20f, 0.53f, 0.76f, 0.28f),
        AmbientColor = new Color4(0.10f, 0.24f, 0.35f, 0.28f),
        SpecularColor = new Color4(0.12f, 0.18f, 0.22f, 0.28f),
        RenderShadowMap = false
    };

    public PhongMaterial VolumeMaterial { get; } = new()
    {
        DiffuseColor = new Color4(0.80f, 0.40f, 0.80f, 0.20f),
        AmbientColor = new Color4(0.40f, 0.20f, 0.40f, 0.20f),
        SpecularColor = new Color4(0.12f, 0.12f, 0.12f, 0.20f),
        RenderShadowMap = false
    };

    public BspTextureManager? SceneTextureManager { get; set; }

    public PhongMaterial CreateMaterial(MeshData mesh)
    {
        var previewTexture = ResolvePreviewTexture(mesh);
        return previewTexture is null ? DefaultMaterial : CreateTexturedMaterial(previewTexture);
    }

    public PhongMaterial CreateGhostedMaterial(MeshData mesh)
    {
        var previewTexture = ResolvePreviewTexture(mesh);
        if (previewTexture is null)
        {
            return new PhongMaterial
            {
                DiffuseColor = new Color4(1f, 1f, 1f, 0.32f),
                SpecularColor = new Color4(0.04f, 0.04f, 0.04f, 0.32f),
                AmbientColor = new Color4(0.16f, 0.16f, 0.16f, 0.32f),
                RenderShadowMap = false
            };
        }

        var material = CreateTexturedMaterial(previewTexture);
        material.DiffuseColor = new Color4(1f, 1f, 1f, 0.32f);
        material.SpecularColor = new Color4(0.04f, 0.04f, 0.04f, 0.32f);
        material.AmbientColor = new Color4(0.16f, 0.16f, 0.16f, 0.32f);
        material.RenderShadowMap = false;
        return material;
    }

    public PhongMaterial CreateSkeletalDiagnosticMaterial()
    {
        return new PhongMaterial
        {
            DiffuseColor = new Color4(0.94f, 0.46f, 0.22f, 0.76f),
            AmbientColor = new Color4(0.30f, 0.14f, 0.08f, 0.76f),
            SpecularColor = new Color4(0.05f, 0.05f, 0.05f, 0.76f),
            ReflectiveColor = new Color4(0f, 0f, 0f, 0f),
            RenderShadowMap = false
        };
    }

    public PhongMaterial CreatePropMaterial(MeshData mesh)
    {
        return ResolvePreviewTexture(mesh) is not null ? CreateMaterial(mesh) : PropMaterial;
    }

    public PhongMaterial CreateBspMaterial(SceneBspMeshSection section)
    {
        var previewTexture = ResolvePreviewTexture(section);
        return previewTexture is null ? PolyMaterial : CreateTexturedMaterial(previewTexture);
    }

    public PhongMaterial CreateScenePropMaterial(SceneStaticMeshSubMeshDefinition subMesh)
    {
        var previewTexture = ResolvePreviewTexture(subMesh);
        return previewTexture is null ? PropMaterial : CreateTexturedMaterial(previewTexture);
    }

    public PhongMaterial CreateBrushPolyMaterial(SceneBrushPolySubMesh subMesh)
    {
        var previewTexture = ResolvePreviewTexture(subMesh);
        return previewTexture is null ? PolyMaterial : CreateTexturedMaterial(previewTexture);
    }

    public PhongMaterial CreateSceneStaticMeshMaterial(SceneStaticMeshDefinition mesh)
    {
        var subMesh = mesh.SubMeshes.OrderByDescending(x => x.TriangleCount).FirstOrDefault();
        if (subMesh is null)
        {
            return DefaultMaterial;
        }

        var previewTexture = ResolvePreviewTexture(subMesh);
        return previewTexture is null ? DefaultMaterial : CreateTexturedMaterial(previewTexture);
    }

    public void UpdateSharedShadowMap(bool renderShadowMap)
    {
        DefaultMaterial.RenderShadowMap = renderShadowMap;
        PropMaterial.RenderShadowMap = renderShadowMap;
        WaterMaterial.RenderShadowMap = renderShadowMap;
        CollisionMaterial.RenderShadowMap = renderShadowMap;
    }

    public PhongMaterial CloneForScene(PhongMaterial source)
    {
        return new PhongMaterial
        {
            DiffuseMap = source.DiffuseMap,
            DiffuseColor = source.DiffuseColor,
            AmbientColor = source.AmbientColor,
            SpecularColor = source.SpecularColor,
            ReflectiveColor = source.ReflectiveColor,
            EmissiveColor = source.EmissiveColor,
            NormalMap = source.NormalMap,
            DisplacementMap = source.DisplacementMap,
            DiffuseAlphaMap = source.DiffuseAlphaMap,
            RenderShadowMap = source.RenderShadowMap,
            UVTransform = source.UVTransform
        };
    }

    public TextureData? ResolvePreviewTexture(SceneBspMeshSection part)
    {
        var graphTexture = part.Material?.TextureSlots
            .FirstOrDefault(x => string.Equals(x.Reference, part.PrimaryTextureReference, StringComparison.OrdinalIgnoreCase))?.Texture
            ?? part.Material?.TextureSlots.FirstOrDefault(x => x.Texture is not null)?.Texture;
        if (graphTexture is not null)
        {
            return graphTexture;
        }

        if (SceneTextureManager is null ||
            string.IsNullOrWhiteSpace(part.MaterialPackageName) ||
            string.IsNullOrWhiteSpace(part.MaterialObjectName))
        {
            return null;
        }

        return ResolveTexture(part.MaterialPackageName, part.MaterialObjectName);
    }

    private TextureData? ResolvePreviewTexture(MeshData mesh)
    {
        if (SceneTextureManager is null)
        {
            return null;
        }

        var reference = mesh.TextureRef ?? mesh.UsedTextures.FirstOrDefault()?.Reference;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var parts = reference.Split('.', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        return ResolveTexture(parts[0], parts[1]);
    }

    private TextureData? ResolvePreviewTexture(SceneStaticMeshSubMeshDefinition subMesh)
    {
        var graphTexture = subMesh.Material?.TextureSlots
            .FirstOrDefault(x => string.Equals(x.Reference, subMesh.PrimaryTextureReference, StringComparison.OrdinalIgnoreCase))?.Texture
            ?? subMesh.Material?.TextureSlots.FirstOrDefault(x => x.Texture is not null)?.Texture;
        if (graphTexture is not null)
        {
            return graphTexture;
        }

        if (SceneTextureManager is null)
        {
            return null;
        }

        var primaryResource = subMesh.PrimaryTextureResource ?? subMesh.TextureResources.FirstOrDefault();
        if (primaryResource is null)
        {
            return null;
        }

        return ResolveTexture(primaryResource.PackageName, primaryResource.ObjectName);
    }

    private TextureData? ResolvePreviewTexture(SceneBrushPolySubMesh subMesh)
    {
        var graphTexture = subMesh.Material?.TextureSlots
            .FirstOrDefault(x => string.Equals(x.Reference, subMesh.PrimaryTextureReference, StringComparison.OrdinalIgnoreCase))?.Texture
            ?? subMesh.Material?.TextureSlots.FirstOrDefault(x => x.Texture is not null)?.Texture;
        if (graphTexture is not null)
        {
            return graphTexture;
        }

        if (SceneTextureManager is null || subMesh.PrimaryTextureResource is null)
        {
            return null;
        }

        return ResolveTexture(subMesh.PrimaryTextureResource.PackageName, subMesh.PrimaryTextureResource.ObjectName);
    }

    private TextureData? ResolveTexture(string packageName, string objectName)
    {
        if (SceneTextureManager is null)
        {
            return null;
        }

        return SceneTextureManager.ResolveMany([new SceneTextureRequest(packageName, objectName)])
            .TryGetValue($"{packageName}.{objectName}", out var resolved)
            ? resolved.Texture
            : null;
    }

    private PhongMaterial CreateTexturedMaterial(TextureData previewTexture)
    {
        var key = $"{previewTexture.SourcePackage}|{previewTexture.Name}|{previewTexture.Width}|{previewTexture.Height}";
        if (!_textureStreams.TryGetValue(key, out var stream))
        {
            stream = new MemoryStream(ScenePreviewGeometry.EncodePng(previewTexture));
            _textureStreams[key] = stream;
        }

        stream.Position = 0;
        return new PhongMaterial
        {
            DiffuseMap = stream,
            DiffuseColor = Color4.White,
            SpecularColor = new Color4(0.06f, 0.06f, 0.06f, 1f),
            AmbientColor = new Color4(0.22f, 0.22f, 0.22f, 1f)
        };
    }
}
