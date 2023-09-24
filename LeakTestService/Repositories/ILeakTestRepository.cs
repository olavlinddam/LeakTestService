using LeakTestService.Models;
using Microsoft.AspNetCore.Mvc;

namespace LeakTestService.Repositories;


public interface ILeakTestRepository
{
    /// <summary>
    /// Retrieves all data from the database, represented as LeakTest objects
    /// </summary>
    /// <returns>An IEnumerable of LeakTest objects.</returns>
    Task<IEnumerable<LeakTest>> GetAllAsync();

    /// <summary>
    /// Adds a single measurement to the database
    /// </summary>
    /// <param name="leakTest">An instance of a LeakTest object</param>
    /// <returns>A pointer to the created resource.</returns>
    Task AddSingleAsync(LeakTest leakTest);

    Task AddBatchAsync(List<LeakTest> leakTest);
    /// <summary>
    /// Queries the database to retrieve all data within the specified time range.
    /// </summary>
    /// <returns>An IEnumerable containing the measurements within the time range as LeakTest objects.</returns>
    Task<IEnumerable<LeakTest>> GetWithinTimeRangeAsync(TimeRange timeRange);
    
    /// <summary>
    /// Queries the database to retrieve all data that matches the provided key-value pair.
    /// </summary>
    /// <param name="key">Represents a tag (column) in the database.</param>
    /// <param name="value">Represents the value of a tag in the database.</param>
    /// <returns>An IEnumerable containing the measurements that matches the provided key-value pair as LeakTest objects.</returns>
    Task<IEnumerable<LeakTest>> GetByTagAsync(string key, string value);
    
    /// <summary>
    /// Queries the database to retrieve a single measurement that matches the provided id.
    /// </summary>
    /// <param name="id">The id to search for.</param>
    /// <returns>A single LeakTest object representing the measurement that matches the provided id.</returns>
    Task<LeakTest> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Queries the database to retrieve all data that matches the provided key-value pair.
    /// </summary>
    /// <param name="tag">Represents a field (column) in the database.</param>
    /// <param name="value">Represents the value of a field in the database.</param>
    /// <returns>An IEnumerable containing the measurements that matches the provided key-value pair as LeakTest objects.</returns>
    Task<IEnumerable<LeakTest>> GetByFieldAsync(string field, string value);
}
