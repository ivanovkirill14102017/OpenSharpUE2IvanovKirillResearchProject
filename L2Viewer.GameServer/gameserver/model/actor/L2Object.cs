using L2Viewer.GameServer.gameserver.model;

namespace L2Viewer.GameServer.gameserver.model.actor;

public abstract class L2Object
{
    protected L2Object(int objectId, string name)
    {
        ObjectId = objectId;
        Name = name;
    }

    public int ObjectId { get; }
    public string Name { get; protected set; }
    public Location Location { get; set; }
}
