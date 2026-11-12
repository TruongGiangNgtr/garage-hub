using GarageHub.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageHub.Repository.Configurations;

public class ServiceBookingConfiguration : IEntityTypeConfiguration<ServiceBooking>
{
    public void Configure(EntityTypeBuilder<ServiceBooking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("DATEADD(HOUR, 7, GETUTCDATE())");
        builder.HasIndex(b => new { b.GarageId, b.ScheduledAt });

        builder.HasOne(b => b.Vehicle)
            .WithMany(v => v.Bookings)
            .HasForeignKey(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Garage)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.GarageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
