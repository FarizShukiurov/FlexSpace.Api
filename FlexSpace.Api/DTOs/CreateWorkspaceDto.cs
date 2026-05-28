using FlexSpace.Api.Models;

namespace FlexSpace.Api.DTOs
{
    public class CreateWorkspaceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PricePerHour { get; set; }
        public WorkspaceType Type { get; set; }
    }
}
