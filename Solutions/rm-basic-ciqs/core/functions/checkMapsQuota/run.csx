#load "..\CiqsHelpers\All.csx"
#load ".\Helpers.csx"

using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;

const int MAX_REQUEST_RETRIES = 15;
const string AzureManagementVersion20150101 = "2015-01-01";
const string AzureURL = "https://management.azure.com/";
const string AzureMapsResourceTypeKey = "microsoft.maps";

private static HttpClient client = new HttpClient ();
//  -----------------------------------------------------------------------------------------------------------------------------------------
//      ENTRY POINT
//  -----------------------------------------------------------------------------------------------------------------------------------------


// This Azure Function returns whether or not RMv2 should use a static map
// **Use static if Azure Maps is not a registered resource provider for subscription
public static async Task<object> Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("checkMapsQuota: Entry.");

    // set response timeout
    client.Timeout = TimeSpan.FromSeconds (60);
    client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string subscriptionId = parametersReader.GetParameter<string> ("subscriptionId");
    string tokenKey = parametersReader.GetParameter<string> ("tokenKey");

    // get Dictionary of registered resource providers
    var providers = await GetResourceProviders (subscriptionId, AzureURL, AzureManagementVersion20150101, tokenKey);

    // if AzureMapsResourceTypeKey is not in Dictionary, use static map
    var useStaticMap = !(providers.ContainsKey (AzureMapsResourceTypeKey));

    return new {

        oStaticMap = useStaticMap.ToString ().ToLowerInvariant ()

    };

}

//  -----------------------------------------------------------------------------------------------------------------------------------------
//      END ENTRY POINT
//  -----------------------------------------------------------------------------------------------------------------------------------------

// This method gets and returns a dictionary of registered resource providers for a given subscriptionId
public static async Task<IDictionary<string, ResourceProvider>> GetResourceProviders (string subscriptionId, string azureEndpoint, string apiVersion, string key) {

    // ex: "https://management.azure.com/subscriptions/{subscriptionId}/providers?api-version=2015-01-01
    string reqUri = "{0}subscriptions/{1}/providers?api-version={2}".FormatInvariant (azureEndpoint, subscriptionId, apiVersion);

    // create GET request, add headers
    var request = new HttpRequestMessage (HttpMethod.Get, reqUri);
    request.Headers.Add ("Authorization", "Bearer {0}".FormatInvariant (key));
    request.Headers.Add ("x-ms-version", apiVersion);

    // pass request to SendRequest(), convert return value to Dictionary and return
    return (await SendRequest<ODataResponse<ResourceProvider>> (request, "Unable to get registered resource providers")).Value.ToDictionary (v => v.Namespace, StringComparer.OrdinalIgnoreCase);
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