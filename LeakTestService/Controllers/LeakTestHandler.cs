using FluentValidation;
using LeakTestService.Models;
using LeakTestService.Models.Validation;
using LeakTestService.Repositories;
using LeakTestService.Services;
using System.Text.Json;

namespace LeakTestService.Controllers;

public class LeakTestHandler
{
    private readonly ILeakTestRepository _leakTestRepository;
    
    public LeakTestHandler(ILeakTestRepository leakTestRepository)
    {
        _leakTestRepository = leakTestRepository;
    }
    
    public async Task<Guid> AddSingleAsync(string leakTestString)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        try
        {
            LeakTest leakTest = JsonSerializer.Deserialize<LeakTest>(leakTestString, options);

            // making sure that "user" and "status" are upper cased.
            leakTest.User = leakTest.User.ToUpper();
            leakTest.Status = leakTest.Status.ToUpper();
            
            // adding the GUID to identify the LeakTest
            leakTest.LeakTestId = Guid.NewGuid();
            
            // Creating the validator and validating the LeakTest object.
            var validator = new LeakTestValidator();
            var validationResult = await validator.ValidateAsync(leakTest);
            
            // setting the id of the leaktest. 
            leakTest.LeakTestId = Guid.NewGuid();
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException($"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }
            
            // Posting the LeakTest object as a point in the database. 
            await _leakTestRepository.AddSingleAsync(leakTest);
            
            // Return the id of the newly created leak test
            return (Guid)leakTest.LeakTestId;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }
}