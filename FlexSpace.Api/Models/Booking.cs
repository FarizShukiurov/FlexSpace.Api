using System.Text.Json.Serialization;

namespace FlexSpace.Api.Models
{
    //(Id, WorkspaceId, CustomerName, StartTime, EndTime, TotalPrice, IsPaid)
    public class Booking
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public Guid UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public decimal TotalPrice { get; set; }

        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
    }

}
