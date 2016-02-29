using Microsoft.WindowsAzure.Storage.Table;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    internal class ConsentEntity : TableEntity
    {
        public string Scopes { get; set; }
    }
}