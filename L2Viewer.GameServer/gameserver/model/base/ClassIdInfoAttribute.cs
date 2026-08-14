namespace L2Viewer.GameServer.gameserver.model.@base;

[AttributeUsage(AttributeTargets.Field)]
public sealed class ClassIdInfoAttribute : Attribute
{
    public ClassIdInfoAttribute(L2Race race, int parentClassId, bool isMage)
    {
        Race = race;
        ParentClassId = parentClassId;
        IsMage = isMage;
    }

    public L2Race Race { get; }
    public int ParentClassId { get; }
    public bool IsMage { get; }
    public bool IsRoot => ParentClassId < 0;
}
