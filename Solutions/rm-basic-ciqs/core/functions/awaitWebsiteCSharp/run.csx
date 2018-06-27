#load "..\CiqsHelpers\All.csx"

using System.Net;

const int MAX_REQUEST_RETRIES = 36;

private static HttpClient client = new HttpClient ();

// This Azure Function waits for the specified Uri to resolve, or timeout waiting and throw error
public static async Task Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("awaitWebsiteCSharp: Entry.");

    // set response timeout
    client.Timeout = TimeSpan.FromSeconds (60);

    // retrieve input params
    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);
    string azureWebsite = parametersReader.GetParameter<string> ("azureWebsite");    

    int retryCount = 0;

    // try to resolve azureWebsite every 10 seconds x MAX_REQUEST_RETRIES
    while (retryCount < MAX_REQUEST_RETRIES) {

        // create new, identical request every 'retry'
        var request = new HttpRequestMessage (HttpMethod.Get, azureWebsite);

        try {

            // get response message from request
            HttpResponseMessage responseMessage = default (HttpResponseMessage);
            responseMessage = await client.SendAsync (request);

            // check to see if website is 'alive'
            if (responseMessage.IsSuccessStatusCode) {
                break;
            }

        } catch (Exception e) {
            // catch any number of exceptions, site could be totally unreachable
            // or request timeout. Not necessarily a problem, keep retrying.
        }
    
        // increment retryCount and delay for 10 seconds
        retryCount++;
        await Task.Delay(10000);
    }

    // if loop exited because retryCount >= MAX_REQUEST_RETRIES,
    // website could not be resolved. throw exception
    if (retryCount >= MAX_REQUEST_RETRIES) {
        throw new Exception("Unable to resolve website");
    }

    log.Info ("awaitWebsiteCSharp: Success.");
}