namespace Portfolio.Domain.Interfaces.ThirdPartyServices;

public interface INotificationProviderFactory
{
    INotificationProvider GetProvider(string providerName);
}