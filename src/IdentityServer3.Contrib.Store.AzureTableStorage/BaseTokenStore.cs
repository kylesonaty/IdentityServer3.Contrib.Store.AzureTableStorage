using System;
using IdentityServer3.Contrib.Store.AzureTableStorage.Serialization;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;

namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    /// <summary>
    /// A abstract base class for redis stores.
    /// </summary>
    /// <typeparam name="T">The type of token to store.</typeparam>
    public abstract class BaseTokenStore<T> where T : class
    {
        /// <summary>
        /// The client store to load client information from
        /// </summary>
        protected readonly IClientStore ClientStore;
        /// <summary>
        /// The scope store to load scope information from
        /// </summary>
        protected readonly IScopeStore ScopeStore;

        /// <summary>
        /// Constructor for setting up base store in derived classes.
        /// </summary>
        /// <param name="clientStore">The client store to load client information from</param>
        /// <param name="scopeStore">The scope store to load scope information from</param>
        protected BaseTokenStore(IClientStore clientStore, IScopeStore scopeStore)
        {
            if (clientStore == null) throw new ArgumentNullException(nameof(clientStore));
            if (scopeStore == null) throw new ArgumentNullException(nameof(scopeStore));

            ClientStore = clientStore;
            ScopeStore = scopeStore;
        }

        /// <summary>
        /// Serializes an object using the specialized converters
        /// </summary>
        /// <param name="value">Value to serialize</param>
        /// <returns>Serialized JSON for the object</returns>
        protected string ToJson(T value)
        {
            return JsonConvert.SerializeObject(value, GetJsonSerializerSettings());
        }

        /// <summary>
        /// Deserializes an object using the specialized converters
        /// </summary>
        /// <param name="json">The json to deserialize</param>
        /// <returns>The deserialized object</returns>
        protected T FromJson(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, GetJsonSerializerSettings());
        }

        private JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ClaimConverter());
            settings.Converters.Add(new ClaimsPrincipalConverter());
            settings.Converters.Add(new ClientConverter(ClientStore));
            settings.Converters.Add(new ScopeConverter(ScopeStore));
            return settings;
        }
    }
}