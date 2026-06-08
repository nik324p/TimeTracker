using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever(); // user ids are externally assigned / seeded
        builder.Property(u => u.DisplayName);

        // One user → many cards. Card has no reference navigation, so the inverse is the User.Cards
        // collection (mapped via its backing field). The FK + its index live on the cards table.
        builder.HasMany(u => u.Cards)
            .WithOne()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
