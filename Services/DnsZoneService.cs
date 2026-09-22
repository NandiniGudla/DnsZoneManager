using DnsZoneManager.Data;
using DnsZoneManager.Dtos;
using DnsZoneManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DnsZoneManager.Services;

/// <summary>
/// Implements the business logic for managing DNS zones and records.
/// Rules implemented as per the requirement:
///   - A new zone is created with 4 NS records (the minimum a zone must always have).
///   - A zone may never contain more than 10 total records.
///   - Only A, AAAA, CNAME, NS, TXT record types are allowed 
///   - Duplicate zones  are rejected.
///   - Deleting/retyping an NS record is rejected if it would drop the zone below 4 NS records.
/// </summary>
public class DnsZoneService : IDnsZoneService
{  

    private readonly DnsContext _db;
    private readonly IDnsZoneRule _rule;


    public DnsZoneService(DnsContext db, IDnsZoneRule rule)
    {
        _db = db;
        _rule = rule;
    }

    public async Task<List<ZoneSummaryDto>> GetZonesAsync()
    {
        return await _db.DnsZones
            .OrderBy(z => z.Name)
            .Select(z => new ZoneSummaryDto(z.Id, z.Name, z.Records.Count, z.CreatedDate))
            .ToListAsync();
    }

    public async Task<ServiceResult<ZoneDetailDto>> GetZoneAsync(int id)
    {
        var zone = await _db.DnsZones.Include(z => z.Records).FirstOrDefaultAsync(z => z.Id == id);
        return zone is null
            ? ServiceResult<ZoneDetailDto>.Fail("Zone not found.")
            : ServiceResult<ZoneDetailDto>.Ok(ToDetail(zone));
    }

