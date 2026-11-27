using ModernBetaPositioningSystem.Models;
namespace ModernBetaPositioningSystem.ResponseModels
{
    public class PositionResponseModel
    {
        public string Username { get; set; } = string.Empty;
        public long X { get; set; }
        public long Y { get; set; }
        public long Z { get; set; }
        public float Speed { get; set; }
        public bool IsInMinecart { get; set; }
        public PositionResponseModel(Position position)
        {
            Username = position?.Username;
            X = position?.X ?? 0;
            Y = position?.Y ?? 0;
            Z = position?.Z ?? 0;
        }
        public PositionResponseModel(PlayerPosition playerPosition)
        {
            Username = playerPosition?.Username;
            X = playerPosition?.ActualPosition?.X ?? 0;
            Y = playerPosition?.ActualPosition?.Y ?? 0;
            Z = playerPosition?.ActualPosition?.Z ?? 0;
            Speed = playerPosition?.Speed ?? 0;
            IsInMinecart = playerPosition?.IsInMinecart ?? false;
        }
    }
}
