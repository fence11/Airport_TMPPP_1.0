using Airport_TMPPP_1._0.Server.BusinessLogic.Entities;
using Airport_TMPPP_1._0.Server.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Airport_TMPPP_1._0.Server.Data.Context
{

    /// Database context implementation following Single Responsibility Principle (SRP)
    /// Responsible only for database configuration and connection management

    public class AirportDbContext : DbContext, IDbContext
    {
        public AirportDbContext(DbContextOptions<AirportDbContext> options) : base(options)
        {
        }

        public DbSet<Airport> Airports { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Passenger> Passengers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Airport entity
            modelBuilder.Entity<Airport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure Flight entity
            modelBuilder.Entity<Flight>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FlightNumber).IsRequired().HasMaxLength(20);
                entity.HasIndex(e => e.FlightNumber).IsUnique();
                entity.HasOne<Airport>()
                    .WithMany()
                    .HasForeignKey(e => e.AirportId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Passenger entity
            modelBuilder.Entity<Passenger>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
}

