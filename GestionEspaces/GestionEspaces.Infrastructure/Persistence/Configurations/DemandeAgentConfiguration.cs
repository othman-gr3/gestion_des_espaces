using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class DemandeAgentConfiguration : IEntityTypeConfiguration<DemandeAgent>
{
    public void Configure(EntityTypeBuilder<DemandeAgent> builder)
    {
        builder.ToTable("Demandes");

        builder.HasKey(demande => demande.IdDemande);

        builder.Property(demande => demande.Version)
            .IsRowVersion();

        builder.Property(demande => demande.Type)
            .IsRequired();

        builder.Property(demande => demande.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(demande => demande.Statut)
            .IsRequired();

        builder.Property(demande => demande.DateCreation)
            .IsRequired();

        builder.Property(demande => demande.Reponse)
            .HasMaxLength(1000);

        builder.HasOne(demande => demande.Agent)
            .WithMany()
            .HasForeignKey(demande => demande.IdAgent)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(demande => demande.Statut);
    }
}
