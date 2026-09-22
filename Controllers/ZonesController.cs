using DnsZoneManager.Dtos;
using DnsZoneManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace DnsZoneManager.Controllers;

[ApiController]
[Route("api/zones")]
public class ZonesController : ApiControllerBase
{
    /// <summary>
    /// The service that handles DNS zone operations.
    /// </summary>
    private readonly IDnsZoneService _svc;
    protected override string ResourceName => "DNS zone";

    /// <summary>
    /// Initializes a new instance of the <see cref="ZonesController"/> class with the specified DNS zone service.
    /// </summary>
    /// <param name="svc"></param>
    public ZonesController(IDnsZoneService svc)
    {
        _svc = svc;
    }
    /// <summary>
    /// Gets a list of all DNS zones.
    /// </summary>
    /// <returns>ZoneSummaryDto List</returns>
    [HttpGet]
    public async Task<ActionResult<List<ZoneSummaryDto>>> GetZones()
    {
        return Ok(await _svc.GetZonesAsync());
    }

    /// <summary>
    /// Gets the details of a specific DNS zone by its ID.
    /// </summary>
    /// <param name="id">The ID of the DNS zone to retrieve.</param>
    /// <returns>The DNS zone details.</returns>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetZone(int id)
    {
        var result = await _svc.GetZoneAsync(id);
        return ToActionResult(result, Ok);
    }

    /// <summary>
    /// Creates a new DNS zone with the specified details.
    /// </summary>
    /// <param name="request">The request containing the details for the new DNS zone.</param>
    /// <returns>The created DNS zone.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateZone(CreateZoneRequest request)
    {
        var result = await _svc.CreateZoneAsync(request);
        return ToActionResult(result, data => Created($"/api/zones/{data.Id}", data));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateZone(int id, UpdateZoneRequest request)
    {
        var result = await _svc.UpdateZoneAsync(id, request);
        return ToActionResult(result, Ok);
    }
    /// <summary>
    /// Deletes a specific DNS zone by its ID.
    /// </summary>
    /// <param name="id">The ID of the DNS zone to delete.</param>
    /// <returns>boolean value indicating success or failure</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var result = await _svc.DeleteZoneAsync(id);
        return ToActionResult(result, _ => NoContent());
    }
    
}