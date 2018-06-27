using System.Globalization;

public class MachineLearningExperiment {
    public string ActivityId { get; set; }
    public string Status { get; set; }
    public string ExperimentId { get; set; }
}

public class MachineLearningWebServiceEndpoint {
    public DateTime CreationTime { get; set; }
    public string WorkspaceId { get; set; }
    public string WebServiceId { get; set; }
    public string HelpLocation { get; set; }
    public string PrimaryKey { get; set; }
    public string SecondaryKey { get; set; }
    public string ApiLocation { get; set; }
    public string ExperimentLocation { get; set; }
    public string Version { get; set; }
    public int MaxConcurrentCalls { get; set; }
    public string DiagnosticsTraceLevel { get; set; }
    public string ThrottleLevel { get; set; }
}

public class MachineLearningWebServiceStatus {
    public string ActivityId { get; set; }
    public string Status { get; set; }
    public string WebServiceGroupId { get; set; }
    public string EndpointId { get; set; }
}
public class MachineLearningWebService {
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreationTime { get; set; }
    public string WorkspaceId { get; set; }
    public string DefaultEndpointName { get; set; }
    public int EndpointCount { get; set; }
}

public class EnvironmentSettings {
    public string MachineLearningApiEndpoint { get; private set; }
    public string MachineLearningManagementEndpoint { get; private set; }

    public static EnvironmentSettings GetSettingsFromLocation (string location) {

        var settings = new EnvironmentSettings () {
            MachineLearningApiEndpoint = "studioapi.azureml.net",
            MachineLearningManagementEndpoint = "management.azureml.net"
        };

        if (!string.IsNullOrEmpty (location)) {

            location = location.Replace (" ", string.Empty).ToLowerInvariant ();

            switch (location) {

                // AzureGermanyCloud
                case "germanycentral":
                case "germanynortheast":
                    settings.MachineLearningApiEndpoint = "germanycentral.studioapi.azureml.de";
                    settings.MachineLearningManagementEndpoint = "germanycentral.management.azureml.de";
                    break;

                    // AzureChinaCloud
                case "chinanorth":
                case "chinaeast":
                    settings.MachineLearningApiEndpoint = "chinaeast.studioapi.azureml.cn";
                    settings.MachineLearningManagementEndpoint = "chinaeast.management.azureml.cn";
                    break;

            }
        }
        return settings;
    }
}

public static string FormatInvariant (this string format, params object[] args) {
    return string.Format (CultureInfo.InvariantCulture, format, args ?? new object[0]);
}