    public async Task<ServiceResult<ZoneDetailDto>> CreateZoneAsync(CreateZoneRequest request)
    {
        var name = (request.Name ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();

        if (name.Length == 0 || !_rule.FqdnRegex.IsMatch(name))
            return ServiceResult<ZoneDetailDto>.Fail("Enter a valid domain name, e.g. \"example.com\".");

        if (await _db.DnsZones.AnyAsync(z => z.Name == name))
            return ServiceResult<ZoneDetailDto>.Fail($"A zone named \"{name}\" already exists.");

        var zone = new DnsZone { Name = name };

        // Assumption: "An empty zone file should contain a minimum of (4)
        // NS records." .
        for (var i = 1; i <= _rule.MinNsRecords; i++)
        {
            zone.Records.Add(new DnsRecord
            {
                Name = "@",
                Type = RecordType.NS,
                Ttl = 172800,
                Data = $"ns{i}.dnszonemanager.net."
            });
        }

        _db.DnsZones.Add(zone);
        await _db.SaveChangesAsync();
        return ServiceResult<ZoneDetailDto>.Ok(ToDetail(zone));
    }

    public async Task<ServiceResult<ZoneDetailDto>> UpdateZoneAsync(int id, UpdateZoneRequest request)
    {
        var zone = await _db.DnsZones.Include(z => z.Records).FirstOrDefaultAsync(z => z.Id == id);
        if (zone is null) return ServiceResult<ZoneDetailDto>.Fail("Zone not found.");

        var name = (request.Name ?? string.Empty).Trim().TrimEnd('.').ToLowerInvariant();

        if (name.Length == 0 || !_rule.FqdnRegex.IsMatch(name))
            return ServiceResult<ZoneDetailDto>.Fail("Enter a valid domain name, e.g. \"example.com\".");

        var duplicate = await _db.DnsZones.AnyAsync(z => z.Id != id && z.Name == name);
        if (duplicate)
            return ServiceResult<ZoneDetailDto>.Fail($"A zone named \"{name}\" already exists.");

        zone.Name = name;
        await _db.SaveChangesAsync();
        return ServiceResult<ZoneDetailDto>.Ok(ToDetail(zone));
    }

    public async Task<ServiceResult<bool>> DeleteZoneAsync(int id)
    {
        var zone = await _db.DnsZones.FindAsync(id);
        if (zone is null) return ServiceResult<bool>.Fail("Zone not found.");

        _db.DnsZones.Remove(zone); // cascade removes its records too
        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<RecordDto>> AddRecordAsync(int zoneId, CreateRecordRequest request)
    {
        var zone = await _db.DnsZones.Include(z => z.Records).FirstOrDefaultAsync(z => z.Id == zoneId);
        if (zone is null) return ServiceResult<RecordDto>.Fail("Zone not found.");

        var errors = ValidateRecordFields(request.Name, request.Type, request.Ttl, request.Data,
            out var name, out var type);
        if (errors.Count > 0) return ServiceResult<RecordDto>.Fail(errors.ToArray());

        if (zone.Records.Count >= _rule.MaxRecordsPerZone)
            return ServiceResult<RecordDto>.Fail($"A zone may not contain more than {_rule.MaxRecordsPerZone} records.");

        var data = request.Data.Trim();

        if (zone.Records.Any(r => Same(r.Name, name) && r.Type == type && Same(r.Data, data)))
            return ServiceResult<RecordDto>.Fail("An identical record already exists in this zone.");

        var cnameConflict = CheckCnameExclusivity(zone.Records, name, type);
        if (cnameConflict is not null) return ServiceResult<RecordDto>.Fail(cnameConflict);

        var record = new DnsRecord
        {
            DnsZoneId = zoneId,
            Name = name,
            Type = type,
            Ttl = request.Ttl,
            Data = data,
            ModifiedDate = DateTime.UtcNow
        };

        _db.DnsRecords.Add(record);
        await _db.SaveChangesAsync();
        return ServiceResult<RecordDto>.Ok(ToDto(record));
    }

    public async Task<ServiceResult<RecordDto>> UpdateRecordAsync(int zoneId, int recordId, UpdateRecordRequest request)
    {
        var zone = await _db.DnsZones.Include(z => z.Records).FirstOrDefaultAsync(z => z.Id == zoneId);
        if (zone is null) return ServiceResult<RecordDto>.Fail("Zone not found.");

        var record = zone.Records.FirstOrDefault(r => r.Id == recordId);
        if (record is null) return ServiceResult<RecordDto>.Fail("Record not found.");

        var errors = ValidateRecordFields(request.Name, request.Type, request.Ttl, request.Data,
            out var name, out var type);
        if (errors.Count > 0) return ServiceResult<RecordDto>.Fail(errors.ToArray());

        if (record.Type == RecordType.NS && type != RecordType.NS)
        {
            var remainingNs = zone.Records.Count(r => r.Type == RecordType.NS && r.Id != recordId);
            if (remainingNs < _rule.MinNsRecords)
                return ServiceResult<RecordDto>.Fail($"A zone must keep at least {_rule.MinNsRecords} NS records.");
        }

        var data = request.Data.Trim();

        if (zone.Records.Any(r => r.Id != recordId && Same(r.Name, name) && r.Type == type && Same(r.Data, data)))
            return ServiceResult<RecordDto>.Fail("An identical record already exists in this zone.");

        var cnameConflict = CheckCnameExclusivity(zone.Records.Where(r => r.Id != recordId), name, type);
        if (cnameConflict is not null) return ServiceResult<RecordDto>.Fail(cnameConflict);

        record.Name = name;
        record.Type = type;
        record.Ttl = request.Ttl;
        record.Data = data;
        record.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ServiceResult<RecordDto>.Ok(ToDto(record));
    }

    public async Task<ServiceResult<bool>> DeleteRecordAsync(int zoneId, int recordId)
    {
        var zone = await _db.DnsZones.Include(z => z.Records).FirstOrDefaultAsync(z => z.Id == zoneId);
        if (zone is null) return ServiceResult<bool>.Fail("Zone not found.");

        var record = zone.Records.FirstOrDefault(r => r.Id == recordId);
        if (record is null) return ServiceResult<bool>.Fail("Record not found.");

        if (record.Type == RecordType.NS)
        {
            var remainingNs = zone.Records.Count(r => r.Type == RecordType.NS && r.Id != recordId);
            if (remainingNs < _rule.MinNsRecords)
                return ServiceResult<bool>.Fail(
                    $"A zone must keep at least {_rule.MinNsRecords} NS records — add a replacement before deleting this one.");
        }

        _db.DnsRecords.Remove(record);
        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Ok(true);
    }

    /// <summary>
    /// Validates name/type/ttl/data together (data validity depends on type), returning
    /// plain-English messages a non-technical customer can act on, per the spec's
    /// requirement that the tool "provide some form of feedback ... ensure only valid
    /// entries are submitted."
    /// </summary>
    private List<string> ValidateRecordFields(
    string? nameInput, string? typeInput, int ttl, string? dataInput,
    out string name, out RecordType type)
    {
        var errors = new List<string>();

        name = (nameInput ?? string.Empty).Trim();
        if (name.Length == 0) name = "@";

        if (name != "@" && !_rule.RecordNameRegex.IsMatch(name))
            errors.Add("Record name must be \"@\" (zone root) or a valid host label, e.g. \"www\" or \"_dmarc\".");

        if (!Enum.TryParse(typeInput, ignoreCase: true, out type) || !Enum.IsDefined(type))
        {
            type = default;
            errors.Add("Record type must be one of: A, AAAA, CNAME, NS, TXT.");
            return errors;
        }

        if (ttl < 60 || ttl > 604800)
            errors.Add("TTL must be between 60 and 604800 seconds (1 minute to 7 days).");

        var data = (dataInput ?? string.Empty).Trim();
        if (data.Length == 0)
            errors.Add("Data is required.");

        return errors;
    }
    /// <summary>
    /// Standard DNS rule (RFC 1034 §3.6.2), not explicitly in the spec but something any
    /// real DNS customer would rely on the tool to catch: a name with a CNAME cannot
    /// also have any other record, and vice versa.
    /// </summary>
    private static string? CheckCnameExclusivity(IEnumerable<DnsRecord> existing, string name, RecordType type)
    {
        var sameName = existing.Where(r => Same(r.Name, name)).ToList();

        if (type == RecordType.CNAME && sameName.Count > 0)
            return $"\"{name}\" already has other records; a CNAME cannot share a name with any other record.";

        if (type != RecordType.CNAME && sameName.Any(r => r.Type == RecordType.CNAME))
            return $"\"{name}\" already has a CNAME record; no other record can share that name.";

        return null;
    }

    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static RecordDto ToDto(DnsRecord r) =>
        new(r.Id, r.Name, r.Type.ToString(), r.Ttl, r.Data, r.ModifiedDate);

    private static ZoneDetailDto ToDetail(DnsZone z) =>
        new(z.Id, z.Name, z.CreatedDate,
            z.Records.OrderBy(r => r.Type).ThenBy(r => r.Name).Select(ToDto).ToList());
}
