using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(entry => entry.IdAuditLog);

        builder.Property(entry => entry.OccurredOnUtc)
            .IsRequired();

        builder.Property(entry => entry.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entry => entry.Payload)
            .IsRequired();

        builder.Property(entry => entry.UtilisateurEmail)
            .HasMaxLength(100);

        builder.Property(entry => entry.UtilisateurRole)
            .HasMaxLength(50);

        builder.HasIndex(entry => entry.OccurredOnUtc);
    }
}
