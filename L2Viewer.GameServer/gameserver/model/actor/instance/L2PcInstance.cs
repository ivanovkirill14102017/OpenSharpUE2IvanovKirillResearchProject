using L2Viewer.GameServer.gameserver.model.@base;

namespace L2Viewer.GameServer.gameserver.model.actor.instance;

public sealed class L2PcInstance : L2Character
{
    public L2PcInstance(int objectId, string name, float collisionRadius, float collisionHeight)
        : base(objectId, name, collisionRadius, collisionHeight)
    {
    }

    public L2ClassId ClassId { get; set; }
    public L2Sex Sex { get; set; }
    public L2Race Race => ClassId.GetRace();
    public L2ClassId RootClassId => ClassId.GetRootClassId();
}
