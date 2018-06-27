#load "..\CiqsHelpers\All.csx"
#load ".\Helpers.csx"

using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const int longPollingDelaySeconds = 10;
const int MAX_REQUEST_RETRIES = 15;
const string ExperimentName = "Remaining Useful Life [Predictive Exp.]";

private static HttpClient client = new HttpClient ();
private static EnvironmentSettings AzureEnvironment;

// This Azure Function copies experiments over to the provisioned ML Workspace
// it returns the MLApiUrl and MLApiKey that the 'jobhost' App Service resource
// depends on.
public static async Task<object> Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("setupMLWorkspace: Entry.");

    // create timeout for setupMLWorkspace operation, should not exceed 20 minutes
    var timeout = TimeSpan.FromMinutes (20);

    // set waiting timeout for HTTP client
    client.Timeout = TimeSpan.FromSeconds (60);
    client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string deploymentLocation = parametersReader.GetParameter<string> ("deploymentLocation");
    string mlLocation = parametersReader.GetParameter<string> ("mlLocation");
    string workspaceId = parametersReader.GetParameter<string> ("workspaceId");
    string tokenKey = parametersReader.GetParameter<string> ("tokenKey");

    // Hardcoded experiment Uri's, keep parity with existing SA codebase
    string trainingUri = "https%3a%2f%2fstorage.azureml.net%2fdirectories%2f3200a36ce557450aab40d18054ad0ac1%2fitems&communityUri=https%3a%2f%2fgallery.cortanaanalytics.com%2fDetails%2fremaining-useful-life-engines-1&entityId=Remaining-Useful-Life-Engines-1";
    string scoringUri = "https%3a%2f%2fstorage.azureml.net%2fdirectories%2f3df9f53c197f465ba50b02dfb9610ef7%2fitems&communityUri=https%3a%2f%2fgallery.cortanaanalytics.com%2fDetails%2fremaining-useful-life-predictive-exp-2&entityId=Remaining-Useful-Life-Predictive-Exp-2";

    // some values (such as endpoints) are location specific, AzureEnvironment represents the
    // environment we want to pull these values from
    AzureEnvironment = EnvironmentSettings.GetSettingsFromLocation (deploymentLocation);

    MachineLearningWebServiceEndpoint mlWebServiceEndpoint;

    // retrieve webService for the ML Workspace if it already exists
    var webService = await GetMachineLearningWebServiceByNameAsync (GetMLAPIEndpoint (mlLocation, AzureEnvironment.MachineLearningManagementEndpoint), workspaceId, tokenKey, ExperimentName);

    if (webService != null) {

        // if webService exists, get its endpoint
        mlWebServiceEndpoint = await GetMachineLearningWebServiceEndpointAsync (GetMLAPIEndpoint (mlLocation, AzureEnvironment.MachineLearningManagementEndpoint), workspaceId, tokenKey, webService.Id);

    } else {

        // webService does not yet exist, copy experiments over to ML Workspace
        await CopyMachineLearningExperimentToWorkspaceAsync (mlLocation, workspaceId, tokenKey, trainingUri, timeout);
        var scoringExperiment = await CopyMachineLearningExperimentToWorkspaceAsync (mlLocation, workspaceId, tokenKey, scoringUri, timeout);

        // create webService
        mlWebServiceEndpoint = await CreateMachineLearningWebServiceAsync (mlLocation, workspaceId, tokenKey, scoringExperiment.ExperimentId, timeout);
    }

    log.Info ("setupMLWorkspace: Succeeded.");

    return new {

        oMlApiUrl = mlWebServiceEndpoint.ApiLocation,
        oMlApiKey = mlWebServiceEndpoint.PrimaryKey,
        oMLHelpLocation = mlWebServiceEndpoint.HelpLocation

    };
}


// This method gets the MLWebService by its name, if it exists
public static async Task<MachineLearningWebService> GetMachineLearningWebServiceByNameAsync (string mlManagementEndpoint, string workspaceId, string key, string name) {

    // ex: https://management.azureml.net/workspaces/{workpaceId}/webservices
    string reqUri = "https://{0}/workspaces/{1}/webservices/".FormatInvariant (mlManagementEndpoint, workspaceId);

    // create GET request (for webServices in workspace), add authorization header
    var request = new HttpRequestMessage (HttpMethod.Get, reqUri);
    request.Headers.Add ("Authorization", "Bearer {0}".FormatInvariant (key));

    // call SendRequest with GET request, await response
    var webServices = await SendRequest<IEnumerable<MachineLearningWebService>> (request, "Failed retrieving WebServices");

    // return the webService that matches the param 'name', or return null
    return webServices == null ? null : webServices.FirstOrDefault (ws => ws.Name.Equals (name, StringComparison.InvariantCultureIgnoreCase));
}

