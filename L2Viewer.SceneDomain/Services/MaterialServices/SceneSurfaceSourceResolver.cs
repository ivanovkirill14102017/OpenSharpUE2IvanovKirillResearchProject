using L2Viewer.SceneDomain.Models;
using L2Viewer.UtxFile;

namespace L2Viewer.SceneDomain.Services.MaterialServices;

public sealed class SceneSurfaceSourceResolver
{
    private readonly string _clientRoot;
    private readonly BspTextureManager _textureManager;
    private readonly SceneMaterialResolver _materialResolver;

    public SceneSurfaceSourceResolver(string clientRoot)
    {
        _clientRoot = clientRoot;
        _textureManager = new BspTextureManager(clientRoot);
        _materialResolver = new SceneMaterialResolver(clientRoot, _textureManager);
    }

    public SceneSurfaceSourceResource Resolve(SceneSurfaceResourceReference reference)
    {
        if (TryResolve(reference, out var resource))
        {
            return resource;
        }

        throw new InvalidOperationException($"Surface '{reference}' did not resolve to an Unreal texture.");
    }

    public bool TryResolve(
        SceneSurfaceResourceReference reference,
        out SceneSurfaceSourceResource resource)
    {
        resource = null!;
        var graphs = _materialResolver.ResolveMany(
            _clientRoot,
            [new SceneMaterialRequest(reference.Id.PackageName, reference.Id.ObjectPath)]);
        var key = reference.ToString();
        if (!graphs.TryGetValue(key, out var graph) || graph is null)
        {
            return false;
        }

        var slot = MaterialTextureSlotOrdering.GetPreferredTextureSlot(graph.TextureSlots);
        if (slot is null)
        {
            return false;
        }

        var textureReference = SceneSurfaceResourceReference.Create(slot.PackageName, slot.ObjectName);
        var texture = slot.Texture;
        if (texture is null)
        {
            var textures = _textureManager.ResolveMany(
                [new SceneTextureRequest(slot.PackageName, slot.ObjectName)]);
            if (!textures.TryGetValue(textureReference.ToString(), out var resolved))
            {
                return false;
            }

            texture = resolved.Texture;
        }

        resource = new SceneSurfaceSourceResource(reference, graph.RootClass, textureReference, texture);
        return true;
}
}

public sealed record SceneSurfaceSourceResource(
    SceneSurfaceResourceReference SurfaceReference,
    MaterialGraphRootClass RootClass,
    SceneSurfaceResourceReference TextureReference,
    TextureData Texture);
