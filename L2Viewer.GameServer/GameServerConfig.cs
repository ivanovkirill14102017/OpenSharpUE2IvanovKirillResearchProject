namespace L2Viewer.GameServer;

public sealed class GameServerConfig
{
    public float PlayerCollisionRadius { get; set; } = 0.35f;
    public float PlayerCollisionHeight { get; set; } = 2f;
    public float PlayerRunSpeed { get; set; } = 4.5f;
    public float StopDistance { get; set; } = 0.08f;
}
