
#load "..\CiqsHelpers\All.csx"

using System.Net;

// Allowed runtimes in manual prompt step
private const string CSHARP_RUNTIME_IN = "C#"; 
private const string JAVA_RUNTIME_IN = "Java";

// ARM template expected runtimes
private const string CSHARP_RUNTIME_OUT = "dotnet"; 
private const string JAVA_RUNTIME_OUT = "java"; 

// This Azure Function converts Input Runtime into the format expected
// by the ARM template
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{

    log.Info ("convertMSRuntime: Entry.");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    string msRuntime = parametersReader.GetParameter<string>("msRuntime");

    // default to 'dotnet' runtime
    string retRuntime = CSHARP_RUNTIME_OUT;

    // update to 'java' if necessary
    if (msRuntime.Equals(JAVA_RUNTIME_IN)) {
        retRuntime = JAVA_RUNTIME_OUT;
    }

    log.Info ("convertMSRuntime: Success.");

    return new {

        microServiceRuntime = retRuntime

    };
}
