using System.Text.RegularExpressions;

namespace DnsZoneManager.Services
{
    public interface IDnsZoneRule
    {
        int MaxRecordsPerZone { get; }
        int MinNsRecords { get; }
        Regex FqdnRegex { get; }
        Regex RecordNameRegex { get; } 
    }
}