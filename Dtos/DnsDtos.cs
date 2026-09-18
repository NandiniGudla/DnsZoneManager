namespace DnsZoneManager.Dtos;

/// <summary>Lightweight row for the zone list sidebar.</summary>
public record ZoneSummaryDto(int Id, string Name, int RecordCount, DateTime CreatedUtc);

/// <summary>Row for the record list in the zone detail view.</summary>
public record RecordDto(int Id, string Name, string Type, int Ttl, string Data, DateTime ModifiedUtc);

/// <summary>Full zone, including its records, for the detail view.</summary>
public record ZoneDetailDto(int Id, string Name, DateTime CreatedUtc, List<RecordDto> Records);

/// <summary>
/// Request to create a new DNS zone.
/// </summary>
/// <param name="Name">The name of the zone to create.</param>
public record CreateZoneRequest(string Name);

/// <summary>
/// Request to update an existing DNS zone. 
/// </summary>
/// <param name="Name">The new name of the zone.</param>
public record UpdateZoneRequest(string Name);

/// <summary>
/// Request to create a new DNS record.
/// </summary>
/// <param name="Name">The name of the record to create.</param>
/// <param name="Type">The type of the record to create.</param>
/// <param name="Ttl">The time-to-live of the record to create.</param>
/// <param name="Data">The data of the record to create.</param>
public record CreateRecordRequest(string Name, string Type, int Ttl, string Data);

/// <summary>
/// Request to update an existing DNS record.
/// </summary>
/// <param name="Name">The name of the record to update.</param>
/// <param name="Type">The type of the record to update.</param>
/// <param name="Ttl">The time-to-live of the record to update.</param>
/// <param name="Data">The data of the record to update.</param>
public record UpdateRecordRequest(string Name, string Type, int Ttl, string Data);
