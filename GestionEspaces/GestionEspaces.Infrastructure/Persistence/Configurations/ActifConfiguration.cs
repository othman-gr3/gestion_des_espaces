using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class ActifConfiguration : IEntityTypeConfiguration<Actif>
{
    public void Configure(EntityTypeBuilder<Actif> builder)
    {
        builder.ToTable("Actifs");

        builder.HasKey(actif => actif.IdActif);

        builder.Property(actif => actif.Version)
            .IsRowVersion();

        builder.Property(actif => actif.Nom)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(actif => actif.Type)
            .HasMaxLength(100);

        builder.Property(actif => actif.Marque)
            .HasMaxLength(100);

        builder.Property(actif => actif.Modele)
            .HasMaxLength(100);

        builder.Property(actif => actif.NumeroSerie)
            .HasMaxLength(150);

        builder.HasIndex(actif => actif.NumeroSerie)
            .IsUnique()
            .HasFilter("[NumeroSerie] IS NOT NULL");

        builder.Property(actif => actif.Image)
            .HasMaxLength(500);

        builder.Property(actif => actif.Etat)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasMany(actif => actif.Affectations)
            .WithOne(affectation => affectation.Actif)
            .HasForeignKey(affectation => affectation.IdActif)
            .OnDelete(DeleteBehavior.NoAction);
    }
}