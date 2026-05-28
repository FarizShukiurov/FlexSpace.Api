namespace FlexSpace.Api.Models
{
    public class Workspace
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public bool isAvailable { get; set; } = true;
        public WorkspaceType Type { get; set; }

        public List<Booking> Bookings { get; set; } = new();
    }
    public enum WorkspaceType
    {
        Desk,
        MeetingRoom
    }
}
