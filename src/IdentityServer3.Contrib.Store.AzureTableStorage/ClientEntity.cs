using Microsoft.WindowsAzure.Storage.Table;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    public class ClientEntity : TableEntity
    {
        public string Json { get; set; }
    }
}