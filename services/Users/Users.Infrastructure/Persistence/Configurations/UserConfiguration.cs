using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;
using Venture.Infrastructure.Persistence.Serialization;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasConversion(id => id.Value, value => UserId.From(value).Value)
            .ValueGeneratedNever();

        builder.Property(user => user.Username)
            .HasConversion(username => username.Value, value => Username.Create(value).Value)
            .HasMaxLength(Username.MaxLength)
            .IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();

        builder.Property(user => user.Email)
            .HasConversion(email => email.Value, value => Email.Create(value).Value)
            .HasMaxLength(254)
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasConversion(hash => hash.Value, value => PasswordHash.FromHash(value).Value)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasConversion(displayName => displayName.Value, value => DisplayName.Create(value).Value)
            .HasMaxLength(DisplayName.MaxLength)
            .IsRequired();

        builder.Property(user => user.Bio)
            .HasConversion(
                bio => BioSerializer.Serialize(bio),
                json => BioSerializer.Deserialize(json))
            .Metadata.SetValueComparer(new ValueComparer<Bio>(
                (left, right) => left!.Equals(right),
                bio => bio.GetHashCode(),
                bio => bio));

        builder.Property(user => user.CreatedAtUtc).IsRequired();

        builder.Ignore(user => user.DomainEvents);

        builder.OwnsMany(user => user.Skills, skill =>
        {
            skill.ToTable("UserSkills");
            skill.WithOwner().HasForeignKey("UserId");

            skill.Property<int>("Id");
            skill.HasKey("Id");

            skill.Property(userSkill => userSkill.Name)
                .HasConversion(name => name.Value, value => SkillName.Create(value).Value)
                .HasColumnName("Name")
                .HasMaxLength(SkillName.MaxLength)
                .IsRequired();

            skill.Property(userSkill => userSkill.Level)
                .HasConversion<string>()
                .HasColumnName("Level")
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}
