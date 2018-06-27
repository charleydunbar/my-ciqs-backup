#load "..\CiqsHelpers\All.csx"
#load ".\Helpers.csx"

using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;

const int MAX_REQUEST_RETRIES = 15;
const string GraphManagementVersion = "1.6";
const string AzureURL = "https://graph.windows.net/";

const string webSiteAppId = "abfa0a7c-a6b6-4736-8310-5855508787cd";

private static HttpClient client = new HttpClient ();

//  -----------------------------------------------------------------------------------------------------------------------------------------
//      ENTRY POINT
//  -----------------------------------------------------------------------------------------------------------------------------------------

// This Azure Function retrieves the objectIds of required Service Principals
public static async Task<object> Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("checkMapsQuota: Entry.");

    // set response timeout
    client.Timeout = TimeSpan.FromSeconds (60);
    client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string tenantId = parametersReader.GetParameter<string> ("tenantId");
    string accessToken = parametersReader.GetParameter<string> ("accessToken");
    string appId = parametersReader.GetParameter<string> ("appId");

    // get service principal with appId = webSiteAppId
    var servicePrincipal = await GetServicePrincipalByName (webSiteAppId, tenantId, accessToken, AzureURL);

    // store objectId of service principal
    string wSObjectId = servicePrincipal.ObjectId;

    // get service principal with appId = appId
    servicePrincipal = await GetServicePrincipalByName (appId, tenantId, accessToken);

    // store objectId of service principal
    string rAObjectId = servicePrincipal.ObjectId;

    return new {

        oWebSitesServicePrincipalObjectId = wSObjectId,
        oRdxAccessPolicyPrincipalObjectId = rAObjectId

    };
}

// This method gets and returns a ServicePrincipal with the given appId under the given tenantId
public static async Task<ServicePrincipal> GetServicePrincipalByName (string azureEndpoint, string tenantId, string apiVersion, string appId, string key) {

    // ex: "https://graph.windows.net/{tenantId}/servicePrincipals?api-version=1.6&$filter=servicePrincipalNames/any(c:%20c%20eq%20'{appId}')"
    string reqUri = "{0}{1}/servicePrincipals?api-version={2}&$filter=servicePrincipalNames/any(c:%20c%20eq%20'{3}')".FormatInvariant (azureEndpoint, tenantId, apiVersion, appId);

    // create GET request, add headers
    var request = new HttpRequestMessage (HttpMethod.Get, reqUri);
    request.Headers.Add ("Authorization", "Bearer {0}".FormatInvariant (key));
    //request.Headers.Add ("x-ms-version", apiVersion);
    request.Headers.Add ("x-ms-version", "1.6");

    // pass request to SendRequest() and get ODataResponse<ServicePrincipal> (contains IEnumerable of ServicePrincipal)
    var result = await SendRequest<ODataResponse<ServicePrincipal>> (request, "Unable to get service principal");

    // if result is not null and there is a single element in the Value IEnumerable, return
    if (result != null && result.Value.Count () == 1) {
        return result.Value.First ();
    }

    // result is either null, empty, or contains more than one value. return 'default' ServicePrincipal
    return default (ServicePrincipal);
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