namespace L2Viewer.PackageCore;

public static class MathEx
{
    public static Vector3 Cross(Vector3 a, Vector3 b) => Vector3.Cross(a, b);

    public static float Dot(Vector3 a, Vector3 b) => Vector3.Dot(a, b);

    public static float Length(Vector3 a) => a.Length();

    public static Vector3 NormalizeSafe(Vector3 a)
    {
        if (a.LengthSquared() <= 1e-12f)
        {
            return Vector3.Zero;
        }

        return Vector3.Normalize(a);
    }

    public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        return 0.5f * Cross(b - a, c - a).Length();
    }
}
