using DnsZoneManager.Models;
using Microsoft.EntityFrameworkCore;

namespace DnsZoneManager.Data;
public class DnsContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DnsContext"/> class using the specified options.
    /// </summary>
    /// <param name="options"></param>
    public DnsContext(DbContextOptions<DnsContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the <see cref="DbSet{DnsZone}"/> representing the DNS zones in the database.
    /// </summary>
    public DbSet<DnsZone> DnsZones => Set<DnsZone>();
    /// <summary>
    /// Gets the <see cref="DbSet{DnsRecord}"/> representing the DNS records in the database.
    /// </summary>
    public DbSet<DnsRecord> DnsRecords => Set<DnsRecord>();

    /// <summary>
    /// Configures the model for the context, including relationships and constraints.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to use.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DnsZone>()
            .HasIndex(z => z.Name)
            .IsUnique();

        modelBuilder.Entity<DnsZone>()
            .HasMany(z => z.Records)
            .WithOne(r => r.DnsZone)
            .HasForeignKey(r => r.DnsZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DnsRecord>()
            .Property(r => r.Type)
            .HasConversion<string>();
    }   

} 