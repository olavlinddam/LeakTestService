using System.Reflection;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Linq;
using InfluxDB.Client.Writes;
using LeakTestService.Models;

namespace LeakTestService.Converters;

public class LeakTestConverter : IDomainObjectMapper, IMemberNameResolver
{
    /// <summary>
    /// This class is used to map from a LeakTest object to a Point Data representation of that object, or to map
    /// from a Flux Record to a LeakTest object. 
    /// </summary>
    /// <param name="fluxRecord">A representation of a point in an InfluxDb. This representation must contain all the
    /// required information to populate a LeakTest object</param>
    /// <typeparam name="T">An object of type LeakTest</typeparam>
    /// <returns>Either a LeakTest object when mapping from a Flux Record, or a Point Data representation of a LeakTest
    /// object when mapping from a LeakTest object.</returns>
    public T ConvertToEntity<T>(FluxRecord fluxRecord)
    {
        return (T)ConvertToEntity(fluxRecord, typeof(T));
    }

    public object ConvertToEntity(FluxRecord fluxRecord, Type type)
    {
        if (fluxRecord == null)
        {
            throw new ArgumentNullException(nameof(fluxRecord));
        }

        if (type != typeof(LeakTest))
        {
            throw new NotSupportedException($"This converter doesn't support: {type}");
        }

        // var ts = fluxRecord.GetTime()?.ToDateTimeUtc().ToLocalTime();
        // var mid = Guid.Parse(fluxRecord.GetValueByKey("machine_id")?.ToString());
        // var tid = Guid.Parse(fluxRecord.GetValueByKey("test_object_id")?.ToString());
        // var lid = Guid.Parse(fluxRecord.GetValueByKey("leak_test_id").ToString());

        // if (ts == null)
        // {
        //     throw new Exception("Timestamp is null");
        // }
        try
        {
            var leakTest = new LeakTest();

            leakTest.TimeStamp = fluxRecord.GetTime().GetValueOrDefault().ToDateTimeUtc().ToLocalTime();
            leakTest.Measurement = fluxRecord.GetValueByKey("_measurement")?.ToString();
            leakTest.MachineId = Guid.Parse(fluxRecord.GetValueByKey("MachineId")?.ToString());
            leakTest.Status = fluxRecord.GetValueByKey("Status")?.ToString();
            leakTest.TestObjectId = Guid.Parse(fluxRecord.GetValueByKey("TestObjectId")?.ToString());
            leakTest.TestObjectType = fluxRecord.GetValueByKey("TestObjectType")?.ToString();
            leakTest.LeakTestId = Guid.Parse(fluxRecord.GetValueByKey("LeakTestId").ToString());
            leakTest.SniffingPoint = Guid.Parse(fluxRecord.GetValueByKey("SniffingPoint")?.ToString());
            leakTest.User = fluxRecord.GetValueByKey("User")?.ToString();
            leakTest.Reason = fluxRecord.GetValueByKey("Reason")?.ToString() ?? null;
            
            

            return Convert.ChangeType(leakTest, type);
        }
        catch (Exception e)
        {
            // throw new Exception(
            //     $"There was an error converting the record with timestamp: {fluxRecord.GetTime()} - {e.Message}");
            throw new Exception(e.Message);
        }
    }


    public PointData ConvertToPointData<T>(T entity, WritePrecision precision)
    {
            if (!(entity is LeakTest ce))
            {
                throw new NotSupportedException($"This converter doesn't support: {typeof(T)}");
            }
            try
            {
                var point = PointData
                    .Measurement(ce.Measurement)
                    .Tag("TestObjectId", ce.TestObjectId.ToString())
                    .Tag("Status", ce.Status)
                    .Tag("MachineId", ce.MachineId.ToString())
                    .Tag("TestObjectType", ce.TestObjectType)
                    .Tag("User", ce.User.ToString())
                    .Field("SniffingPoint", ce.SniffingPoint)
                    .Field("Reason", ce.Reason ?? null)
                    .Field("LeakTestId", ce.LeakTestId.ToString())
                    .Timestamp(ce.TimeStamp, precision);

                return point;
            }
            catch (Exception e)
            {
                // Log here if necessary
                throw new Exception(e.Message);
            }
    }
    

    public MemberType ResolveMemberType(MemberInfo memberInfo)
    {
        return memberInfo.Name switch
        {
            "TimeStamp" => MemberType.Timestamp,
            "TestObjectId" => MemberType.Tag,
            "Status" => MemberType.Tag,
            "MachineId" => MemberType.Tag,
            "TestObjectType" => MemberType.Tag,
            "User" => MemberType.Tag,
            _ => MemberType.Field
        };
    }

    public string GetColumnName(MemberInfo memberInfo)
    {
        return memberInfo.Name.ToLower();
    }

    public string GetNamedFieldName(MemberInfo memberInfo, object value)
    {
        return memberInfo.Name.ToLower();
    }
    
    public List<PointData> ConvertLeakTestsToPoints(IEnumerable<LeakTest> entities, WritePrecision precision)
    {
        return entities.Select(entity => ConvertToPointData(entity, precision)).ToList();
    }
}
