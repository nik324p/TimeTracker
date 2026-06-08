using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class ScheduleExclusionConfiguration : IEntityTypeConfiguration<ScheduleExclusion>
{
    public void Configure(EntityTypeBuilder<ScheduleExclusion> builder)
    {
        builder.ToTable("schedule_exclusions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever(); // Guid v7 assigned in the domain

        builder.Property(e => e.UserId);

        // Stored as the enum name (stable across reorderings, readable in SQL) — not an ordinal int.
        builder.Property(e => e.Type)
            .HasColumnName("type_exclusion")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.StartDateTime).HasColumnName("start_datetime");
        builder.Property(e => e.EndDateTime).HasColumnName("end_datetime");

        // The user_id FK index is created by convention for the relationship below.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
