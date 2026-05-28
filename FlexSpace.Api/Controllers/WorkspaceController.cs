using FlexSpace.Api.Data;
using FlexSpace.Api.DTOs;
using FlexSpace.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlexSpace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WorkspaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves a list of all available workspaces in the catalog.
        /// </summary>
        /// <response code="200">Returns the list of workspaces.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkspaceDto>>> GetAll()
        {
            var workspaces = await _context.Workspaces
                .Select(w => new WorkspaceDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Description = w.Description,
                    PricePerHour = w.PricePerHour,
                    Type = w.Type.ToString(),
                })
                .ToListAsync();

            return Ok(workspaces);
        }

        /// <summary>
        /// Finds available workspaces that have no active bookings for the specified time range.
        /// </summary>
        /// <param name="startTime">The desired start time (e.g. 2026-05-30T10:00:00Z)</param>
        /// <param name="endTime">The desired end time</param>
        /// <response code="200">Returns a list of available workspaces.</response>
        /// <response code="400">If the time range is invalid.</response>
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableWorkspaces([FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            if (startTime < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Start time cannot be in the past." });
            }

            if (endTime <= startTime)
            {
                return BadRequest(new { message = "End time must be after start time." });
            }

            var availableWorkspaces = await _context.Workspaces
                .Where(w => !_context.Bookings.Any(b =>
                    b.WorkspaceId == w.Id &&
                    b.Status != BookingStatus.Cancelled &&
                    b.StartTime < endTime &&
                    b.EndTime > startTime))
                .ToListAsync();

            return Ok(availableWorkspaces);
        }

        /// <summary>
        /// Adds a new workspace to the catalog. (Admin only)
        /// </summary>
        /// <response code="200">Workspace successfully created.</response>
        /// <response code="401">Unauthorized access.</response>
        /// <response code="403">Forbidden. Requires Admin role.</response>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WorkspaceDto>> Create(CreateWorkspaceDto request)
        {
            var newWorkspace = new Models.Workspace
            {
                Name = request.Name,
                Description = request.Description,
                PricePerHour = request.PricePerHour,
                Type = request.Type,
            };

            _context.Workspaces.Add(newWorkspace);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = newWorkspace.Id }, newWorkspace);
        }

    }
}
