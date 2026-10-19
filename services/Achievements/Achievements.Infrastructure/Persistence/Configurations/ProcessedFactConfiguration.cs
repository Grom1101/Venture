using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Venture.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedFactConfiguration : IEntityTypeConfiguration<ProcessedFact>
{
    public void Configure(EntityTypeBuilder<ProcessedFact> builder)
    {
        builder.ToTable("ProcessedFacts");

        builder.HasKey(fact => fact.EventId);

        builder.Property(fact => fact.EventId)
            .HasMaxLength(80)
            .ValueGeneratedNever();

        builder.Property(fact => fact.AcceptedOnUtc).IsRequired();
    }
}
