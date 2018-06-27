
#load "..\CiqsHelpers\All.csx"

using System.Text;
using System.Net;
using System.Net.Http;

using Microsoft.Azure;
using Microsoft.Azure.Management.StreamAnalytics; 
using Microsoft.Azure.Management.StreamAnalytics.Models;

private const int MAX_START_JOB_RETRY_COUNT = 6; // max number of retry on failure
private const int RETRY_DELAY = 10000; // 10 seconds
private const int START_JOB_TIMEOUT_LIMIT = 240; // 4 minutes

// This Azure Function starts the streaming job provisioned in the preceding
// ARM deployment.
public static async Task Run(HttpRequestMessage req, TraceWriter log)
{

    log.Info ("startStreamingJob: Entry.");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    string subscriptionId = parametersReader.GetParameter<string>("subscriptionId");
    string authorizationToken = parametersReader.GetParameter<string>("accessToken");
    string resourceGroupName = parametersReader.GetParameter<string>("resourceGroupName");
    string saJobName = parametersReader.GetParameter<string>("streamingJobsName");

    int retryCounter = 0;
    
    // startStreamingJob failures are common, okay, and retriable
    // keep retrying for
    while (retryCounter < MAX_START_JOB_RETRY_COUNT)
    {
        try
        {
            // StartStreamingJob can throw an error, retry when this happens
            await StartStreamingJob(subscriptionId, authorizationToken, resourceGroupName, saJobName, log);

            // if no error is thrown, break out of while loop
            break;

        }
        catch (Exception e)
        {
            // StartStreamingJob failed, increment retryCounter
            retryCounter++;

            // if there are still 'retries' left, delay then continue
            if (retryCounter < MAX_START_JOB_RETRY_COUNT)
            {
                await Task.Delay(RETRY_DELAY);
                log.Info("START JOB FAILED -- RETRYING");

            }
            else
            {
                // passed max number of retries, throw error
                throw e;

            }
        }
    }
}

// Code for StartStreamingJob found in IoT VSTS codebase
private static async Task StartStreamingJob(string subscriptionId, string authorizationToken, string resourceGroupName, string jobName, TraceWriter log)
{

    TokenCloudCredentials credentials = new TokenCloudCredentials(subscriptionId, authorizationToken);

    using (StreamAnalyticsManagementClient streamClient = new StreamAnalyticsManagementClient(credentials))
    {

        JobStartParameters jobStartParameters = new JobStartParameters
        {

            OutputStartMode = OutputStartMode.JobStartTime

        };


        int timeoutCounter = 0;

        LongRunningOperationResponse response = streamClient.StreamingJobs.Start(resourceGroupName, jobName, jobStartParameters);        

        while (response.Status != OperationStatus.Succeeded)
        {

            timeoutCounter++;

            log.Info(response.Status.ToString());

            var uri = new Uri(response.OperationStatusLink);            

            response = await streamClient.GetLongRunningOperationStatusAsync(response.OperationStatusLink);

            if (response.Status == OperationStatus.Failed)
            {

                throw new Exception($"Start SA job: {jobName} failed");
            }

            if (timeoutCounter > START_JOB_TIMEOUT_LIMIT)
            {
                throw new Exception("Starting job timed out, try again at another time");
            }

            await Task.Delay(1000);

        }
    }
}