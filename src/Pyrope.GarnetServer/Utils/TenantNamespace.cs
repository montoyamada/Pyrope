using System;
using System.Text.RegularExpressions;

namespace Pyrope.GarnetServer.Utils
{
    public static class TenantNamespace
    {
        private static readonly Regex SegmentPattern = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryValidateTenantId(string tenantId, out string? errorMessage)
        {
            return TryValidateSegment("TenantId", tenantId, out errorMessage);
        }

        public static bool TryValidateIndexName(string indexName, out string? errorMessage)
        {
            return TryValidateSegment("IndexName", indexName, out errorMessage);
        }

        public static void ValidateTenantId(string tenantId)
        {
            if (!TryValidateTenantId(tenantId, out var errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(tenantId));
            }
        }

        public static void ValidateIndexName(string indexName)
        {
            if (!TryValidateIndexName(indexName, out var errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(indexName));
            }
        }

        private static bool TryValidateSegment(string label, string value, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = $"{label} is required.";
                return false;
            }

            if (!SegmentPattern.IsMatch(value))
            {
                errorMessage = $"{label} must match [A-Za-z0-9_-]+ (letters, digits, underscore, hyphen).";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
