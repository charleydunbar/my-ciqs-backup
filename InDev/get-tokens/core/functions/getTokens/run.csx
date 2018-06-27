
#load "..\CiqsHelpers\All.csx"

using System.Net;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)

{

    log.Info("Starting function");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    string gauth = parametersReader.GetParameter<string>("graphAccessToken");
    
    string auth = parametersReader.GetParameter<string>("accessToken");

    log.Info(gauth);
    log.Info(auth);

    return new {

        oGAuth = gauth,
        oAuth = auth

    };
}
