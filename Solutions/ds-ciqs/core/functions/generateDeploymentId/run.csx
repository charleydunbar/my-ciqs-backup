#load "..\CiqsHelpers\All.csx"

using System.Net;
using System;

// This Azure Function generates a unique deploymentId (guid) for this particular
// deployment.
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{

    log.Info ("generateDeploymentId: Entry.");

    // create guid
    string retGuid = Guid.NewGuid().ToString();    

    log.Info ("generateDeploymentId: Success.");

    return new {

        oGenGuid = retGuid

    };
}
