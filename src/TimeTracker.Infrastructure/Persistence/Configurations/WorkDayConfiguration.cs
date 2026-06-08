using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class WorkDayConfiguration : IEntityTypeConfiguration<WorkDay>
{
    public void Configure(EntityTypeBuilder<WorkDay> builder)
    {
        builder.ToTable("work_days");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever(); // Guid v7 assigned in the domain

        builder.Property(d => d.UserId);
        builder.Property(d => d.Date);        // date
        builder.Property(d => d.ArrivalAt);   // timestamptz, null until first tap
        builder.Property(d => d.DepartureAt); // timestamptz, null until second tap

        // One row per (user, date) — backs the touch lookup and prevents duplicate days.
        builder.HasIndex(d => new { d.UserId, d.Date }).IsUnique();

        // Date-ordered scans for global history(limit) / statistics(period).
        builder.HasIndex(d => d.Date);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
