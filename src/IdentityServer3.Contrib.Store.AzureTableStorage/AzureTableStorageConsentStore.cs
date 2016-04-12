using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    public class AzureTableStorageConsentStore : IConsentStore
    {
        private readonly Lazy<CloudTable> _table;

        /// <summary>
        /// Creates a new instance of the Azure Table Storage consent store
        /// </summary>
        /// <param name="connectionString">Table storage connection string</param>
        /// <param name="tableName">Optional table name.</param>
        public AzureTableStorageConsentStore(string connectionString, string tableName = "Consent")
        {
            _table = new Lazy<CloudTable>(() =>
            {
                var account = CloudStorageAccount.Parse(connectionString);
                var client = account.CreateCloudTableClient();
                var table = client.GetTableReference(tableName);

                table.CreateIfNotExists();
                return table;
            });
        }

        /// <summary>
        /// Retrieves all the consent for a subject
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <returns></returns>
        public async Task<IEnumerable<Consent>> LoadAllAsync(string subject)
        {
            var query = new TableQuery<ConsentEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, subject));
            var list = new List<ConsentEntity>();
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await _table.Value.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = result.ContinuationToken;
                list.AddRange(result.Results);
            } while (continuationToken != null);
            return list.Select(c => new Consent { ClientId = c.RowKey, Subject = c.PartitionKey, Scopes = c.Scopes.Split(',')});
        }

        /// <summary>
        /// Removes the consent for a subject and client
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="client">The client</param>
        /// <returns></returns>
        public async Task RevokeAsync(string subject, string client)
        {
            var entity = new ConsentEntity
            {
                PartitionKey = subject,
                RowKey = client,
                ETag = "*"
            };
            var op = TableOperation.Delete(entity);
            await _table.Value.ExecuteAsync(op);
        }

        /// <summary>
        /// Loads the subject's prior consent for the client
        /// </summary>
        /// <param name="subject">The subject</param>
        /// <param name="client">The client</param>
        /// <returns></returns>
        public async Task<Consent> LoadAsync(string subject, string client)
        {
            var op = TableOperation.Retrieve<ConsentEntity>(subject, client);
            var result = await _table.Value.ExecuteAsync(op);
            var entity = result.Result as ConsentEntity;
            if (entity != null)
            {
                return new Consent
                {
                    ClientId = entity.RowKey,
                    Subject = entity.PartitionKey,
                    Scopes = entity.Scopes.Split(',')
                };
            }
            return null;
        }

        /// <summary>
        /// Updates the consent for a subject and client.
        /// </summary>
        /// <param name="consent">The consent</param>
        /// <returns></returns>
        public async Task UpdateAsync(Consent consent)
        {
            var entity = new ConsentEntity
            {
                PartitionKey = consent.Subject,
                RowKey = consent.ClientId,
                Scopes = string.Join(",",consent.Scopes)
            };
            var op = TableOperation.InsertOrReplace(entity);
            await _table.Value.ExecuteAsync(op);
        }
    }
}
