#load "..\CiqsHelpers\All.csx"

using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

// This Azure Function gets the default domain for the given tenantId
public static async Task<object> Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("getDomain: Entry.");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string tenantId = parametersReader.GetParameter<string> ("tenantId");
    string graphAccessToken = parametersReader.GetParameter<string> ("graphAccessToken");

    string defaultDomain = "";

    if (tenantId.Equals ("72f988bf-86f1-41af-91ab-2d7cd011db47")) {

        // This tenantId is has many verified domains. This step is simply to shortcut that search.
        defaultDomain = "microsoft.onmicrosoft.com";

    } else {

        // otherwise, make request for tenantDetails and search for default domain
        string getDomainsQuery = string.Format ("https://graph.windows.net/{0}/tenantDetails?api-version=1.6", tenantId);
        HttpClient client = new HttpClient ();

        // GET request for domains associated with tenantId
        HttpRequestMessage domainsRequest = new HttpRequestMessage (HttpMethod.Get, getDomainsQuery);
        domainsRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue ("bearer", graphAccessToken);

        // Send GET request and await response
        HttpResponseMessage domainsResponse = await client.SendAsync (domainsRequest);

        // Read response as a string
        string domainsResponseString = domainsResponse.Content.ReadAsStringAsync ().Result;

        // convert response to JObject
        JObject domainsObject = JObject.Parse (domainsResponseString);

        // pull associated verifiedDomains array from JObject
        JArray domains = domainsObject["value"][0]["verifiedDomains"] as JArray;

        // iterating through verifiedDomains looking for default
        foreach (JObject domain in domains) {

            if ((bool) domain["default"]) {

                // default domain found
                defaultDomain = domain["name"].ToString ();
            }
        }
    }

    if (String.IsNullOrWhiteSpace(defaultDomain)) {

        // empty defaultDomain means none could be found, for any number of reasons
        log.Info ("getDomain: Failed.");
        throw new Exception ("Default domain not found for tenantId");
    }

    log.Info ("getDomain: Success.");

    return new {
        oDomain = defaultDomain
    };
}