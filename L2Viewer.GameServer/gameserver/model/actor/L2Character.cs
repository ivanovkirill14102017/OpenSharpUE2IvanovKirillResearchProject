using L2Viewer.GameServer.gameserver.model;

namespace L2Viewer.GameServer.gameserver.model.actor;

public abstract class L2Character : L2Object
{
    private Location? _moveTarget;

    protected L2Character(int objectId, string name, float collisionRadius, float collisionHeight)
        : base(objectId, name)
    {
        CollisionRadius = collisionRadius;
        CollisionHeight = collisionHeight;
    }

    public float CollisionRadius { get; }
    public float CollisionHeight { get; }
    public float RunSpeed { get; set; }
    public bool IsMoving => _moveTarget.HasValue;
    public bool IsRunning => IsMoving;
    public Location? MoveTarget => _moveTarget;

    public void MoveTo(Location target)
    {
        _moveTarget = target;
    }

    public void StopMove()
    {
        _moveTarget = null;
    }
}
