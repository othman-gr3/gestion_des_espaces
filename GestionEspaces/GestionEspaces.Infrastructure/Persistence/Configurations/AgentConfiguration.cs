using GestionEspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("Agents");

        builder.HasKey(agent => agent.IdAgent);

        builder.Property(agent => agent.Version)
            .IsRowVersion();

        builder.Property(agent => agent.Nom)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(agent => agent.Prenom)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(agent => agent.Matricule)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(agent => agent.Email)
            .HasMaxLength(250);

        builder.Property(agent => agent.Telephone)
            .HasMaxLength(30);

        builder.Property(agent => agent.Fonction)
            .HasMaxLength(150);

        builder.Property(agent => agent.Departement)
            .HasMaxLength(150);

        builder.Property(agent => agent.Image)
            .HasMaxLength(500);

        builder.HasIndex(agent => agent.Matricule)
            .IsUnique();

        builder.HasMany(agent => agent.AffectationsPoste)
            .WithOne(affectation => affectation.Agent)
            .HasForeignKey(affectation => affectation.IdAgent)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(agent => agent.AffectationsActif)
            .WithOne(affectation => affectation.Agent)
            .HasForeignKey(affectation => affectation.IdAgent)
            .OnDelete(DeleteBehavior.NoAction);
    }
}