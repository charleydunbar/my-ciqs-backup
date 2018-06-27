
#load "..\CiqsHelpers\All.csx"

using System.Net;

private const string CSHARP_RUNTIME_IN = "C#"; 
private const string JAVA_RUNTIME_IN = "Java";

private const string CSHARP_RUNTIME_OUT = "dotnet"; 
private const string JAVA_RUNTIME_OUT = "java"; 

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)

{

    log.Info ("convertMSRuntime: Entry.");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    string msRuntime = parametersReader.GetParameter<string>("msRuntime");

    string retRuntime = CSHARP_RUNTIME_OUT;

    if (msRuntime.Equals(JAVA_RUNTIME_IN)) {
        retRuntime = JAVA_RUNTIME_OUT;
    }

    log.Info ("convertMSRuntime: Success.");

    return new {

        microServiceRuntime = retRuntime

    };
}
