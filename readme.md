# Azure Table Storage Store for Identity Server 3

This project implement different stores for Identity Server 3 using Azure Table Storage as the data store.

Currently implemented
* Refresh Token Store
* Authorization Code Store
* Client Store

Planned
* Scope Store
* Consent Store

## How to use

To use download the [nuget package](https://www.nuget.org/packages/IdentityServer3.Contrib.Store.AzureTableStorage/).

Once you have the nuget package installed setup the factory.

```csharp

var connectionString = ConfigurationManager.ConnectionStrings["AzureTableStorage"].ConnectionString;
var clientStore = new AzureTableStorageClientStore(connectionString);

var refreshTokenStore = new AzureTableStorageRefreshTokenStore(clientStore, scopeStore, connectionString);
var authorizationTokenStore = new AzureTableStorageAuthorizationCodeStore(clientStore, scopeStore, connectionString);

var factory = new IdentityServerServiceFactory
{
    ClientStore = new Registration<IClientStore>(clientStore),
    RefreshTokenStore = new Registration<IRefreshTokenStore>(_ => refreshTokenStore),
    AuthorizationCodeStore = new Registration<IAuthorizationCodeStore>(_ => authorizationTokenStore)
    // other setup omitted
};
```
