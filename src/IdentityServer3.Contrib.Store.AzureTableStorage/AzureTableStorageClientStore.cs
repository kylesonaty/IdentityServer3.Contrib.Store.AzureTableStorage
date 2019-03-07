using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using IdentityServer3.Contrib.Store.AzureTableStorage.Serialization;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Retry;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    /// <summary>
    /// An Azure Table Storage backed client store for Identity Server 3
    /// </summary>
    public class AzureTableStorageClientStore: IClientStore
    {
        private readonly Lazy<CloudTable> _table;
        private readonly RetryHelper _retryHelper;

        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> {new ClaimConverter()}
        };

        /// <summary>
        /// Creates a new instance of the Azure Table Storage client store
        /// </summary>
        /// <param name="connectionString">Table storage connection string</param>
        /// <param name="tableName">Optional table name.</param>
        public AzureTableStorageClientStore(string connectionString, string tableName = "Clients")
        {
            _table = new Lazy<CloudTable>(() =>
            {
                var account = CloudStorageAccount.Parse(connectionString);
                var client = account.CreateCloudTableClient();
                var table = client.GetTableReference(tableName);

                table.CreateIfNotExists();
                return table;
            });

            _retryHelper = new RetryHelper(new TraceSource("AzureTableStorageClientStore"))
            {
                DefaultMaxTryCount = 3,
                DefaultMaxTryTime = TimeSpan.FromSeconds(30),
                DefaultTryInterval = TimeSpan.FromMilliseconds(200),
            };
        }

        /// <summary>
        /// Retrieve a client by client id
        /// </summary>
        /// <param name="clientId">The id of the client to retrieve</param>
        /// <returns></returns>
        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var op = TableOperation.Retrieve<ClientEntity>(clientId.GetParitionKey(), clientId);
            var result = await _retryHelper.Try(() => _table.Value.ExecuteAsync(op)).UntilNoException();
            var client = result.Result as ClientEntity;
            return client != null ? JsonConvert.DeserializeObject<Client>(client.Json, _settings) : null;
        }
    }
}
