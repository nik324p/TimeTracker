using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.ToTable("work_schedules");

        // One schedule per user → user_id is the primary key (set upserts).
        builder.HasKey(w => w.UserId);
        builder.Property(w => w.UserId).ValueGeneratedNever();

        builder.Property(w => w.StartTime); // time
        builder.Property(w => w.EndTime);   // time
        builder.Property(w => w.Days);      // WorkDays [Flags] enum → integer bitmask
        builder.Property(w => w.FreeSchedule);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
