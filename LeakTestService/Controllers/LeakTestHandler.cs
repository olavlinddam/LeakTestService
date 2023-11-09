using System.Globalization;
using FluentValidation;
using LeakTestService.Models;
using LeakTestService.Models.Validation;
using LeakTestService.Repositories;
using LeakTestService.Services;
using System.Text.Json;
using InfluxDB.Client.Core.Exceptions;
using LeakTestService.Exceptions;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace LeakTestService.Controllers;

public class LeakTestHandler
{
    private readonly ILeakTestRepository _leakTestRepository;
    
    public LeakTestHandler(ILeakTestRepository leakTestRepository)
    {
        _leakTestRepository = leakTestRepository;
    }

    #region Post

    public async Task<LeakTest> AddSingleAsync(string leakTestString)
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
                throw new ValidationException(
                    $"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            // Posting the LeakTest object as a point in the database. 
            await _leakTestRepository.AddSingleAsync(leakTest);

            // Return the id of the newly created leak test
            return leakTest;
        }
        catch (ValidationException e)
        {
            throw;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }
    
    public async Task<List<LeakTest>> AddBatchAsync(string leakTestString)
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

            return leakTests;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }

    #endregion
    


    #region Get

    public async Task<List<LeakTest>> GetAllAsync()
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

            return leakTests;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }
    
    
    public async Task<LeakTest> GetById(Guid id)
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

            return leakTest;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }

    public async Task<IEnumerable<LeakTest>> GetByTagAsync(string key, string value)
    {
        try
        {
            // Check if 'tag' is a valid property of the LeakTest class. 
            var match = false;
            var culture = CultureInfo.InvariantCulture;
            foreach (var property in typeof(LeakTest).GetProperties())
            {
                // returns true if the tag matches a property name.
                var tagMatchesProperty =
                    culture.CompareInfo.IndexOf(property.Name, key, CompareOptions.IgnoreCase) >= 0;

                if (tagMatchesProperty)
                {
                    match = true;
                }
            }

            if (!match)
            {
                throw new NoMatchingDataException($"The specified tag '{key}' does not exist.");
            }

            if (key.ToLower() == "status" || key.ToLower() == "user")
            {
                value = value.ToUpper();
            }

            key = StringExtensions.FirstCharToUpper(key);

            var leakTests = await _leakTestRepository.GetByTagAsync(key, value);

            if (!leakTests.Any())
            {
                throw new NoMatchingDataException("No test results match the specified tag key-value pair.");
            }

            return leakTests;
        }
        catch (NoMatchingDataException noMatchingDataException)
        {
            // Log the exception 

            // Return a NotFound status code along with the exception message
            throw new NoMatchingDataException(noMatchingDataException.Message);
        }
        catch (InfluxException influxException)
        {
            // Log the exception 

            // Return a BadRequest status code along with the exception message
            throw new InfluxException(influxException.Message);
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"The request could not be processed due to: {e.Message}");
        }
    }
    
    public async Task<List<LeakTest>> GetByFieldAsync(string key, string value)
    {
        try
        {
            // Check if 'key' is a valid property of the LeakTest class, by comparing the names of the LeakTest properties
            // with the value of the 'key' input parameter
            
            var match = false;
            var culture = CultureInfo.InvariantCulture;
            foreach (var property in typeof(LeakTest).GetProperties())
            {
                // returns true if the tag matches a property name.
                var keydMatchesProperty = culture.CompareInfo.IndexOf(property.Name, key, CompareOptions.IgnoreCase) >= 0;

                if (keydMatchesProperty)
                {
                    match = true;
                }
            }
            if (!match)
            {
                throw new NoMatchingDataException($"The specified key '{key}' does not exist.");
            }

            // Making sure that the value of the input param 'key' has the correct letters Upper Cased. 
            key = StringExtensions.NormalizeField(key);
            
            var leakTests = await _leakTestRepository.GetByTagAsync(key, value);

            if (!leakTests.Any())
            {
                throw new NoMatchingDataException("No test results match the specified tag key-value pair.");
            }

            return leakTests.ToList();
        }
        catch (NoMatchingDataException noMatchingDataException)
        {
            // Log the exception 
            
            // Return a BadRequest status code along with the exception message
            throw new NoMatchingDataException(noMatchingDataException.Message);
        }
        catch (InfluxException influxException)
        {
            // Log the exception 
        
            // Return a BadRequest status code along with the exception message
            throw new InfluxException(influxException.Message);
        }
    }
    
    // Start and stop values must be valid DateTimes, input as strings.
    public async Task<List<LeakTest>> GetWithinTimeRangeAsync(DateTime start, DateTime? stop)
    {
        try
        {
            // Create TimeRange object
            var timeRange = new TimeRange(start, stop);
            
            // Validate the TimeRange object
            var timeRangeValidator = new TimeRangeValidator();
            var timeRangeValidationResult = await timeRangeValidator.ValidateAsync(timeRange);

            if (!timeRangeValidationResult.IsValid)
            {
                throw new ValidationException(
                    $"Time range could not be validated: {string.Join(", ", timeRangeValidationResult.Errors.Select(e => e.ErrorMessage))}");
            }
            
            // Create and fill the list of LeakTest objects.  
            var leakTests = await _leakTestRepository.GetWithinTimeRangeAsync(timeRange) as List<LeakTest>;
            
            // Check if there are any objects in the LeakTest list and if not, return NoContent error message. 
            if (leakTests == null) throw new Exception("No data about Leak Tests in the specified timeframe could be found.");

            // Validate the objects
            var leakTestValidator = new LeakTestValidator();
            
            foreach (var leakTest in leakTests)
            {
                var leakTestValidationResult = await leakTestValidator.ValidateAsync(leakTest);
                if (!leakTestValidationResult.IsValid)
                {
                    throw new ValidationException(
                        $"LeakTest object could not be validated: {string.Join(", ", leakTestValidationResult.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            return leakTests;
        }
        catch (Exception e)
        {
            // Log the exception here
            throw new Exception($"Internal server error: {e.Message}");
        }
    }

    #endregion
    
    
    
}