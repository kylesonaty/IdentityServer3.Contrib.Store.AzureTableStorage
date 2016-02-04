namespace IdentityServer3.Contrib.Store.AzureTableStorage.Serialization
{
    internal class ClaimsPrincipalLite
    {
        public string AuthenticationType { get; set; }
        public ClaimLite[] Claims { get; set; }
    }
}
