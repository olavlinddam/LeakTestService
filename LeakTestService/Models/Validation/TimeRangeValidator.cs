using FluentValidation;
using System;
using System.Text.RegularExpressions;
using LeakTestService.Models;


namespace LeakTestService.Models.Validation;

public class TimeRangeValidator : AbstractValidator<TimeRange>
{
    ///<summary>
    /// This class is used to validate the time range provided by the client when they call GetWithinTimeRange(). 
    /// </summary>
 
    public TimeRangeValidator()
    {
        RuleFor(x => x.Start)
            .NotEmpty().WithMessage("Start date must not be empty.")
            .LessThanOrEqualTo(x => x.Stop ?? DateTime.UtcNow)
            .WithMessage("Start date must be less than or equal to Stop date.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Start date must be in the past or present.");

        RuleFor(x => x.Stop)
            .NotEmpty().WithMessage("Stop date must not be empty.")
            .GreaterThanOrEqualTo(x => x.Start).WithMessage("Stop date must be greater than or equal to Start date.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Stop date must be in the past or present.");
    }
}


