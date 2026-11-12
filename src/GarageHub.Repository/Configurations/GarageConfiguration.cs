using GarageHub.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageHub.Repository.Configurations;

public class GarageConfiguration : IEntityTypeConfiguration<Garage>
{
    public void Configure(EntityTypeBuilder<Garage> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);
        builder.Property(g => g.Address).IsRequired().HasMaxLength(500);
        builder.Property(g => g.Phone).IsRequired().HasMaxLength(20);
        builder.Property(g => g.AvgRating).HasPrecision(3, 2);

        // A garage has at most one owner account.
        builder.HasOne(g => g.Owner)
            .WithMany(u => u.Garages)
            .HasForeignKey(g => g.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // SAMPLE DATA ONLY - for development/demo. OwnerId is null (unassigned).
        builder.HasData(
            new Garage { Id = 1, Name = "Sample Garage 1", Address = "1 Sample Street, District 1, Ho Chi Minh City", Phone = "0900000001", AvgRating = 0m, CapacityPerSlot = 2 },
            new Garage { Id = 2, Name = "Sample Garage 2", Address = "2 Sample Street, District 3, Ho Chi Minh City", Phone = "0900000002", AvgRating = 0m, CapacityPerSlot = 2 },
            new Garage { Id = 3, Name = "Sample Garage 3", Address = "3 Sample Street, Thu Duc City, Ho Chi Minh City", Phone = "0900000003", AvgRating = 0m, CapacityPerSlot = 2 });
    }
}
