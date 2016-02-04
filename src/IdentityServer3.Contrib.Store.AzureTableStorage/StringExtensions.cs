namespace IdentityServer3.Contrib.Store.AzureTableStorage
{
    public static class StringExtensions
    {
        public static string GetParitionKey(this string s)
        {
            return s.Substring(0, 3);
        }
    }
}
