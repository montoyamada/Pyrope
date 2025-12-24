namespace Pyrope.GarnetServer.Utils
{
    public static class KeyUtils
    {
        public static string GetIndexConfigKey(string tenantId, string indexName)
        {
            return $"_meta:tenant:{tenantId}:index:{indexName}:config";
        }

        public static string GetTenantConfigKey(string tenantId)
        {
            return $"_meta:tenant:{tenantId}:config";
        }
    }
}
