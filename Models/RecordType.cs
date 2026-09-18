namespace DnsZoneManager.Models
{
    /// <summary>
    /// Represents the type of a DNS record.
    /// </summary>
    public enum RecordType
    { 
        A, 
        AAAA, 
        CNAME, 
        NS, 
        TXT        
    }
}