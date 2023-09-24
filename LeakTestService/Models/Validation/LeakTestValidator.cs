using FluentValidation;
using System.Text.RegularExpressions;


namespace LeakTestService.Models.Validation;

public class LeakTestValidator : AbstractValidator<LeakTest>
{
    ///<summary>
    /// This class is used to validate the properties of a LeakTest object. 
    /// </summary>
    public LeakTestValidator()
    {
        RuleFor(x => x.TimeStamp)
            .NotNull().WithMessage("TimeStamp is required and can not be null.")
            .Must(x => x <= DateTime.Now).WithMessage("TimeStamp cannot be a future date.");

        RuleFor(x => x.Reason)
            .NotEmpty().When(x => x.Status == "NOK").WithMessage("Reason cannot be empty when Status is NOK.")
            .NotNull().When(x => x.Status == "NOK").WithMessage("Reason cannot be null when Status is NOK.");
        
        RuleFor(x => x.TestObjectId)
            .Must(x => x != Guid.Empty)
            .WithMessage("TestObject is empty");
        
        RuleFor(x => x.LeakTestId)
            .Must(x => x != Guid.Empty).WithMessage("LeakTestId can not be empty")
            .NotNull().WithMessage("LeakTestId can not be null");
        
        
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status can not be empty.")
            .Matches("^(OK|NOK)$").WithMessage("Status must be either OK or NOK");
        
        RuleFor(x => x.TestObjectId)
            .Must(x => x != Guid.Empty)
            .WithMessage("User is empty");


        RuleFor(x => x.SniffingPoint)
            .NotEmpty().WithMessage("SniffingPoint cannot be empty.")
            .NotNull().WithMessage("SniffingPoint cannot be null.")
            .Custom((value, context) =>
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    context.AddFailure("SniffingPoint should not be whitespace.");
                }
                else if (!Regex.IsMatch(value, @"^[a-zA-Z0-9-_]+$"))
                {
                    context.AddFailure("SniffingPoint can only contain alphanumeric characters, hyphens, and underscores.");
                }
            })
            .Length(1, 999).WithMessage("SniffingPoint must have a length between 1 and 999 characters.");

        RuleFor(x => x.Measurement)
            .Equal("LeakTest").WithMessage("The measurement for LeakTest objects must be 'LeakTest'.");
    }
    
    
    private bool IsValidGuid(string value)
    {
        // Checking if the GUID is valid and dashed ("-")
        return Guid.TryParse(value, out var guid) && value == guid.ToString();    }
    }
