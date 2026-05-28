namespace FlexSpace.Api.DTOs
{
    public class WorkspaceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
