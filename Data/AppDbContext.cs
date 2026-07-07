using BlueHarbor.Domain;
using Microsoft.EntityFrameworkCore;

namespace BlueHarbor.Data;

/// <summary>
/// Contesto EF Core del dominio BlueHarbor.
/// Definisce le entity e la configurazione dello schema.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Ship> Ships => Set<Ship>();
    public DbSet<Berth> Berths => Set<Berth>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<TerminalClock> TerminalClocks => Set<TerminalClock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ship>(e =>
        {
            e.Property(s => s.Name).IsRequired();
            e.Property(s => s.Notes).HasMaxLength(1000);
            e.Property(s => s.Size).HasConversion<string>().IsRequired();
            e.Property(s => s.Status).HasConversion<string>().IsRequired();
            e.Property(s => s.ArrivalDay).IsRequired();
            e.Property(s => s.OccupationDuration).IsRequired();
        });

        modelBuilder.Entity<Berth>(e =>
        {
            e.Property(b => b.Code).IsRequired();
            e.HasIndex(b => b.Code).IsUnique();
            e.Property(b => b.Size).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<Assignment>(e =>
        {
            e.HasOne(a => a.Ship)
                .WithMany()
                .HasForeignKey(a => a.ShipId)
                .IsRequired();
            e.HasIndex(a => a.ShipId).IsUnique(); // una nave -> al massimo una assegnazione
            e.HasOne(a => a.Berth)
                .WithMany()
                .HasForeignKey(a => a.BerthId)
                .IsRequired();
            e.Property(a => a.StartDay).IsRequired();
            e.Property(a => a.EndDay).IsRequired();
        });

        modelBuilder.Entity<TerminalClock>(e =>
        {
            // Orologio a riga singola: id impostato manualmente, non generato.
            e.Property(c => c.Id).ValueGeneratedNever();
            e.Property(c => c.CurrentDay).IsRequired();
        });
    }
}
