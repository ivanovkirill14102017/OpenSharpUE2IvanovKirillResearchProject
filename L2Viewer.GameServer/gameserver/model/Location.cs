namespace L2Viewer.GameServer.gameserver.model;

public readonly struct Location
{
    public Location(float x, float y, float z, float heading = 0f)
    {
        X = x;
        Y = y;
        Z = z;
        Heading = heading;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float Heading { get; }

    public Location WithPosition(float x, float y, float z)
    {
        return new Location(x, y, z, Heading);
    }

    public Location WithHeading(float heading)
    {
        return new Location(X, Y, Z, heading);
    }
}