// This method copies a specified machine learning experiment to a workspace
public static async Task<MachineLearningExperiment> CopyMachineLearningExperimentToWorkspaceAsync (string location, string workspaceId, string key, string packageUri, TimeSpan timeout) {

    // set 'timeout' for copying experiment
    DateTime expireTime = DateTime.Now.Add (timeout);
    MachineLearningExperiment experiment = null;

    // keep trying to start copy experiment until successful, or 'timeout'
    while (experiment == null) {

        if (DateTime.Now > expireTime) {

            // start copy timed out, throw exception
            throw new Exception ("Timed out waiting to start experiment to copy");
        }

        try {

            // make call to start copy experiment, await the copied experiment
            experiment = await CopyMachineLearningExperimentAsync (GetMLAPIEndpoint (location, AzureEnvironment.MachineLearningApiEndpoint), workspaceId, key, packageUri);

        } catch (Exception e) {

            // start copy did not complete successfully, delay then continue (try again)
            await Task.Delay (TimeSpan.FromSeconds (longPollingDelaySeconds));
        }
    }

    // experiment started copying succesfully, wait for it to finish or timeout
    while (String.IsNullOrWhiteSpace (experiment.ExperimentId)) {

        if (DateTime.Now > expireTime) {

            // copy timed out, throw exception
            throw new Exception ("Timed out waiting for experiment to copy");
        }

        // delay retry
        await Task.Delay (TimeSpan.FromSeconds (longPollingDelaySeconds));

        try {

            // Get results of copy experiment
            experiment = await GetMachineLearningExperimentCopyResultAsync (GetMLAPIEndpoint (location, AzureEnvironment.MachineLearningApiEndpoint), workspaceId, key, experiment.ActivityId);

        } catch (Exception e) {

            // copy did not complete successfully, try to start it again
            experiment = await CopyMachineLearningExperimentAsync (GetMLAPIEndpoint (location, AzureEnvironment.MachineLearningApiEndpoint), workspaceId, key, packageUri);
        }
    }

    // copy completed successfully
    return experiment;
}

// This method sends a PUT request to start copying machine learning experiment to workspace
public static async Task<MachineLearningExperiment> CopyMachineLearningExperimentAsync (string mlApiEndpoint, string workspaceId, string key, string packageUri) {

    // ex: https://studioapi.azureml.net/api/workspaces/{workspaceId}/packages?api-version=2.0&packageUri={experimentUri}
    string reqUri = "https://{0}/api/workspaces/{1}/packages?api-version=2.0&packageUri={2}".FormatInvariant (mlApiEndpoint, workspaceId, packageUri);

    // create PUT request (to start copying experiment), add authorization header
    var request = new HttpRequestMessage (HttpMethod.Put, reqUri);
    request.Headers.Add ("x-ms-metaanalytics-authorizationtoken", key);

    // send request and await response, return
    return await SendRequest<MachineLearningExperiment> (request, "Unable to copy machine learning experiment");
}

// This method sends a GET request to retrieve the results of the copy machine learning experiment PUT request
public static async Task<MachineLearningExperiment> GetMachineLearningExperimentCopyResultAsync (string mlApiEndpoint, string workspaceId, string key, string activityId) {

    // ex: https://studioapi.azureml.net/api/workspaces/{workspaceId}/packages?unpackActivityId={experimentCopyActivityId}
    string reqUri = "https://{0}/api/workspaces/{1}/packages?unpackActivityId={2}".FormatInvariant (mlApiEndpoint, workspaceId, activityId);

    // make GET request (for the results of the copy experiment request)
    var request = new HttpRequestMessage (HttpMethod.Get, reqUri);
    request.Headers.Add ("x-ms-metaanalytics-authorizationtoken", key);

    // send request await response and return
    return await SendRequest<MachineLearningExperiment> (request, "Unable to get 'copy machine learning experiment' result");
}

