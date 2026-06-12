namespace L2Viewer.SceneDomain.Services;

public static class SceneTransformUtilities
{
    private const float UnrealUnitsToDegrees = 360f / 65536f;

    public static Vector3 ComputeActorScale(UnrActorBaseObject actor)
    {
        return actor.DrawScale3D * actor.DrawScale;
    }

    public static Vector3 UnrealRotatorToEulerDegrees(Vector3 unrealRotator)
    {
        return new Vector3(
            unrealRotator.X * UnrealUnitsToDegrees,
            unrealRotator.Y * UnrealUnitsToDegrees,
            unrealRotator.Z * UnrealUnitsToDegrees);
    }

    public static Vector3 RotateByUnrealRotator(Vector3 vector, Vector3 rot)
    {
        var pitch = rot.X * (float)(Math.PI / 32768.0);
        var yaw = rot.Y * (float)(Math.PI / 32768.0);
        var roll = rot.Z * (float)(Math.PI / 32768.0);

        var cp = MathF.Cos(pitch);
        var sp = MathF.Sin(pitch);
        var cy = MathF.Cos(yaw);
        var sy = MathF.Sin(yaw);
        var cr = MathF.Cos(roll);
        var sr = MathF.Sin(roll);

        var forward = new Vector3(cp * cy, cp * sy, -sp);
        var right = new Vector3(-sr * sp * cy + cr * sy, -sr * sp * sy - cr * cy, -sr * cp);
        var up = new Vector3(cr * sp * cy + sr * sy, cr * sp * sy - sr * cy, cr * cp);
        return new Vector3(
            forward.X * vector.X - right.X * vector.Y + up.X * vector.Z,
            forward.Y * vector.X - right.Y * vector.Y + up.Y * vector.Z,
            forward.Z * vector.X - right.Z * vector.Y + up.Z * vector.Z);
    }
}
