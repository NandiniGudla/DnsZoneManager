namespace DnsZoneManager.Models
{
    /// <summary>
    /// Represents a DNS record associated with a DNS zone.
    /// </summary>
    public class DnsRecord
    {
        public int Id { get; set; }
        public int DnsZoneId { get; set; }
        public DnsZone? DnsZone { get; set; }
        public string Name { get; set; } = "@";
        public int Ttl { get; set; } = 172800;
        public RecordType Type { get; set; }
        public string Data { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    }
}