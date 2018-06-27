#load "..\CiqsHelpers\All.csx"

using System.Net;
using System.IdentityModel.Tokens.Jwt;

// This azure function grabs the user's objectId from the jwt access token and returns
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("getUserObjectId: Entry");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string accessToken = parametersReader.GetParameter<string> ("accessToken");

    // create token object
    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(accessToken);

    // grab value of 'oid' or objectId
    var objectId = token.Claims.FirstOrDefault(c => c.Type.Equals("oid"))?.Value;

    return new {

        oRdxOwnerServicePrincipalObjectId = objectId

    };
    
}
