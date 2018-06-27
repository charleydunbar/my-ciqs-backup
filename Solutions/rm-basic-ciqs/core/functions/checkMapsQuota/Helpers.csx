using Newtonsoft.Json;
using System.Globalization;

public class ResourceProvider {
    [JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    [JsonProperty ("namespace", NullValueHandling = NullValueHandling.Ignore)]
    public string Namespace { get; set; }

    [JsonProperty ("authorization", NullValueHandling = NullValueHandling.Ignore)]
    public Authorization NamespaceAuthorization { get; set; }

    [JsonProperty ("registrationState", NullValueHandling = NullValueHandling.Ignore)]
    public string RegistrationState { get; set; }

    [JsonProperty ("resourceTypes", NullValueHandling = NullValueHandling.Ignore)]
    public List<ProviderResourceType> ResourceTypes { get; set; }

    public class Authorization {
        [JsonProperty ("applicationId", NullValueHandling = NullValueHandling.Ignore)]
        public string ApplicationId { get; set; }

        [JsonProperty ("roleDefinitionId", NullValueHandling = NullValueHandling.Ignore)]
        public string RoleDefinitionId { get; set; }
    }

    public class ProviderResourceType {
        [JsonProperty ("resourceType", NullValueHandling = NullValueHandling.Ignore)]
        public string ResourceType { get; set; }

        [JsonProperty ("locations", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Locations { get; set; }

        [JsonProperty ("apiVersions", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ApiVersions { get; set; }
    }
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