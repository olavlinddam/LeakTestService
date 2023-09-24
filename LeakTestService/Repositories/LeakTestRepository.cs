using System.Reflection;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using InfluxDB.Client.Linq;
using LeakTestService.Configuration;
using LeakTestService.Converters;
using LeakTestService.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using LeakTestService.Exceptions;
using Expression = System.Linq.Expressions.Expression;

namespace LeakTestService.Repositories;

public class LeakTestRepository : ILeakTestRepository
{
    private readonly InfluxDbConfig _config;
    private readonly InfluxDBClient _client;
    // private readonly QueryApiSync _queryApi;
    
    public LeakTestRepository(IOptions<InfluxDbConfig> influxDbConfigOptions)
    {
        // Mapping InfluxDbConfig class to _config, with the configuration from appsettings.
        _config = influxDbConfigOptions.Value;
        
        // Setting the options of the client. 
        var options = new InfluxDBClientOptions(_config.Url)
        {
            Token = _config.Token,
            Org = _config.Org,
            Bucket = _config.Bucket
        };
        
        // Initializing a new client with the options. This client will be used to establish the connection to the DB.
        _client = new InfluxDBClient(options).EnableGzip();
    }

    #region Get
    public async Task<IEnumerable<LeakTest>> GetAllAsync()
    {
        try
        {
            // Creating a Task so we can run the method async.
            return await Task.Run(() =>
            {
                // Init the LeakTestConverter, which implements IDomainObjectMapper, and using it as input for GetQueryApi
                var converter = new LeakTestConverter();
            
                // using var client = _client;
                var queryApi = _client.GetQueryApiSync(converter);
        
                // Creating an instance of QueryableOptimizerSettings to enable Measurement Column
                var optimizerSettings = new QueryableOptimizerSettings()
                {
                    DropMeasurementColumn = false
                };
        
                // Creating the query to pull all points from the specified bucket and mapping each to a LeakTest objects
                var query = from t in InfluxDBQueryable<LeakTest>
                        .Queryable(_config.Bucket, _config.Org, queryApi, converter, optimizerSettings)
                    select t;
            
                var leakTests = query.ToList();

                return leakTests;
            });
        }
        catch (Exception e)
        {
            throw new BadHttpRequestException($"The request could not be processed. {e.Message}");
        }
    }

