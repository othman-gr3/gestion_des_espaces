using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEspaces.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.IdRefreshToken);

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.UserEmail)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(token => token.UserEmail);

        builder.Property(token => token.CreatedAtUtc)
            .IsRequired();

        builder.Property(token => token.ExpiresAtUtc)
            .IsRequired();
    }
}
