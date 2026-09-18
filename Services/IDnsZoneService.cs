using DnsZoneManager.Dtos;

namespace DnsZoneManager.Services;

public interface IDnsZoneService
{
    Task<List<ZoneSummaryDto>> GetZonesAsync();
    Task<ServiceResult<ZoneDetailDto>> GetZoneAsync(int id);
    Task<ServiceResult<ZoneDetailDto>> CreateZoneAsync(CreateZoneRequest request);
    Task<ServiceResult<ZoneDetailDto>> UpdateZoneAsync(int id, UpdateZoneRequest request);
    Task<ServiceResult<bool>> DeleteZoneAsync(int id);

    Task<ServiceResult<RecordDto>> AddRecordAsync(int zoneId, CreateRecordRequest request);
    Task<ServiceResult<RecordDto>> UpdateRecordAsync(int zoneId, int recordId, UpdateRecordRequest request);
    Task<ServiceResult<bool>> DeleteRecordAsync(int zoneId, int recordId);
}
