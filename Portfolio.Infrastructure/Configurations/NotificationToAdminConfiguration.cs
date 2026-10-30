using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Configurations;

public class NotificationToAdminConfiguration : IEntityTypeConfiguration<NotificationToAdmin>
{
    public void Configure(EntityTypeBuilder<NotificationToAdmin> builder)
    {
        builder.ToTable(t => t.HasComment("Notifications that have been sent to the admin via the website ui"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasMaxLength(NotificationToAdmin.MaxNameLength)
            .IsRequired();

        builder.Property(x => x.EmailAddress)
            .HasMaxLength(NotificationToAdmin.MaxEmailAddressLength)
            .IsRequired();

        builder.Property(x => x.Subject)
            .HasMaxLength(NotificationToAdmin.MaxSubjectLength)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(NotificationToAdmin.MaxMessageLength)
            .IsRequired();

        builder.Property(x => x.IsItSentSuccessfully)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.CreatedAt, x.EmailAddress });
    }
}
