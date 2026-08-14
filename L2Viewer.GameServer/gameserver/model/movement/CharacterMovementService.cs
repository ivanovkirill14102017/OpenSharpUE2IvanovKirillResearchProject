using L2Viewer.GameServer.gameserver.model.actor;

namespace L2Viewer.GameServer.gameserver.model.movement;

public sealed class CharacterMovementService
{
    public void UpdateMovement(L2Character character, float deltaSeconds, GameServerConfig config)
    {
        if (character == null || !character.MoveTarget.HasValue || deltaSeconds <= 0f)
        {
            return;
        }

        var target = character.MoveTarget.Value;
        var dx = target.X - character.Location.X;
        var dz = target.Z - character.Location.Z;
        var distance = MathF.Sqrt(dx * dx + dz * dz);
        if (distance <= config.StopDistance)
        {
            character.Location = character.Location.WithPosition(target.X, character.Location.Y, target.Z);
            character.StopMove();
            return;
        }

        var step = MathF.Min(distance, MathF.Max(0f, character.RunSpeed) * deltaSeconds);
        var nx = dx / distance;
        var nz = dz / distance;
        var heading = MathF.Atan2(nx, nz);
        character.Location = new Location(
            character.Location.X + nx * step,
            character.Location.Y,
            character.Location.Z + nz * step,
            heading);
    }
}
