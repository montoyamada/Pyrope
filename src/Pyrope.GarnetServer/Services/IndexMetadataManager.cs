using System;
using System.Text.Json;
using Pyrope.GarnetServer.Model;
using Pyrope.GarnetServer.Utils;

namespace Pyrope.GarnetServer.Services
{
    public class IndexMetadataManager
    {
        private const string Prefix = "sys:index";

        public string GetMetadataKey(string tenantId, string indexName)
        {
            TenantNamespace.ValidateTenantId(tenantId);
            TenantNamespace.ValidateIndexName(indexName);

            return $"{Prefix}:{tenantId}:{indexName}";
        }

        public byte[] SerializeConfig(IndexConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return JsonSerializer.SerializeToUtf8Bytes(config);
        }

        public IndexConfig? DeserializeConfig(byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            return JsonSerializer.Deserialize<IndexConfig>(data);
        }

        public IndexConfig? DeserializeConfig(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<IndexConfig>(json);
        }
    }
}
