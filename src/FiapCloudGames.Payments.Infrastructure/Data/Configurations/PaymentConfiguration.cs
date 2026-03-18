using FiapCloudGames.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FiapCloudGames.Payments.Infrastructure.Data.Configurations;
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Tabela
        builder.ToTable("Payments");

        // Chave primária
        builder.HasKey(p => p.Id);

        // Propriedades
        builder.Property(p => p.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(p => p.OrderId)
            .IsRequired();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.GameId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.TransactionId)
            .HasMaxLength(20);

        builder.Property(p => p.CardLastFourDigits)
            .HasMaxLength(4);

        builder.Property(p => p.RejectionReason)
            .HasMaxLength(200);

        builder.Property(p => p.ProcessingTimeMs)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.ProcessedAt);

        // Índices
        builder.HasIndex(p => p.OrderId)
            .HasDatabaseName("IX_Payments_OrderId");

        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Payments_UserId");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Payments_Status");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");

        builder.HasIndex(p => p.TransactionId)
            .IsUnique()
            .HasDatabaseName("IX_Payments_TransactionId")
            .HasFilter("\"TransactionId\" IS NOT NULL");
    }
}