namespace FlexSpace.Api.DTOs
{
    public class CreateBookingRequest
    {
        public Guid WorkspaceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
