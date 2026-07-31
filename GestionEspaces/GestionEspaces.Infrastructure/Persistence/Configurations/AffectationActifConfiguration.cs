using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class AffectationActifConfiguration : IEntityTypeConfiguration<AffectationActif>
{
    public void Configure(EntityTypeBuilder<AffectationActif> builder)
    {
        builder.ToTable("AffectationsActif");

        builder.HasKey(affectation => affectation.IdAffectationActif);

        builder.Property(affectation => affectation.DateAffectation)
            .IsRequired();

        builder.Property(affectation => affectation.DateFin);

        builder.HasIndex(affectation => affectation.IdActif)
            .IsUnique()
            .HasFilter("[DateFin] IS NULL");

        builder.HasOne(affectation => affectation.Agent)
            .WithMany(agent => agent.AffectationsActif)
            .HasForeignKey(affectation => affectation.IdAgent)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(affectation => affectation.Actif)
            .WithMany(actif => actif.Affectations)
            .HasForeignKey(affectation => affectation.IdActif)
            .OnDelete(DeleteBehavior.NoAction);
    }
}