using Microsoft.WindowsAzure.Storage.Table;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    /// <summary>
    /// Internal class for storing clients
    /// </summary>
    internal class ClientEntity : TableEntity
    {
        public string Json { get; set; }
    }
}