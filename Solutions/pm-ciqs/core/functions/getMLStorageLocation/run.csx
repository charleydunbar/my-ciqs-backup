
#load "..\CiqsHelpers\All.csx"

using System.Net;

// This azure function gets the most appropriate MachineLearning location
// based on the solution deployment location
public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{

    log.Info("Starting function");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage(req);

    string deploymentLocation = parametersReader.GetParameter<string>("deploymentLocation");

    // make call to get MLStorageLocation
    string retLocation = GetMLStorageLocation(deploymentLocation);

    return new {

        oMlLocation = retLocation

    };
}

// This function employs a switch statement based on solution deployment
// location, returning most appropriate MLStorageLocation
private static string GetMLStorageLocation(string location)
{
    string mlLocation = location;
    switch (location.ToLowerInvariant().Replace(" ", ""))
    {
        case "eastus":
        case "westus":
            mlLocation = "southcentralus";
            break;
        case "northeurope":
            mlLocation = "westeurope";
            break;
        case "eastasia":
        case "japaneast":
        case "japanwest":
        case "australiaeast":
        case "australiasoutheast":
            mlLocation = "southeastasia";
            break;
        case "germanycentral":
        case "germanynortheast":
            mlLocation = "germanycentral";
            break;
        case "westeurope":
        case "southeastasia":
            // Use same storage location as suite
            break;
        default:
            mlLocation = "southcentralus";//throw new Exceptions.BusinessException("Unknown region '{0}'".FormatInvariant(location));
            break;
    }
    return mlLocation;
}
