using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TimeTracker.Core;

namespace TimeTracker.Infrastructure;

public sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("cards");

        // The normalized UID is the identity; the PK enforces global uniqueness (touch looks up by it).
        builder.HasKey(c => c.Uid);
        builder.Property(c => c.Uid).HasColumnName("card_uid").HasMaxLength(64);

        builder.Property(c => c.UserId);
        builder.Property(c => c.AssignedAt);

        // The user_id FK + index + cascade are configured from UserConfiguration (User.Cards).
    }
}