// This method sends a POST request to create a webService in the MLWorkspace
public static async Task<MachineLearningWebServiceEndpoint> CreateMachineLearningWebServiceAsync (string location, string workspaceId, string key, string scoringExperimentId, TimeSpan timeout) {

    DateTime expireTime = DateTime.Now.Add (timeout);

    // ex:  https://studioapi.azureml.net/api/workspaces/{workspaceId}/experiments/{experimentId}/webservice
    string reqUri = "https://{0}/api/workspaces/{1}/experiments/{2}/webservice".FormatInvariant (GetMLAPIEndpoint (location, AzureEnvironment.MachineLearningApiEndpoint), workspaceId, scoringExperimentId);

    // make POST request to create webService
    var request = new HttpRequestMessage (HttpMethod.Post, reqUri);
    request.Headers.Add ("x-ms-metaanalytics-authorizationtoken", key);

    // send request await result status
    var webCreateResult = await SendRequest<MachineLearningWebServiceStatus> (request, "Failed waiting to create webservice");

    // while webService creation is pending, wait or timeout
    while (webCreateResult.Status.Equals ("pending", StringComparison.InvariantCultureIgnoreCase)) {

        if (DateTime.Now > expireTime) {

            // creation timed out, throw exception
            throw new Exception ("Timed out waiting for WebService to create");
        }

        // creation not yet completed, delay then check on status again
        await Task.Delay (TimeSpan.FromSeconds (longPollingDelaySeconds));
        webCreateResult = await GetMachineLearningWebServiceCreateResultAsync (GetMLAPIEndpoint (location, AzureEnvironment.MachineLearningApiEndpoint), workspaceId, key, webCreateResult.ActivityId);
    }

    // creation done pending, if not 'completed' (could not complete) throw exception
    if (!webCreateResult.Status.Equals ("completed", StringComparison.InvariantCultureIgnoreCase)) {

        throw new Exception ("Failed to create WebService status: {0}".FormatInvariant (webCreateResult.Status));
    }

    // creation of webService was successful, call to get webService endpoint and return
    return await GetMachineLearningWebServiceEndpointAsync (GetMLAPIEndpoint (location, AzureEnvironment.MachineLearningManagementEndpoint), workspaceId, key, webCreateResult.WebServiceGroupId);
}

// This method sends a GET request to get the result (or status) of the create webService request
public static async Task<MachineLearningWebServiceStatus> GetMachineLearningWebServiceCreateResultAsync (string mlApiEndpoint, string workspaceId, string key, string activityId) {

    // ex: https://studioapi.azureml.net/api/workspaces/{workspaceId}/experiments/{activityId}/webservice
    string reqUri = "https://{0}/api/workspaces/{1}/experiments/{2}/webservice".FormatInvariant (mlApiEndpoint, workspaceId, activityId);

    // make GET request to get the status of create webService
    var request = new HttpRequestMessage (HttpMethod.Get, reqUri);
    request.Headers.Add ("x-ms-metaanalytics-authorizationtoken", key);

    // send request and await result, return
    return await SendRequest<MachineLearningWebServiceStatus> (request, "Failed waiting for WebService create result");
}

// This method sends a GET request to retrieve and return the endpoint of the created webService
public static async Task<MachineLearningWebServiceEndpoint> GetMachineLearningWebServiceEndpointAsync (string mlManagementEndpoint, string workspaceId, string key, string webserviceId) {

    // ex: https://management.azureml.net/workspaces/{workspaceId}/webservices/{webserviceId}/endpoints/default
    string reqUri = "https://{0}/workspaces/{1}/webservices/{2}/endpoints/default".FormatInvariant (mlManagementEndpoint, workspaceId, webserviceId);

    // make GET request to get webService endpoint
    var request = new HttpRequestMessage (HttpMethod.Get, reqUri);
    request.Headers.Add ("Authorization", "Bearer {0}".FormatInvariant (key));

    // send request await result and return
    return await SendRequest<MachineLearningWebServiceEndpoint> (request, "Failed retrieving WebService Endpoint");
}

public static async Task<T> SendRequest<T> (HttpRequestMessage request, string exceptionMessage) {

    HttpResponseMessage responseMessage = default (HttpResponseMessage);

    responseMessage = await client.SendAsync (request);
    int retryCount = 0;

    while (retryCount < MAX_REQUEST_RETRIES) {

        if (responseMessage.IsSuccessStatusCode) {

            var responseContent = await responseMessage.Content.ReadAsStringAsync ();
            return JsonConvert.DeserializeObject<T> (responseContent);
        }

        retryCount++;

        HttpRequestMessage clone = CloneRequestMessageAsync (request);
        responseMessage = await client.SendAsync (clone);

    }

    throw new Exception (exceptionMessage);
}

public static HttpRequestMessage CloneRequestMessageAsync (HttpRequestMessage req) {

    HttpRequestMessage clone = new HttpRequestMessage (req.Method, req.RequestUri);

    foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers) {
        clone.Headers.TryAddWithoutValidation (header.Key, header.Value);
    }

    return clone;
}

public static string GetMLAPIEndpoint (string location, string endpoint) {
    string mlregion = location;
    switch (location) {
        case "southcentralus":
            mlregion = "";
            break;
        case "westeurope":
            mlregion = "europewest";
            break;
        case "japaneast":
            mlregion = "japaneast";
            break;
        case "southeastasia":
            mlregion = "asiasoutheast";
            break;
        default:
            throw new Exception("Unknown region for ML '{0}'".FormatInvariant (location));
    }
    return string.IsNullOrEmpty (mlregion) ? endpoint : $"{mlregion}.{endpoint}";
}