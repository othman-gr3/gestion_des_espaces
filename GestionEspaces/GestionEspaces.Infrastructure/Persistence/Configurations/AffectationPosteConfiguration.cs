using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class AffectationPosteConfiguration : IEntityTypeConfiguration<AffectationPoste>
{
    public void Configure(EntityTypeBuilder<AffectationPoste> builder)
    {
        builder.ToTable("AffectationsPoste");

        builder.HasKey(affectation => affectation.IdAffectationPoste);

        builder.Property(affectation => affectation.DateAffectation)
            .IsRequired();

        builder.Property(affectation => affectation.DateFin);

        builder.Property(affectation => affectation.Motif)
            .HasMaxLength(100);

        builder.HasIndex(affectation => affectation.IdAgent)
            .IsUnique()
            .HasFilter("[DateFin] IS NULL");

        builder.HasIndex(affectation => affectation.IdBureau)
            .IsUnique()
            .HasFilter("[DateFin] IS NULL");

        builder.HasOne(affectation => affectation.Agent)
            .WithMany(agent => agent.AffectationsPoste)
            .HasForeignKey(affectation => affectation.IdAgent)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(affectation => affectation.Bureau)
            .WithMany(bureau => bureau.Affectations)
            .HasForeignKey(affectation => affectation.IdBureau)
            .OnDelete(DeleteBehavior.NoAction);
    }
}