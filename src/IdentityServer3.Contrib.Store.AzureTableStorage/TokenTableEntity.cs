using Microsoft.WindowsAzure.Storage.Table;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    /// <summary>
    /// Internal class for storing tokens
    /// </summary>
    internal class TokenTableEntity : TableEntity
    {
        public string Json { get; set; }
        public string SubjectId { get; set; }
        public string ClientId { get; set; }
    }
}
