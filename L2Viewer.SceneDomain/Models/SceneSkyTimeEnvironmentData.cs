namespace L2Viewer.SceneDomain.Models;

public enum SceneSkyTimeLayerRole
{
    SkyBox,
    HazeRing,
    Cloud1,
    Cloud2,
    Cloud3
}

public sealed record SceneSkyTimeColorKey(float Hour, byte Red, byte Green, byte Blue);
public sealed record SceneSkyTimeColorTrack(SceneSkyTimeLayerRole Role, string MaterialName, SceneSkyTimeColorKey[] Keys);
public sealed record SceneSkyTimeProfile(int EnvironmentType, SceneSkyTimeColorTrack[] Tracks);
public sealed record SceneSkyTimeEnvironmentData(SceneSkyTimeProfile[] Profiles);
