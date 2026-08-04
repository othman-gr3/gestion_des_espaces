using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations");

        builder.HasKey(r => r.IdReservation);

        builder.Property(r => r.Version)
            .IsRowVersion()
            .IsRequired();

        builder.Property(r => r.DateDebut)
            .IsRequired();

        builder.Property(r => r.DateFin)
            .IsRequired();

        builder.Property(r => r.Statut)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Motif)
            .HasMaxLength(500);

        // Index for conflict detection queries: find overlapping reservations for same bureau.
        builder.HasIndex(r => new { r.IdBureau, r.DateDebut, r.DateFin });

        builder.HasOne(r => r.Bureau)
            .WithMany()
            .HasForeignKey(r => r.IdBureau)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.Agent)
            .WithMany()
            .HasForeignKey(r => r.IdAgent)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
