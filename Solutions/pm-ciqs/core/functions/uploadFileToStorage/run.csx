#load "..\CiqsHelpers\All.csx"
#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using System.Net.Http.Formatting;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;

const int longPollingDelaySeconds = 10;
const int maxRetryAttempts = 6;
const int MAX_RETRY_COUNT = 36;
const string STORAGE_ENDPOINT = "core.windows.net";

// This azure function uploads a file to the provisioned storageAccount
public static async Task Run (HttpRequestMessage req, TraceWriter log)
{

    log.Info ("uploadFileToStorage: Entry.");
    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string accountName = parametersReader.GetParameter<string> ("accountName");
    string key = parametersReader.GetParameter<string> ("key");
    string containerName = parametersReader.GetParameter<string> ("containerName");
    string fileName = parametersReader.GetParameter<string> ("fileName");
    string fileUrl = parametersReader.GetParameter<string> ("fileUrl");

    // create timeout, entire upload process should not exceed 20 minutes
    var timeout = TimeSpan.FromMinutes (20);

    // download file content to upload to storageAccount
    string data = await DownloadFile (fileUrl);

    // call method to actually upload file to storageAccount, passing in data
    await UploadFileFromUriAsync (accountName, key, containerName, fileName, data, timeout);

    // UploadFileFromUri didn't throw exception, successful completion
    log.Info ("uploadFileToStorage: Succeeded.");
}

// This method takes in a fileUrl and downloads the content at that Url to a local string, returns
public static async Task<string> DownloadFile (string fileUrl) {

    // init vars, and retryCounter
    string data = null;
    int retryCount = 0;

    // set delay between each retry
    TimeSpan delay = TimeSpan.FromSeconds (longPollingDelaySeconds);

    // using statement to dispose WebClient when done using it
    using (var client = new WebClient ()) {

        // while fileUrl contents have not been downloaded to 'data'
        // and retry has not exceeded MAX, retry
        while (String.IsNullOrWhiteSpace (data) && retryCount < MAX_RETRY_COUNT) {

            // increment retryCount
            retryCount++;

            // try to download fileContent as string
            data = client.DownloadString (fileUrl);

            // delay
            await Task.Delay (delay);
        }
    }

    // if data is still empty, ran out of retries so throw exception
    if (String.IsNullOrWhiteSpace(data)) {

        throw new Exception("File content could not be downloaded from specified fileUrl");
    }

    // fileContent successfully downloaded to 'data', return
    return data;
}

// This method uploads a file (represented by string 'fileData') to specified storageAccount
public static async Task UploadFileFromUriAsync (string accountName, string key, string containerName, string fileName, string fileData, TimeSpan timeout) {

    // **code largely taken from existing SA codebase

    // convert fileData to a 'Stream', format expected by upload method
    byte[] byteArray = Encoding.UTF8.GetBytes (fileData);
    MemoryStream stream = new MemoryStream (byteArray);

    // set timeout for upload
    DateTime timeoutTime = DateTime.Now.Add (timeout);

    // set delay for retries
    TimeSpan delay = TimeSpan.FromSeconds (longPollingDelaySeconds);

    // 'get' storageAccount to upload data to
    StorageCredentials creds = new StorageCredentials (accountName, key);
    CloudStorageAccount storageAccount = new CloudStorageAccount (creds, STORAGE_ENDPOINT, false);
    var blobClient = storageAccount.CreateCloudBlobClient ();
    blobClient.DefaultRequestOptions = new BlobRequestOptions () { RetryPolicy = new LinearRetry (TimeSpan.FromSeconds (longPollingDelaySeconds), maxRetryAttempts) };
    var container = blobClient.GetContainerReference (containerName);

    // keep trying to create container until successful or timeout
    while (true) {
        try {

            // send request to create container
            await container.CreateIfNotExistsAsync ();

            // creation successful (no exception thrown), break out of while
            break;
        } catch (StorageException ex) {

            // exception thrown on CreateIfNotExistsAsync(), if out of time rethrow exception
            if (DateTime.Now > timeoutTime) {
                throw;
            }

            // still have time to retry, delay then continue
            await Task.Delay (delay);
        }
    }

    // get blob to upload data to
    var blob = container.GetBlockBlobReference (fileName);

    // while blob does not exist, try to upload data
    while (!await blob.ExistsAsync ()) {

        // check and throw exception if out of time
        if (DateTime.Now > timeoutTime) {
            throw new TimeoutException ("Timed out waiting to upload data file");
        }

        // make request to upload data to blob
        await blob.UploadFromStreamAsync (stream);

        // delay before continuing/retrying
        await Task.Delay (delay);
    }

    // blob exists, upload successful
}