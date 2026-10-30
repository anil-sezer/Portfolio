using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Configurations;

public class DailyImageConfiguration : IEntityTypeConfiguration<DailyImage>
{
    public void Configure(EntityTypeBuilder<DailyImage> builder)
    {
        builder.ToTable(t => t.HasComment("List of daily background images by Bing, NASA, etc. They are great"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(DailyImage.MaxImageUrlLength)
            .IsRequired();

        builder.Property(x => x.AltText)
            .HasMaxLength(DailyImage.MaxAltTextLength)
            .IsRequired();

        builder.Property(x => x.Source)
            .IsRequired();

        builder.Property(x => x.UrlWorks)
            .IsRequired();

        builder.Property(x => x.DoIPreferToDisplayThis)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.UrlWorks, x.DoIPreferToDisplayThis });
    }
}
