namespace ModernBetaPositioningSystem.Models
{
    public class PlayerPosition
    {
        public string Username { get; private set; }
        public Position ActualPosition { get; private set; }
        public Position PreviousPosition { get; private set; }

        public bool IsTracked
        {
            get; private set;
        }
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
                {
                    return new Position { X = 0, Y = 0, Z = 0 };
                }
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
        public void UpdatePosition(Position newPosition)
        {
            PreviousPosition = ActualPosition;
            ActualPosition = newPosition;

            if (ActualPosition.Time == null)
                ActualPosition.Time = DateTime.Now;

            IsTracked = true;
        }
        public void UnTrack()
        {
            IsTracked = false;
        }
        public override string ToString()
        {
            return @$"Username: {Username},
                      Position: ({ActualPosition.X}, {ActualPosition.Y}, {ActualPosition.Z}), 
                      Distance: {Distance}, Speed: {Speed}, 
                      Movement Vector: ({MovementVector.X}, {MovementVector.Y}, {MovementVector.Z})
                      Tracking: {IsTracked}
                      InMinecart: {IsInMinecart}";
        }
    }
}
