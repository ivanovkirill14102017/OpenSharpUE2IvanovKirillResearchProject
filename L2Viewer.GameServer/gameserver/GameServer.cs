using L2Viewer.GameServer.gameserver.model;
using L2Viewer.GameServer.gameserver.model.@base;
using L2Viewer.GameServer.gameserver.model.actor.instance;

namespace L2Viewer.GameServer.gameserver;

public sealed class GameServer
{
    private readonly World _world;

    public GameServer(GameServerConfig? config = null)
    {
        Config = config ?? new GameServerConfig();
        _world = new World();
    }

    public GameServerConfig Config { get; }
    public World World => _world;

    public L2PcInstance CreateDefaultHumanMale(Location location)
    {
        var player = new L2PcInstance(_world.NextObjectId(), "Player", Config.PlayerCollisionRadius, Config.PlayerCollisionHeight)
        {
            ClassId = L2ClassId.HumanFighter,
            Sex = L2Sex.Male,
            RunSpeed = Config.PlayerRunSpeed,
            Location = location
        };
        _world.AddObject(player);
        return player;
    }

    public void Tick(float deltaSeconds)
    {
        _world.Tick(deltaSeconds, Config);
    }
}
