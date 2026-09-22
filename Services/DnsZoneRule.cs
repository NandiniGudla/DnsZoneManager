using System.Text.RegularExpressions;

namespace DnsZoneManager.Services
{
    public class DnsZoneRule : IDnsZoneRule
    {
        public int MaxRecordsPerZone => 10;
        public int MinNsRecords => 4;
        public Regex FqdnRegex => new(
        @"^(?!-)[A-Za-z0-9-]{1,63}(?<!-)(\.(?!-)[A-Za-z0-9-]{1,63}(?<!-))+$",
        RegexOptions.Compiled);
        public Regex RecordNameRegex =>  new(
        @"^(@|(\*\.)?(_)?[A-Za-z0-9-]{1,63}(\.[A-Za-z0-9_-]{1,63})*)$",
        RegexOptions.Compiled);
    }
}