    public async Task<IEnumerable<LeakTest>> GetWithinTimeRangeAsync(TimeRange timeRange)
    {
        try
        {
            // Creating a Task so we can run the method async.
            return await Task.Run(() =>
            {
                // Init the LeakTestConverter, which implements IDomainObjectMapper, and using it as input for GetQueryApi
                var converter = new LeakTestConverter();
            
                // using var client = _client;
                var queryApi = _client.GetQueryApiSync(converter);
        
                // Creating an instance of QueryableOptimizerSettings to enable Measurement Column
                var optimizerSettings = new QueryableOptimizerSettings()
                {
                    DropMeasurementColumn = false
                };
                

                // Creating the query to pull all points from the specified bucket and mapping each to a LeakTest objects
                IQueryable<LeakTest> query;
                if (timeRange.Stop != null)
                {
                    query = from t in InfluxDBQueryable<LeakTest>
                            .Queryable(_config.Bucket, _config.Org, queryApi, optimizerSettings)
                        where t.TimeStamp >= timeRange.Start && t.TimeStamp <= timeRange.Stop
                        select t;
                }
                else
                {
                    query = from t in InfluxDBQueryable<LeakTest>
                            .Queryable(_config.Bucket, _config.Org, queryApi, optimizerSettings)
                        where t.TimeStamp >= timeRange.Start
                        select t;
                }
                
                var leakTests = query.ToList();

                return leakTests;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    
    /// <summary>
    /// Get all the records matching the key-value pair provided. 
    /// </summary>
    /// <param name="key">Key represents the name or identifier for a specific data attribute within a record.
    /// In the context of tags and fields, the key serves as the column name that categorizes or labels the associated
    /// value. For example, a key could be 'user' to indicate that the corresponding value will contain user
    /// information.</param>
    /// <param name="value">"Value is the actual data associated with a given key. In a tag or field, the value serves
    /// as the entry in the column represented by the key. For instance, if the key is 'user,' the value could be
    /// 'USER1,' indicating that this particular record pertains to USER1."</param>
    /// <returns>A collection of LeakTest objects matching the key-value pair.</returns>
    /// <exception cref="InfluxException"></exception>
    public async Task<IEnumerable<LeakTest>> GetByTagAsync(string key, string value)
    {
        // Creating a Task so we can run the method async.
        return await Task.Run(() =>
        {
            try
            {
                // Init the LeakTestConverter, which implements IDomainObjectMapper, and using it as input for GetQueryApi
                var converter = new LeakTestConverter();

                // using var client = _client;
                var queryApi = _client.GetQueryApiSync(converter);

                // Creating an instance of QueryableOptimizerSettings to enable Measurement Column
                var optimizerSettings = new QueryableOptimizerSettings()
                {
                    DropMeasurementColumn = false
                };

                // Creating a query to pull all points from the DB that matches the specified tag and value key-value pair.
                var query = InfluxDBQueryable<LeakTest>
                    .Queryable(_config.Bucket, _config.Org, queryApi, optimizerSettings).AsQueryable();

                if (!string.IsNullOrEmpty(key) && value != null)
                {
                    var parameter = Expression.Parameter(typeof(LeakTest), "t");
                    var member = Expression.Property(parameter, key);

                    object constantValue;

                    // check if value is a valid guid
                    if (Guid.TryParse(value, out Guid parsedGuid))
                    {
                        constantValue = (Guid?)parsedGuid;
                    }
                    else
                    {
                        constantValue = value;
                    }

                    var constant = Expression.Constant(constantValue, constantValue.GetType());

                    // Ensure both sides of the expression have the same type
                    if (member.Type != constant.Type)
                    {
                        // If they're not the same type, an explicit conversion will be needed
                        var converted = Expression.Convert(constant, member.Type);
                        var body = Expression.Equal(member, converted);
                        var lambda = Expression.Lambda<Func<LeakTest, bool>>(body, parameter);
                        query = query.Where(lambda);
                    }
                    else
                    {
                        var body = Expression.Equal(member, constant);
                        var lambda = Expression.Lambda<Func<LeakTest, bool>>(body, parameter);
                        query = query.Where(lambda);
                    }
                }

                var leakTests = query.ToList();

                return leakTests;
            }
            catch (InfluxException influxException)
            {
                throw new InfluxException("Could not retrieve data", influxException);
            }
        });
    }
    
    public async Task<IEnumerable<LeakTest>> GetByFieldAsync(string field, string value)
    {
        // Creating a Task so we can run the method async.
        return await Task.Run(() =>
        {
            try
            {
                // Init the LeakTestConverter, which implements IDomainObjectMapper, and using it as input for GetQueryApi
                var converter = new LeakTestConverter();

                // using var client = _client;
                var queryApi = _client.GetQueryApiSync(converter);

                // Creating an instance of QueryableOptimizerSettings to enable Measurement Column
                var optimizerSettings = new QueryableOptimizerSettings()
                {
                    DropMeasurementColumn = false
                };

                // Creating a query to pull all points from the DB that matches the specified tag and value key-value pair.
                var query = InfluxDBQueryable<LeakTest>
                    .Queryable(_config.Bucket, _config.Org, queryApi, optimizerSettings).AsQueryable();

                if (!string.IsNullOrEmpty(field) && value != null)
                {
                    var parameter = Expression.Parameter(typeof(LeakTest), "t");
                    var member = Expression.Property(parameter, field);

                    object constantValue;

                    // check if value is a valid guid
                    if (Guid.TryParse(value, out Guid parsedGuid))
                    {
                        constantValue = (Guid?)parsedGuid;
                    }
                    else
                    {
                        constantValue = value;
                    }

                    var constant = Expression.Constant(constantValue, constantValue.GetType());

                    // Ensure both sides of the expression have the same type
                    if (member.Type != constant.Type)
                    {
                        // If they're not the same type, an explicit conversion will be needed
                        var converted = Expression.Convert(constant, member.Type);
                        var body = Expression.Equal(member, converted);
                        var lambda = Expression.Lambda<Func<LeakTest, bool>>(body, parameter);
                        query = query.Where(lambda);
                    }
                    else
                    {
                        var body = Expression.Equal(member, constant);
                        var lambda = Expression.Lambda<Func<LeakTest, bool>>(body, parameter);
                        query = query.Where(lambda);
                    }
                }

                var leakTests = query.ToList();

                return leakTests;
            }
            catch (InfluxException influxException)
            {
                throw new InfluxException("Could not retrieve data", influxException);
            }
        });
    }
    
    

    public async Task<LeakTest> GetByIdAsync(Guid id)
    {
        return await Task.Run(() =>
        {
            // Init the LeakTestConverter, which implements IDomainObjectMapper, and using it as input for GetQueryApi
            var converter = new LeakTestConverter();

            // using var client = _client;
            var queryApi = _client.GetQueryApiSync(converter);

            // Creating an instance of QueryableOptimizerSettings to enable Measurement Column
            var optimizerSettings = new QueryableOptimizerSettings()
            {
                DropMeasurementColumn = false
            };

            var query = from t in InfluxDBQueryable<LeakTest>
                    .Queryable(_config.Bucket, _config.Org, queryApi, optimizerSettings)
                where t.LeakTestId == id
                select t;

            var leakTest = query.ToList().Single();

            // Check if the resource exists
            if (leakTest == null)
            {
                throw new NullReferenceException($"LeakTest with ID {id} not found.");
            }

            return leakTest;
        });
    }
    

    #endregion

    

    #region Post

    public async Task AddSingleAsync(LeakTest leakTest)
    {
        try
        {
            await _client.GetWriteApiAsync().WriteMeasurementAsync(leakTest);
        }
        
        catch (InfluxException influxException)
        {
            throw new InfluxException($"Could not save data due to: {influxException.Message}", influxException);
        }
        catch (Exception ex)
        {
            throw new LeakTestRepositoryException("An unexpected error occured", ex);
        }
    }

    public async Task AddBatchAsync(List<LeakTest> leakTests)
    {
        try
        {
            await _client.GetWriteApiAsync().WriteMeasurementsAsync(leakTests);
        }
        
        catch (InfluxException influxException)
        {
            throw new InfluxException("Could not save data", influxException);
        }
        catch (Exception ex)
        {
            throw new LeakTestRepositoryException("An unexpected error occured", ex);
        }
    }


    #endregion
}





