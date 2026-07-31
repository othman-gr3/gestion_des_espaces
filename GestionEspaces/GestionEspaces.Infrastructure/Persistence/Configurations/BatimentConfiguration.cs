using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class BatimentConfiguration : IEntityTypeConfiguration<Batiment>
{
    public void Configure(EntityTypeBuilder<Batiment> builder)
    {
        builder.ToTable("Batiments");

        builder.HasKey(batiment => batiment.IdBatiment);

        builder.Property(batiment => batiment.Version)
            .IsRowVersion();

        builder.Property(batiment => batiment.Nom)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(batiment => batiment.Superficie)
            .HasColumnType("real");

        builder.Property(batiment => batiment.Image)
            .HasMaxLength(500);

        builder.HasMany(batiment => batiment.Bureaux)
            .WithOne(bureau => bureau.Batiment)
            .HasForeignKey(bureau => bureau.IdBatiment)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(batiment => batiment.Site)
            .WithMany(site => site.Batiments)
            .HasForeignKey(batiment => batiment.IdSite)
            .OnDelete(DeleteBehavior.Restrict);
    }
}