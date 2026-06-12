using L2Viewer.SceneDomain.Models;

namespace L2Viewer.SceneDomain.Services;

public sealed class SceneBspBuilder
{
    private readonly SceneBspRoomBuilder _inner;

    public SceneBspBuilder()
    {
        _inner = new SceneBspRoomBuilder();
    }

    public SceneBspBuilder(string clientRoot, SceneMaterialResolver materialResolver, BspTextureManager textureManager)
    {
        _inner = new SceneBspRoomBuilder(clientRoot, materialResolver, textureManager);
    }

    public SceneBspScene Load(string path)
    {
        return _inner.Load(path);
    }

    public SceneBspScene Build(L2Viewer.UnrFile.UnrFile unr)
    {
        return _inner.Build(unr);
    }
}
