using L2Viewer.GameServer.gameserver.model.actor;
using L2Viewer.GameServer.gameserver.model.movement;

namespace L2Viewer.GameServer.gameserver.model;

public sealed class World
{
    private readonly Dictionary<int, L2Object> _objects = new();
    private readonly CharacterMovementService _movementService = new();
    private int _nextObjectId = 1;

    public IReadOnlyCollection<L2Object> Objects => _objects.Values;

    public int NextObjectId()
    {
        return _nextObjectId++;
    }

    public void AddObject(L2Object obj)
    {
        _objects[obj.ObjectId] = obj;
    }

    public bool TryGetObject(int objectId, out L2Object? obj)
    {
        return _objects.TryGetValue(objectId, out obj);
    }

    public void Tick(float deltaSeconds, GameServerConfig config)
    {
        foreach (var obj in _objects.Values)
        {
            if (obj is L2Character character)
            {
                _movementService.UpdateMovement(character, deltaSeconds, config);
            }
        }
    }
}
