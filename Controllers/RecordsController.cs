using DnsZoneManager.Dtos;
using DnsZoneManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace DnsZoneManager.Controllers;

[ApiController]
[Route("api/zones/{zoneId:int}/records")]
public class RecordsController : ApiControllerBase
{
    /// <summary>
    /// The service that handles DNS zone operations.
    /// </summary>
    private readonly IDnsZoneService _svc;

    protected override string ResourceName => "DNS record";

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordsController"/> class with the specified DNS zone service.
    /// </summary>
    /// <param name="svc">The DNS zone service.</param>
    public RecordsController(IDnsZoneService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Adds a new DNS record to the specified zone.
    /// </summary>
    /// <param name="zoneId">The ID of the zone to which to add the record.</param>
    /// <param name="request">The request containing the details for the new DNS record.</param>
    /// <returns>The created DNS record.</returns>
    [HttpPost]
    public async Task<IActionResult> AddRecord(int zoneId, CreateRecordRequest request)
    {
        var result = await _svc.AddRecordAsync(zoneId, request);
        return ToActionResult(result, data => Created($"/api/zones/{zoneId}/records/{data.Id}", data));
    }

    /// <summary>
    /// Updates an existing DNS record in the specified zone.
    /// </summary>
    /// <param name="zoneId">The ID of the zone containing the record.</param>
    /// <param name="recordId">The ID of the record to update.</param>
    /// <param name="request">The request containing the updated details for the DNS record.</param>
    /// <returns>The updated DNS record.</returns>
    [HttpPut("{recordId:int}")]
    public async Task<IActionResult> UpdateRecord(int zoneId, int recordId, UpdateRecordRequest request)
    {
        var result = await _svc.UpdateRecordAsync(zoneId, recordId, request);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Deletes a specific DNS record from the specified zone.
    /// </summary>
    /// <param name="zoneId">The ID of the zone containing the record.</param>
    /// <param name="recordId">The ID of the record to delete.</param>
    /// <returns>A boolean value indicating success or failure.</returns>
    [HttpDelete("{recordId:int}")]
    public async Task<IActionResult> DeleteRecord(int zoneId, int recordId)
    {
        var result = await _svc.DeleteRecordAsync(zoneId, recordId);
        return ToActionResult(result, _ => NoContent());
    }
}