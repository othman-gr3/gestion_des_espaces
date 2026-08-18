using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class BureauConfiguration : IEntityTypeConfiguration<Bureau>
{
    public void Configure(EntityTypeBuilder<Bureau> builder)
    {
        builder.ToTable("Bureaux");

        builder.HasKey(bureau => bureau.IdBureau);

        builder.Property(bureau => bureau.Version)
            .IsRowVersion();

        builder.Property(bureau => bureau.Numero)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(bureau => bureau.Superficie)
            .HasColumnType("real");

        builder.Property(bureau => bureau.Image)
            .HasMaxLength(500);

        builder.HasIndex(bureau => new { bureau.IdBatiment, bureau.Numero })
            .IsUnique();

        builder.HasMany(bureau => bureau.Affectations)
            .WithOne(affectation => affectation.Bureau)
            .HasForeignKey(affectation => affectation.IdBureau)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(bureau => bureau.Batiment)
            .WithMany(batiment => batiment.Bureaux)
            .HasForeignKey(bureau => bureau.IdBatiment)
            .OnDelete(DeleteBehavior.Restrict);
    }
}