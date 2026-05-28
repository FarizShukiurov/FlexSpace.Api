using FluentValidation;
using FlexSpace.Api.DTOs;

namespace FlexSpace.Api.Services
{
    public class CreateBookingValidator: AbstractValidator<CreateBookingRequest>
    {
        public CreateBookingValidator() { 

            RuleFor(x => x.StartTime)
                .GreaterThan(DateTime.UtcNow).WithMessage("Start time must be in the future.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage("End time must be after start time.");
        }
    }
}
