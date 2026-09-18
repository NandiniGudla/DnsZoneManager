namespace DnsZoneManager.Models
{
    /// <summary>
    /// Represents a DNS zone, which is a collection of DNS records for a specific domain.
    /// </summary>
    public class DnsZone
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<DnsRecord> Records { get; set; } = new List<DnsRecord>();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    }
}