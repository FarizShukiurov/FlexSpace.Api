using FlexSpace.Api.Data;
using FlexSpace.Api.DTOs;
using FlexSpace.Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FlexSpace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<CreateBookingRequest> _validator;

        public BookingsController(ApplicationDbContext context, IValidator<CreateBookingRequest> validator)
        {
            _context = context;
            _validator = validator;
        }

        /// <summary>
        /// Creates a new booking and automatically calculates the total price.
        /// </summary>
        /// <param name="request">Booking details containing workspace ID and time range.</param>
        /// <response code="200">Booking successfully created.</response>
        /// <response code="400">Invalid time range.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="404">Workspace not found.</response>
        /// <response code="409">Conflict. The workspace is already booked for this time.</response>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateBooking(CreateBookingRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            var workspace = await _context.Workspaces.FindAsync(request.WorkspaceId);
            if (workspace == null)
            {
                return NotFound($"Workspace with ID {request.WorkspaceId} not found.");
            }

            var isOverlapping = await _context.Bookings
            .AnyAsync(b => b.WorkspaceId == request.WorkspaceId &&
                           b.Status != BookingStatus.Cancelled &&
                           request.StartTime < b.EndTime &&
                           request.EndTime > b.StartTime);

            if (isOverlapping)
            {
                return BadRequest("This place is already booked for the selected time.");
            }

            var totalHours = (decimal)(request.EndTime - request.StartTime).TotalHours;
            var totalPrice = totalHours * workspace.PricePerHour;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in token." });
            }

            var customerEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var customerName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(customerEmail) || string.IsNullOrEmpty(customerName))
            {
                return Unauthorized("User identity claims are missing in token.");
            }

            var newBooking = new Booking
            {
                WorkspaceId = request.WorkspaceId,
                UserId = userId,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                TotalPrice = totalPrice, 
            };

            _context.Bookings.Add(newBooking);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "The reservation has been successfully created and is awaiting payment.",
                BookingId = newBooking.Id,
                TotalPrice = newBooking.TotalPrice
            });

        }

        /// <summary>
        /// Confirms a pending booking, changing its status to Confirmed. (Admin only)
        /// </summary>
        /// <param name="id">The unique identifier of the booking.</param>
        /// <response code="200">Booking successfully confirmed.</response>
        /// <response code="400">Booking has already been confirmed.</response>
        /// <response code="401">User is not authenticated.</response>
        /// <response code="403">Forbidden. Requires Admin role.</response>
        /// <response code="404">Booking not found.</response>
        [HttpPut("{id:guid}/confirm")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmBooking(Guid id)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
            {
                return NotFound($"Reservation with ID {id} not found.");
            }

            if (booking.Status == BookingStatus.Confirmed)
            {
                return BadRequest("This booking has already been confirmed.");
            }

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Reservation №{id} has been successfully confirmed! The seat will now appear as occupied." });
        }

        /// <summary>
        /// Retrieves all future confirmed time slots for a specific workspace.
        /// </summary>
        /// <param name="workspaceId">The unique identifier of the workspace.</param>
        /// <response code="200">Returns a list of occupied time slots. Returns an empty list if the workspace is free.</response>
        [HttpGet("workspace/{workspaceId:guid}")]
        public async Task<ActionResult<IEnumerable<OccupiedTimeSlotDto>>> GetOccupiedSlots(Guid workspaceId)
        {
            var now = DateTime.UtcNow;

            var occupiedSlots = await _context.Bookings
                .Where(b => b.WorkspaceId == workspaceId &&
                            b.Status == BookingStatus.Confirmed &&
                            b.EndTime > now)
                .Select(b => new OccupiedTimeSlotDto
                {
                    StartTime = b.StartTime,
                    EndTime = b.EndTime
                })
                .ToListAsync();

            return Ok(occupiedSlots);
        }

        /// <summary>
        /// Retrieves all bookings made by the currently authenticated user.
        /// </summary>
        /// <response code="200">Returns the user's booking history.</response>
        /// <response code="401">User is not authenticated.</response>
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyBookings()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in token." });
            }

            var myBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt) 
                .ToListAsync();

            return Ok(myBookings);
        }

        /// <summary>
        /// Cancels an active booking. Users can only cancel their own bookings.
        /// </summary>
        /// <param name="id">The unique identifier of the booking to cancel.</param>
        /// <response code="200">Booking successfully cancelled.</response>
        /// <response code="400">Booking is already cancelled.</response>
        /// <response code="403">Forbidden. Attempting to cancel another user's booking.</response>
        /// <response code="404">Booking not found.</response>
        [HttpPut("{id:guid}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized(new { message = "User ID is missing or invalid in token." });
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound(new { message = "Booking not found." });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (booking.UserId != userId && userRole != "Admin")
            {
                return Forbid(); 
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return BadRequest(new { message = "Booking is already cancelled." });
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking successfully cancelled.", bookingId = booking.Id });
        }
    }
}
