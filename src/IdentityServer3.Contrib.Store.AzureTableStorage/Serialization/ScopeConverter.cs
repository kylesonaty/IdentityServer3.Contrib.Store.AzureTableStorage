using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using Newtonsoft.Json;

namespace IdentityServer3.Contrib.Store.AzureTableStorage.Serialization
{
    public class ScopeConverter : JsonConverter
    {
        private readonly IScopeStore _scopeStore;

        public ScopeConverter(IScopeStore scopeStore)
        {
            if (scopeStore == null) throw new ArgumentNullException(nameof(scopeStore));
            _scopeStore = scopeStore;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var source = (Scope)value;
            var target = new ScopeLite
            {
                Name = source.Name
            };
            serializer.Serialize(writer, target);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var source = serializer.Deserialize<ScopeLite>(reader);
            var factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
            return factory.StartNew(async () => await _scopeStore.FindScopesAsync(new[] {source.Name}))
                    .Unwrap()
                    .GetAwaiter()
                    .GetResult()
                    .Single();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof (Scope) == objectType;
        }
    }
}