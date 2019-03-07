using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Retry;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    public class AzureTableStorageTokenHandleStore : BaseTokenStore<Token>, ITokenHandleStore
    {
        private readonly Lazy<CloudTable> _table;
        private readonly RetryHelper _retryHelper;

        /// <summary>
        /// Creates a new instance of the Azure Table Storage token handle store
        /// </summary>
        /// <param name="clientStore">Needed because we don't serialize the whole TokenHandle. It is looked up by id from the store.</param>
        /// <param name="scopeStore">Needed because we don't serialize the whole TokenHandle. It is looked up by id from the store.</param>
        /// <param name="connectionString">The connection string for connecting to Azure Table Storage.</param>
        /// <param name="tableName">Optional table name. Defaults to TokenHandle</param>
        public AzureTableStorageTokenHandleStore(IClientStore clientStore, IScopeStore scopeStore, string connectionString, string tableName = "TokenHandle")
            : base(clientStore, scopeStore)
        {
            _table = new Lazy<CloudTable>(() =>
            {
                var account = CloudStorageAccount.Parse(connectionString);
                var client = account.CreateCloudTableClient();
                var table = client.GetTableReference(tableName);

                table.CreateIfNotExists();
                return table;
            });

            _retryHelper = new RetryHelper(new TraceSource("AzureTableStorageAuthorizationCodeStore"))
            {
                DefaultMaxTryCount = 3,
                DefaultMaxTryTime = TimeSpan.FromSeconds(30),
                DefaultTryInterval = TimeSpan.FromMilliseconds(200),
            };
        }

        /// <summary>
        /// Saves the token with its given key
        /// </summary>
        /// <param name="key">The key for the token</param>
        /// <param name="value">The refresh token to serialize and store</param>
        public async Task StoreAsync(string key, Token value)
        {
            var entity = new TokenTableEntity
            {
                PartitionKey = key.GetParitionKey(),
                RowKey = key,
                ClientId = value.ClientId,
                Json = ToJson(value),
                SubjectId = value.SubjectId
            };
            var op = TableOperation.InsertOrReplace(entity);
            await _retryHelper.Try(() => _table.Value.ExecuteAsync(op)).UntilNoException();
        }

        /// <summary>
        /// Retrieves the token using its key 
        /// </summary>
        /// <param name="key">The key for the token</param>
        /// <returns>A Tasks with the token</returns>
        public async Task<Token> GetAsync(string key)
        {
            var op = TableOperation.Retrieve<TokenTableEntity>(key.GetParitionKey(), key);
            var result = await _retryHelper.Try(() => _table.Value.ExecuteAsync(op)).UntilNoException();
            var tokenEntity = result.Result as TokenTableEntity;
            return tokenEntity != null ? FromJson(tokenEntity.Json) : null;
        }

        /// <summary>
        /// Removes the token from the store with a given key
        /// </summary>
        /// <param name="key">The key of the token</param>
        public async Task RemoveAsync(string key)
        {
            var entity = new TokenTableEntity
            {
                PartitionKey = key.GetParitionKey(),
                RowKey = key,
                ETag = "*"
            };
            var op = TableOperation.Delete(entity);
            await _retryHelper.Try(() => _table.Value.ExecuteAsync(op)).UntilNoException();
        }

        /// <summary>
        /// Retrieves all the tokens for a given subject
        /// </summary>
        /// <param name="subject">The subject to filter by.</param>
        /// <returns></returns>
        public async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subject)
        {
            var query = new TableQuery<TokenTableEntity>().Where(TableQuery.GenerateFilterCondition("SubjectId", QueryComparisons.Equal, subject));
            var list = new List<TokenTableEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await _retryHelper.Try(() => _table.Value.ExecuteQuerySegmentedAsync(query, continuationToken)).UntilNoException();
                continuationToken = result.ContinuationToken;
                list.AddRange(result.Results);
            } while (continuationToken != null);
            return list.Select(tte => FromJson(tte.Json));
        }

        /// <summary>
        /// Removes the token for a given subject and client.
        /// </summary>
        /// <param name="subject">The subject to filter by.</param>
        /// <param name="client">The client to filter by.</param>
        /// <returns></returns>
        public async Task RevokeAsync(string subject, string client)
        {
            var query = new TableQuery<TokenTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("SubjectId", QueryComparisons.Equal, subject),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("ClientId", QueryComparisons.Equal, client)));
            var list = new List<TokenTableEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await _retryHelper.Try(() => _table.Value.ExecuteQuerySegmentedAsync(query, continuationToken)).UntilNoException();
                continuationToken = result.ContinuationToken;
                list.AddRange(result.Results);
            } while (continuationToken != null);
            var entityDeletionTasks = list.Select(entity =>
            {
                var op = TableOperation.Delete(entity);
                return _retryHelper.Try(() => _table.Value.ExecuteAsync(op)).UntilNoException();
            });

            await Task.WhenAll(entityDeletionTasks);
        }
    }
}