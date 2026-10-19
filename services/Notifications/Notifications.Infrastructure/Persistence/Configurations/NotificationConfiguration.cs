using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Application.Abstractions.Persistence.Models;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id).ValueGeneratedNever();

        builder.Property(notification => notification.RecipientId)
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .IsRequired();

        builder.Property(notification => notification.ActorId)
            .HasConversion(id => id!.Value, value => UserId.From(value).Value);

        builder.Property(notification => notification.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(notification => notification.ReferenceId);
        builder.Property(notification => notification.CreatedAtUtc).IsRequired();
        builder.Property(notification => notification.IsRead).IsRequired();

        builder.HasIndex(notification => new { notification.RecipientId, notification.CreatedAtUtc });
    }
}
