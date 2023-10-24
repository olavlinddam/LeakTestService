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

    #region Post

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
    
    public async Task<List<Guid>> AddBatchAsync(string leakTestString)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var leakTests = JsonSerializer.Deserialize<List<LeakTest>>(leakTestString, options);
                
            if (leakTests == null || !leakTests.Any())
            {
                throw new Exception("The request body was null or empty.");
            }

            // making sure the value of user and status are upper case and that the leakTestId is set to a new guid.
            foreach (var leakTest in leakTests)
            {
                leakTest.User = leakTest.User.ToUpper();
                leakTest.Status = leakTest.Status.ToUpper();

                // setting the id of the leaktest. 
                leakTest.LeakTestId = Guid.NewGuid();
            }

            var validator = new LeakTestValidator();
            var validationErrors = new List<string>();

            foreach (var leakTest in leakTests)
            {

                var validationResult = await validator.ValidateAsync(leakTest);
                if (!validationResult.IsValid)
                {
                    validationErrors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
                }
                
                if (validationErrors.Any())
                {
                    throw new ValidationException($"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            
            await _leakTestRepository.AddBatchAsync(leakTests);

            var ids = new List<Guid>();
            leakTests.ForEach(lt =>
            {
                if (lt.LeakTestId != null) ids.Add((Guid)lt.LeakTestId);
            });
            if (ids == null || ids.Count == 0)
            {
                throw new Exception("There was an error returning the Ids of the added resources.");
            }
            return ids;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }

    #endregion
    


    #region Get

    public async Task<string> GetAllAsync()
    {
        try
        {
            // Create and fill the list of LeakTest objects.  
            var leakTests = await _leakTestRepository.GetAllAsync() as List<LeakTest>;
            
            // Check if there are any objects in the LeakTest list and if not, return error message. 
            if (leakTests == null) throw new NullReferenceException($"No test results found.");

            // Validate the objects
            var validator = new LeakTestValidator();
            
            foreach (var leakTest in leakTests)
            {
                var validationResult = await validator.ValidateAsync(leakTest);
                if (!validationResult.IsValid)
                {
                    throw new ValidationException(
                        $"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            
            return JsonSerializer.Serialize(leakTests, new JsonSerializerOptions { WriteIndented = true });

        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }
    
    
    public async Task<string> GetById(Guid id)
    {
        try
        {
            // Getting the LeakTest from the database by id. 
            var leakTest = await _leakTestRepository.GetByIdAsync(id);
            
            // Creating the validator and validating the LeakTest object.
            var validator = new LeakTestValidator();
            var validationResult = await validator.ValidateAsync(leakTest);
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException($"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }
            
            return JsonSerializer.Serialize(leakTest, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }

    #endregion
    
    
    
}