
#load "..\CiqsHelpers\All.csx"

using System.Net;

private const string CSHARP_RUNTIME_IN = "C#"; 
private const string JAVA_RUNTIME_IN = "Java";

private const string CSHARP_RUNTIME_OUT = "dotnet"; 
private const string JAVA_RUNTIME_OUT = "java"; 

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)

{

    log.Info("Starting function");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    string gauth = parametersReader.GetParameter<string>("graphAccessToken");
    
    string auth = parametersReader.GetParameter<string>("accessToken");

    return new {

        oGAuth = gauth,
        oAuth = auth

    };
}
