using Newtonsoft.Json;
using System.Globalization;

public class ApplicationRole
    {
        [JsonProperty("allowedMemberTypes@odata.type")]
        public string AllowedMemberTypesODataType = "Collection(Edm.String)";
        [JsonProperty("allowedMemberTypes")]
        public List<string> AllowedMemberTypes { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("isEnabled")]
        public bool IsEnabled { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }

public class ServicePrincipal
    {
        [JsonProperty("appId")]
        public string AppId { get; set; }
        [JsonProperty("accountEnabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool AccountEnabled { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AppDisplayName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string AppOwnerTenantId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<ApplicationRole> AppRoles { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectType { get; set; }
    }

public sealed class ODataResponse<T> {
    [JsonProperty ("odata.metadata")]
    public string Metadata { get; set; }

    public IEnumerable<T> Value { get; set; }

    public string NextLink { get; set; }
}

public static string FormatInvariant (this string format, params object[] args) {
    return string.Format (CultureInfo.InvariantCulture, format, args ?? new object[0]);
}