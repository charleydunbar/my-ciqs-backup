
#load "..\CiqsHelpers\All.csx"

using System.Net;

// This azure function basically removes '-' characters from storageNames
// because they are invalid. Though, should other name manipulation be required
// for other resources, this function should handle that too.
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{

    log.Info("Starting function");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    // Get resource names
    string storageName = parametersReader.GetParameter<string>("preStorageName");
    string mlStorageName = parametersReader.GetParameter<string>("preMlStorageName");

    // Get modified resource names
    string retStorageName = ModifyStorageResourceName(storageName);
    string retMlStorageName = ModifyStorageResourceName(mlStorageName);

    return new {

        oStorageName = retStorageName,
        oMlStorageName = retMlStorageName

    };
}

// This function returns a valid storageAccount resource name for the
// passed in storageName
private static string ModifyStorageResourceName(string storageName) {

    // '-' are not allowed in storageAccount resource names
    return storageName.Replace("-", string.Empty);
}