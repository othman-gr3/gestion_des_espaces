using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");

        builder.HasKey(site => site.IdSite);

        builder.Property(site => site.Version)
            .IsRowVersion();

        builder.Property(site => site.Nom)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(site => site.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(site => site.Code)
            .IsUnique();

        builder.Property(site => site.Image)
            .HasMaxLength(500);

        builder.OwnsOne(site => site.Adresse, adresse =>
        {
            adresse.Property(value => value.Rue).HasColumnName("AdresseRue").HasMaxLength(250).IsRequired();
            adresse.Property(value => value.Ville).HasColumnName("AdresseVille").HasMaxLength(150).IsRequired();
            adresse.Property(value => value.CodePostal).HasColumnName("AdresseCodePostal").HasMaxLength(20).IsRequired();
            adresse.Property(value => value.Pays).HasColumnName("AdressePays").HasMaxLength(100).IsRequired();
        });

        builder.Navigation(site => site.Adresse).IsRequired();

        builder.HasMany(site => site.Batiments)
            .WithOne(batiment => batiment.Site)
            .HasForeignKey(batiment => batiment.IdSite)
            .OnDelete(DeleteBehavior.Restrict);
    }
}