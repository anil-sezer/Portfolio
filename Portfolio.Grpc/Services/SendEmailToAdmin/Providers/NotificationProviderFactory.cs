using Portfolio.Domain.Interfaces.ThirdPartyServices;

namespace Portfolio.Grpc.Services.SendEmailToAdmin.Providers;

public class NotificationProviderFactory : INotificationProviderFactory
{
    private readonly Dictionary<string, INotificationProvider> _providers;

    public NotificationProviderFactory(IEnumerable<INotificationProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.GetType().Name, p => p);
    }

    public INotificationProvider GetProvider(string providerName)
    {
        return _providers.TryGetValue(providerName, out var provider)
            ? provider
            : throw new InvalidOperationException($"No email provider found for {providerName}");
    }
}
