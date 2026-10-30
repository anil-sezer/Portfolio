using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Configurations;

public class RequestLogConfiguration : IEntityTypeConfiguration<RequestLog>
{
    public void Configure(EntityTypeBuilder<RequestLog> builder)
    {
        builder.ToTable(t => t.HasComment("Accesses to the website"));

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UserAgent).HasMaxLength(RequestLog.MaxUserAgentLength).IsRequired();
        builder.Property(x => x.AcceptLanguage).HasMaxLength(RequestLog.MaxAcceptLanguageLength).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(RequestLog.MaxPlatformLength).IsRequired();
        builder.Property(x => x.Webdriver).IsRequired();
        builder.Property(x => x.DeviceMemory).HasMaxLength(RequestLog.MaxDeviceMemoryLength).IsRequired();
        builder.Property(x => x.HardwareConcurrency).HasMaxLength(RequestLog.MaxHardwareConcurrencyLength).IsRequired();
        builder.Property(x => x.MaxTouchPoints).IsRequired();
        builder.Property(x => x.DoNotTrack).HasMaxLength(RequestLog.MaxDoNotTrackLength).IsRequired();
        builder.Property(x => x.Connection).HasMaxLength(RequestLog.MaxConnectionLength).IsRequired();
        builder.Property(x => x.CookieEnabled).IsRequired();
        builder.Property(x => x.OnLine).IsRequired();
        builder.Property(x => x.Referrer).HasMaxLength(RequestLog.MaxReferrerLength).IsRequired();
        builder.Property(x => x.Resolution).HasMaxLength(RequestLog.MaxResolutionLength).IsRequired();
        builder.Property(x => x.ClientIp).HasMaxLength(RequestLog.MaxClientIpLength).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(RequestLog.MaxCountryLength).IsRequired();
        builder.Property(x => x.City).HasMaxLength(RequestLog.MaxCityLength).IsRequired();
        builder.Property(x => x.RequestedUrl).HasMaxLength(RequestLog.MaxRequestedUrlLength).IsRequired();
        builder.Property(x => x.Extras).HasMaxLength(RequestLog.MaxExtrasLength).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.ClientIp, x.City, x.Country });
    }
}
