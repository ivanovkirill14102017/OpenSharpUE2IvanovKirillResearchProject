using L2Viewer.SceneDomain.Models;
using L2Viewer.UtxFile;

namespace L2Viewer.SceneDomain.Services.MaterialServices;

public sealed class SceneTextureSourceResolver
{
    private readonly BspTextureManager _legacyTextureManager;

    public SceneTextureSourceResolver(string clientRoot)
    {
        _legacyTextureManager = new BspTextureManager(clientRoot);
    }

    public SceneTextureSourceResource Resolve(SceneSurfaceResourceReference reference)
    {
        var key = reference.ToString();
        var resolved = _legacyTextureManager.ResolveMany(
            [new SceneTextureRequest(reference.Id.PackageName, reference.Id.ObjectPath)]);
        var texture = resolved[key];
        return new SceneTextureSourceResource(reference, texture.Texture);
    }
}

public sealed record SceneTextureSourceResource(
    SceneSurfaceResourceReference Reference,
    TextureData Texture);
