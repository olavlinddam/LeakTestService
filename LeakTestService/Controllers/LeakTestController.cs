using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using LeakTestService.Models;
using LeakTestService.Repositories;
using Newtonsoft.Json;
using InfluxDB.Client.Core.Exceptions;
using LeakTestService.Configuration;
using LeakTestService.Exceptions;
using LeakTestService.Models.Validation;
using LeakTestService.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;


namespace LeakTestService.Controllers;

[ApiController]
[Route("api/LeakTests")]
public class LeakTestController : ControllerBase
{
    private readonly ILeakTestRepository _leakTestRepository;
    private readonly IRabbitMqProducer _rabbitMqProducer;
    
    public LeakTestController(ILeakTestRepository leakTestRepository, IRabbitMqProducer rabbitMqProducer)
    {
        _leakTestRepository = leakTestRepository;
        _rabbitMqProducer = rabbitMqProducer;
    }
    

    
    [HttpPost("Batch")] 
    public async Task<IActionResult> AddBatchAsync([FromBody] List<LeakTest> leakTests)
    {
        try
        {
            if (leakTests == null || !leakTests.Any())
            {
                return BadRequest("The request body was null or empty.");
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
            }

            if (validationErrors.Any())
            {
                return BadRequest(
                    $"Some LeakTest objects could not be validated: {string.Join(", ", validationErrors)}");
            }

            await _leakTestRepository.AddBatchAsync(leakTests);

            // Add links to the location of the LeakTest resource
            AddLinkToList(leakTests.ToList());
            return Ok($"Data received successfully: {JsonConvert.SerializeObject(leakTests, Formatting.Indented)}");

        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddSingleAsync([FromBody] LeakTest leakTest)
    {
        try
        {
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
                return BadRequest($"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }
            
            // Posting the LeakTest object as a point in the database. 
            await _leakTestRepository.AddSingleAsync(leakTest);
            
            // Returner en 201 Created statuskode og en Location header
            return CreatedAtAction(nameof(GetById), new { id = leakTest.LeakTestId }, null);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
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
                return BadRequest($"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            }

            // Add HATEOAS links
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            leakTest.Links = new Dictionary<string, string>()
            {
                { "self", $"{baseUrl}/api/LeakTests/{leakTest.LeakTestId}" }
            };

            _rabbitMqProducer.SendMessage(leakTest);
            return Ok(JsonConvert.SerializeObject(leakTest, Formatting.Indented));
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        try
        {
            // Create and fill the list of LeakTest objects.  
            var leakTests = await _leakTestRepository.GetAllAsync() as List<LeakTest>;
            
            // Check if there are any objects in the LeakTest list and if not, return NoContent error message. 
            if (leakTests == null) return NoContent();

            // Validate the objects
            var validator = new LeakTestValidator();
            
            foreach (var leakTest in leakTests)
            {
                var validationResult = await validator.ValidateAsync(leakTest);
                if (!validationResult.IsValid)
                {
                    return BadRequest(
                        $"LeakTest object could not be validated: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            // Add links to the location of the LeakTest resource
            AddLinkToList(leakTests.ToList());

            // Convert to JSON format
            var jsonPayload = JsonConvert.SerializeObject(leakTests, Formatting.Indented);

            // Return the JSON payload
            return Ok(jsonPayload);
        }
        catch (Exception e)
        {
            // Log the exception here
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }
    
    [HttpGet("TimeRange")] // Start and stop values must be valid DateTimes, input as strings.
    public async Task<IActionResult> GetWithinTimeRangeAsync([FromQuery] DateTime start, DateTime? stop)
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
                return BadRequest(
                    $"Time range could not be validated: {string.Join(", ", timeRangeValidationResult.Errors.Select(e => e.ErrorMessage))}");
            }
            
            // Create and fill the list of LeakTest objects.  
            var leakTests = await _leakTestRepository.GetWithinTimeRangeAsync(timeRange) as List<LeakTest>;
            
            // Check if there are any objects in the LeakTest list and if not, return NoContent error message. 
            if (leakTests == null) return NoContent();

            // Validate the objects
            var leakTestValidator = new LeakTestValidator();
            
            foreach (var leakTest in leakTests)
            {
                var leakTestValidationResult = await leakTestValidator.ValidateAsync(leakTest);
                if (!leakTestValidationResult.IsValid)
                {
                    return BadRequest(
                        $"LeakTest object could not be validated: {string.Join(", ", leakTestValidationResult.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            // Add links to the location of the LeakTest resource
            AddLinkToList(leakTests.ToList());

            // Convert to JSON format
            var jsonPayload = JsonConvert.SerializeObject(leakTests, Formatting.Indented);

            // Return the JSON payload
            return Ok(jsonPayload);
        }
        catch (Exception e)
        {
            // Log the exception here
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    [HttpGet("Tag")] // Queries the db to return where a tag is equal to a certain value
    public async Task<IActionResult> GetByTagAsync(string key, string value)
    {
        try
        {
            // Check if 'tag' is a valid property of the LeakTest class. 
            var match = false;
            var culture = CultureInfo.InvariantCulture;
            foreach (var property in typeof(LeakTest).GetProperties())
            {
                // returns true if the tag matches a property name.
                var tagMatchesProperty = culture.CompareInfo.IndexOf(property.Name, key, CompareOptions.IgnoreCase) >= 0;

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
            // Add links to the location of the LeakTest resource
            AddLinkToList(leakTests.ToList());
            
            var jsonPayload = JsonConvert.SerializeObject(leakTests, Formatting.Indented);

            return Ok(jsonPayload);
        }
        catch (NoMatchingDataException noMatchingDataException)
        {
            // Log the exception 
            
            // Return a NotFound status code along with the exception message
            return NotFound(noMatchingDataException.Message);
        }
        catch (InfluxException influxException)
        {
            // Log the exception 
            
            // Return a BadRequest status code along with the exception message
            return BadRequest(influxException.Message);
        }
    }
    
    [HttpGet("Field")] // Queries the db to return where a tag is equal to a certain value
    public async Task<IActionResult> GetByFieldAsync(string key, string value)
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
            
            AddLinkToList(leakTests.ToList());
            var jsonPayload = JsonConvert.SerializeObject(leakTests, Formatting.Indented);

            return Ok(jsonPayload);
        }
        catch (NoMatchingDataException noMatchingDataException)
        {
            // Log the exception 
            
            // Return a BadRequest status code along with the exception message
            return NotFound(noMatchingDataException.Message);
        }
        catch (InfluxException influxException)
        {
            // Log the exception 
        
            // Return a BadRequest status code along with the exception message
            return BadRequest(influxException.Message);
        }
    }

    private void AddLinkToList(List<LeakTest> leakTests)
    {
        // Add HATEOAS links
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        leakTests.ForEach(leakTest =>
        {
            leakTest.Links = new Dictionary<string, string>
            {
                { "self", $"{baseUrl}/api/LeakTests/{leakTest.LeakTestId}" }
            };
        });
    }
}

