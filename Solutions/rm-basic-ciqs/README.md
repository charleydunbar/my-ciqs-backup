# Remote Monitoring v2 (Basic) - CIQS
### Solution Description
This CIQS solution deploys the RMv2 - Basic Solution Accelerator. It is setup such that if Azure Maps is not a registered resource provider for the subscription, it will try to deploy with a 'Static' map.

----
### Manifest(s)

#### Manifest.xml
- This is the default Manifest used for deployment. It deploys RMv2 - Basic.
- Flow
	+ Manual - Enter parameters (msRuntime)
	+ ARM Deployment - [defaults1.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#defaults1json)
	+ ARM Deployment - [defaults2.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#defaults2json)
	+ Bootstrap Azure Function App
	+ Azure Function - [createServicePrincipal](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#createserviceprincipal-javascript)
	+ Azure Function - [createCertificate](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#createcertificate-javascript)
	+ Azure Function - [generatePassword](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#generatepassword-c)
	+ Azure Function - [convertMSRuntime](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#convertmsruntime-c)
	+ Azure Function - [checkMapsQuota](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#checkmapsquota-c)
	+ ARM Deployment - [basic.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#basicjson)
	+ Azure Function - [startStreamingJob](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#startstreamingjob-c)
	+ Azure Function - [awaitWebsiteCSharp](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/rm-basic-ciqs/README.md#awaitwebsitecsharp-c)
	+ Manual - Done
----
### Azure Functions
#### createServicePrincipal (JavaScript)
This Azure Function creates a service principal for the application and, if necessary, assigns the role of 'owner' under the specified subscription.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| azureWebsiteName | No | {ResourceGroup.Name} | Used as the 'displayName' for the created application. Also used to  create the website URL, which is then used for the application's 'identifierUris' and 'reployUrls'. |
| subscriptionId | No | {SubscriptionId} | The subscription that the application is assigned roles (such as 'owner') for (if applicable). |
| userId | No | {UserId} | Used by a dependent API to identify and retrieve tokens from the 'tokenCache'. Necessary only because the API expects a format for tokens with this value included.  |
| tenantId | No | {TenantId} | Used to create the authorities for which different tokens act upon. Used to initialize the 'GraphRbacManagementClient'. |
| graphAccessToken | No | {GraphAuthorization} | Used as authorization. |
| accessToken | No | {Authorization} | Used as authorization when assigning roles (such as 'owner') to the application (if applicable) |
| solutionSku | Yes | "basic" | Essentially dictates whether or not roles should be assigned  to the application. "standard" will assign roles and "basic" will not. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oGraphAccessToken | The 'graphAccessToken' from the function Inputs |
| oSolutionSku | The 'solutionSku' from the function Inputs |
| oAppId | The application ID of the created application object |
| oDomainName | The domain name for the created application object |
| oObjectId | The service principal object ID |
| oServicePrincipalId | The service principal ID |
| oServicePrincipalSecret | The service principal secret |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)
| Name | Value | Description |
| ---- | ----- | ----------- |
| _clientId | "04b07795-8ddb-461a-bbee-02f9e1bf7b46" | Azure CLI default clientId. Calls to dependent API break without this. Used by the tokens in the tokenCache. |
| roleId | "8e3af657-a8ff-443c-a75c-2fe8c4bcb635" | Identifier of the 'owner' role. Meaning on "standard" calls to this function, role assignment will always be 'owner' under the provided subscription. |




#### createCertificate (JavaScript)
This Azure Function creates a Certificate for the deployment to use.
###### Inputs

###### Outputs
| Name | Description |
| ---- | ----------- |
| cert | The created certificate |
| certThumbprint | The certificate thumbprint |
| certPrivateKey | The private key of the certificate |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)




#### generatePassword (C#)
This Azure Function generates a 12 character password.
###### Inputs

###### Outputs
| Name | Description |
| ---- | ----------- |
| oPassword | A randomly generated password of length 12. |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)




#### convertMSRuntime (C#)
This Azure Function converts {Inputs.msRuntime} ('C#' or 'Java') into the format expected by the ARM template ('dotnet' or 'java').
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| msRuntime | No | {Inputs.msRuntime} | The user selected runtime for the VM (either 'C#' or 'Java'). |

###### Outputs
| Name | Description |
| ---- | ----------- |
| microServiceRuntime | The ARM expected format of {Inputs.msRuntime} ('dotnet' for 'C#' and 'java' for 'Java'). |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)
| Name | Value | Description |
| ---- | ----- | ----------- |
| CSHARP_RUNTIME_IN | "C#" | The C# runtime format displayed to user. |
| JAVA_RUNTIME_IN | "Java" | The Java runtime format displayed to user. |
| CSHARP_RUNTIME_OUT | "dotnet" | The C# runtime format fed into the ARM template. |
| JAVA_RUNTIME_OUT | "java" | The Java runtime format fed into the ARM template. |




#### checkMapsQuota (C#)
This Azure Function checks if Azure Maps ("microsoft.maps") is a registered resource provider for the Subscription. It then returns whether or not the deployment should use a static map ('true' if Azure Maps is not registered for the Subscription, 'false' if it is).  
Keeps parity with current, non-CIQS deployment. But in the future it would be good to add actual numeric checking of quota, if that functionality becomes supported.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| subscriptionId | No | {SubscriptionId} | The subscription to check if Azure Maps is a registered resource provider. |
| tokenKey | No | {Authorization} | The token that authorizes the requests made in this function. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oStaticMap | The value signifying whether or not to use a 'static' map ('true' for yes, 'false' for no). |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)
| Name | Value | Description |
| ---- | ----- | ----------- |
| AzureManagementVersion20150101 | "2015-01-01" | The API version to include in the HTTP requests. |
| AzureURL | "https://management.azure.com/" | The azure endpoint which prefixes the HTTP requests. |
| AzureMapsResourceTypeKey | "microsoft.maps" | The identifier for the Azure Maps resource provider (used to 'look-up' existence of Azure Maps in a Dictionary of registered resource providers). |




#### startStreamingJob (C#)
This Azure Function starts the Streaming Job provisioned by the deployment.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| accessToken | No | {Authorization} | The token that authorizes the requests made in this function. |
| subscriptionId | No | {SubscriptionId} | The subscription for which the resource group containing the streaming job exists. |
| resourceGroupName | No | {ResourceGroup.Name} | The name of the resource group the streaming job belongs to. |
| streamingJobsName | No | {Outputs.oStreamingJobsName} | The name of the streamingJob we are starting. |

###### Outputs

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)




#### awaitWebsiteCSharp (C#)
This Azure Function waits for the specified Uri to resolve, or timeout waiting and throw an error. (A JavaScript version was being used before, see 'awaitWebsite' in functions folder).
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| azureWebsite | No | {Outputs.azureWebsite} | The url we are waiting to be resolvable. |

###### Outputs

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)

----
### ARM Templates
#### defaults1.json
This ARM template generates the name for the IoT Hub (The output of which is a dependency for 'defaults2.json', which is why these templates are broken up).
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| solutionNameD1 | No | {ResourceGroup.Name} | Partly used to generate unique identifiers for the resource names. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oIotHubName | The name of the IoT Hub |




#### defaults2.json
This ARM template generates names for the different resources that could be used in the deployment (i.e Storage Account, Event Hub). It is also being used to make certain 'hardcoded' lengthy strings accessible by {Outputs.varName} in CIQS (to keep the Manifest a little cleaner).
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| solutionNameD | No | {ResourceGroup.Name} | Partly used to generate unique identifiers for the resource names. |
| iotHubNameD2 | No | {Outputs.oIotHubName} | The name of the IoT Hub. |
| pcsReleaseVersionD2 | Yes | master | The pcsReleaseVersion used for deployment in the ARM template. Partly composes the vmSetupScriptUri. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oStorageName | The name of the Storage Account |
| oDocumentDBName | The name of the Database |
| oVmName | The name of the Virtual Machine |
| oEventHubName | The name of the Event Hub |
| oEventHubNamespaceName | The name of the Event Hub Namespace |
| oEventHubAuthorizationName | Value: ('iothubroutes-' + iotHubName) |
| oStreamingJobsName | The name of the Streaming Job |
| oVmSetupScriptUri | The URI where the vmSetupScript is located |
| oApplyRuleFilterJsUdf | A value used for the streamingJobsQuery. Placed in {Outputs} for cleanliness |
| oFlattenMeasurementsJsUdf | A value used for the streamingJobsQuery. Placed in {Outputs} for cleanliness |
| oRemoveUnusedPropertiesJsUdf | A value used for the streamingJobsQuery. Placed in {Outputs} for cleanliness |
| oTransformQuery | A value used for the streamingJobsQuery. Placed in {Outputs} for cleanliness |
| oPcsReleaseVersion | The same pcsReleaseVersion in the inputs. Placed in {Outputs} to eliminate redundancy of hardcoding in the Manifest (Only one hardcoded declaration of value) |
| oAzureWebsiteName | The name of the azureWebsite |




#### basic.json
This Arm template deploys RMv2 - Basic. It has been modified to use ARM conditionals, so that deployment of Azure Maps, or a Static map can be done through a single template (at the moment this is the only way to offer functionality of defaulting to 'static' in the middle of a deployment, through CIQS). The value passed in for 'staticMap' dictates the conditionals.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| aadTenantId  | No | {TenantId} | The AAD tenant identifier (GUID) |
| solutionName  | No | {ResourceGroup.Name} | The name of the solution |
| aadClientId  | No | {Outputs.oAppId} | AAD application identifier (GUID) |
| remoteEndpointSSLThumbprint  | No | {Outputs.certThumbprint} | This is the thumbprint of the HTTPS SSL Certificate |
| remoteEndpointCertificate  | No | {Outputs.cert} | The certficate that needs to be updated to the VM |
| remoteEndpointCertificateKey  | No | {Outputs.certPrivateKey} | The certficate key that needs to be updated to the VM |
| adminPassword  | No | {Outputs.oPassword} | User password for the Linux Virtual Machines, must between 12 and 72 characters long and have 3 of the following: 1 uppercase character, 1 lowercase character, 1 number and 1 special character that is not slash (\\) or dash (-) |
| microServiceRuntime  | No | {Outputs.microServiceRuntime} | The microservice runtime of the solution |
| staticMap  | No | {Outputs.oStaticMap} | If the RMv2 deployment will not use a static map |
| iotHubName  | No | {Outputs.oIotHubName} | The name of Azure IoT Hub |
| storageName  | No | {Outputs.oStorageName} | The name of the storageAccount |
| documentDBName  | No | {Outputs.oDocumentDBName} | The name of the documentDB |
| vmName  | No | {Outputs.oVmName} | The name of the Virtual Machine |
| eventHubName  | No | {Outputs.oEventHubName} | The name of the Event Hub |
| eventHubNamespaceName  | No | {Outputs.oEventHubNamespaceName} | The name of the Event Hub Namespace |
| eventHubAuthorizationName  | No | {Outputs.oEventHubAuthorizationName} | Authorization Rule Name for Event Hub endpoint in Iot Hub |
| streamingJobsName  | No | {Outputs.oStreamingJobsName} | The name of Azure StreamingJobs |
| vmSetupScriptUri  | No | {Outputs.oVmSetupScriptUri} | The URL of the script to setup a single VM deployment |
| applyRuleFilterJsUdf  | Yes | {Outputs.oApplyRuleFilterJsUdf} | 1st child of 'streamingJobsQuery' object, separated to satisfy CIQS compilation |
| flattenMeasurementsJsUdf  | Yes | {Outputs.oFlattenMeasurementsJsUdf} | 2nd child of 'streamingJobsQuery' object, separated to satisfy CIQS compilation |
| removeUnusedPropertiesJsUdf  | Yes | {Outputs.oRemoveUnusedPropertiesJsUdf} | 3rd child of 'streamingJobsQuery' object, separated to satisfy CIQS compilation |
| transformQuery  | Yes | {Outputs.oTransformQuery} | 4th child of 'streamingJobsQuery' object, separated to satisfy CIQS compilation |
| pcsReleaseVersion  | Yes | {Outputs.oPcsReleaseVersion} | The release version is used for repoURL for reverse-proxy-dotnet and vmScriptUri |
| azureWebsiteName  | No | {Outputs.oAzureWebsiteName} | The name of the azure website that you want to create. It will be of format {azureWebsiteName}.azurewebsites.net |
| aadInstance  | Yes | https://login.microsoftonline.com/ | Url of the AAD login page (example: https://login.microsoftonline.com/) |
| solutionType  | Yes | RemoteMonitoringV2 | The type of the solution |
| solutionWebAppPort  | Yes | 80  | The port of the solution web application (e.g. 80, 443) |
| microServiceVersion  | Yes | testing  | The container image version of the solution |
| storageSkuName  | Yes | Standard_LRS  | The storage SKU name |
| storageEndpointSuffix  | Yes | core.windows.net | Suffix added to Azure Storage hostname |
| docDBConsistencyLevel  | Yes | Strong  | The documentDB deault consistency level for this account. |
| docDBMaxStalenessPrefix  | Yes | 10  | When documentDB consistencyLevel is set to BoundedStaleness, then this value is required, else it can be ignored. |
| docDBMaxIntervalInSeconds  | Yes | 5  | When documentDB consistencyLevel is set to BoundedStaleness, then this value is required, else it can be ignored. |
| eventHubRetentionInDays  | Yes | 1  | The event hub message retention in days |
| eventHubPartitionCount  | Yes | 2  | The event hub partition count |
| eventHubSkuTier  | Yes | Basic  | The Azure Event Hub SKU Tier |
| eventHubSkuCapacity  | Yes | 1  | The Azure Event Hub SKU Capacity |
| serviceBusEndpointSuffix  | Yes | servicebus.windows.net | Suffix added to Service Bus endpoint |
| iotHubSku  | Yes | S1 | The Azure IoT Hub SKU |
| iotHubTier  | Yes | Standard  | The Azure IoT Hub tier |
| streamingJobsOutputStartMode  | Yes | JobStartTime  | The start behavior of streamingjobs immediately upon creation |
| streamingJobsEventsOutOfOrderPolicy  | Yes | Adjust  | Events that arrive outside the delay window will be dropped or adjusted based on the value selected |
| streamingJobsInputContainerName  | Yes | referenceinput  | The container name of reference input for the streamingjobs |
| numberOfStreamingUnits  | Yes | 3 | Number of Streaming Units |
| vmSize  | Yes | Standard_D1_v2  | The size of the Virtual Machine. |
| ubuntuOSVersion  | Yes | 16.04.0-LTS | The Ubuntu version for the Virtual Machine. |
| vmFQDNSuffix  | Yes | cloudapp.azure.com |  |
| bingMapsLocation  | Yes | westus  | Bing Maps region (Marked for deletion, unused) |
| globalMapsLocation  | Yes | global  | Global Maps region (Marked for deletion, unused) |
| adminUsername  | Yes | rmadmin  | User name for the Linux Virtual Machine. |
| pcsDockerTag  | Yes | 1.0.0 | The docker tag can be same as release version and is the latest released docker image |

###### Outputs
| Name | Description |
| ---- | ----------- |
| resourceGroup  | The name of the resource group |
| messagesEventHubConnectionString  | The Event Hub connection string |
| messagesEventHubName  | The Event Hub name |
| iotHubConnectionString  | The IoT Hub connection string |
| documentDBConnectionString  | The Database connection string |
| azureWebsite  | The full azureWebsite URL |
| vmFQDN  |  |
| adminUsername  | The adminUsername for the VM |
| streamingJobsName  | The name of the Streaming Job |

###### Outputs


----
#### Other
