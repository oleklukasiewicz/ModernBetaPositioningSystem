namespace ModernBetaPositioningSystem.Models;

public class PlayerPosition
{
    public string Username { get; private set; } = string.Empty;
    public Position ActualPosition { get; private set; } = Position.Zero;
    public Position PreviousPosition { get; private set; } = Position.Zero;

    public bool IsInMinecart => Speed > 6;

    public float Distance
    {
        get
        {
            if (ActualPosition == null || PreviousPosition == null)
                return 0;

            var movementVector = MovementVector;
            return (float)Math.Sqrt(movementVector.X * movementVector.X + movementVector.Y * movementVector.Y + movementVector.Z * movementVector.Z);
        }
    }

    public float Speed
    {
        get
        {
            var seconds = (ActualPosition?.Time - PreviousPosition?.Time)?.TotalSeconds ?? 0;
            return seconds > 0 ? Distance / (float)seconds : 0f;
        }
    }

    public Position MovementVector
    {
        get
        {
            if (ActualPosition == null || PreviousPosition == null)
                return Position.Zero;

            return new Position
            {
                X = ActualPosition.X - PreviousPosition.X,
                Y = ActualPosition.Y - PreviousPosition.Y,
                Z = ActualPosition.Z - PreviousPosition.Z
            };
        }
    }

    public PlayerPosition(string username, Position position)
    {
        Username = username;
        UpdatePosition(position);
    }

    public PlayerPosition UpdatePosition(Position newPosition)
    {
        PreviousPosition = ActualPosition;
        ActualPosition = newPosition;

        if (ActualPosition.Time == null)
            ActualPosition.Time = DateTime.Now;

        return this;
    }

    public override string ToString()
    {
        return @$"Username: {Username},
                      Position: ({ActualPosition.X}, {ActualPosition.Y}, {ActualPosition.Z}), 
                      Distance: {Distance}, Speed: {Speed}, 
                      Movement Vector: ({MovementVector.X}, {MovementVector.Y}, {MovementVector.Z})
                      InMinecart: {IsInMinecart}";
    }
}
