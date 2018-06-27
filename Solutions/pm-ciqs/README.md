# Predictive Maintenance - CIQS
### Solution Description
This CIQS solution deploys the Predictive Maintenance Solution Accelerator. Right now, it is setup so that all resource provisioning (including the ML Workspace) is done through a single ARM template, rather than being split into different arm templates or provisioning resources in code. Though, an additional ARM template is needed at the end to update a couple settings on one of the resources.

----
### Manifest(s)

#### Manifest.xml
- This is the default Manifest used for deployment. It deploys Predictive Maintenance.
- Flow
	+ ARM Deployment - [defaults.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#defaultsjson)
	+ Bootstrap Azure Function App
	+ Azure Function - [createServicePrincipal](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#createserviceprincipal-javascript)
	+ Azure Function - [createCertificate](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#createcertificate-javascript)
	+ Azure Function - [getMLStorageLocation](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#getmlstoragelocation-c)
	+ Azure Function - [modifyResourceNames](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#modifyresourcenames-c)
	+ ARM Deployment - [predictivemaintenance.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#predictivemaintenancejson)
	+ Azure Function - [uploadFileToStorage](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#uploadfiletostorage-c)
	+ Azure Function - [setupMLWorkspace](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#setupmlworkspace-c)
	+ ARM Deployment - [updateTemplate.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#updatetemplatejson)
	+ Azure Function - [awaitWebsiteCSharp](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/pm-ciqs/README.md#awaitwebsitecsharp-c)
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




#### getMLStorageLocation (C#)
This Azure Function selects the most appropriate Machine Learning (ML) location based on the solution deployment location.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| deploymentLocation | No | {Location} | The user selected deployment location. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oMlLocation | The Machine Learning location. |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)
ML locations are selected in a switch statement that uses hardcoded values. If either the ML locations or the deploy location mappings change, the switch statement should be updated.




#### modifyResourceNames (C#)
This Azure Function removes invalid characters from resource names and modifies them so they are valid.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| preStorageName | No | {Outputs.oPreStorageName} | The storageAccount resource name (may not be valid). |
| preMlStorageName | No | {Outputs.oPreMlStorageName} | The ML storageAccount resource name (may not be valid). |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oStorageName | The modified, valid storageAccount resource name. |
| oMlStorageName | The modified, valid ML storageAccount resource name. |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)




#### uploadFileToStorage (C#)
This Azure Function uploads a specified file from a fileUrl, to the specified storageAccount.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| accountName | No | {Outputs.oStorageName} | The name of the storage account to upload the file to. |
| key | No | {Outputs.oStorageAccountKey} | The storageAccount key that allows access for uploading the file. |
| containerName | Yes | simulatordata | The container name for the data. |
| fileName | Yes | data.csv | The destination file name to create/upload to. |
| fileUrl | Yes | http://aka.ms/azureiot/predictivemaintenance/data | The uri for the fileData to upload into the storageAccount. |

###### Outputs

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)
| Name | Value | Description |
| ---- | ----- | ----------- |
| STORAGE_ENDPOINT | "core.windows.net" | The endpoint for the storageAccount. |




#### setupMLWorkspace (C#)
This Azure Function copies experiments over to the provisioned ML Workspace and returns the MLApiUrl and MLApiKey that the 'jobhost' App Service resource depends on.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| deploymentLocation | No | {Location} | Used to deduce which Machine learning endpoints to use in operation requests. |
| mlLocation | No | {Outputs.oMllocation} | The location of the provisioned ML workspace and its respective storage account. |
| workspaceId | No | {Outputs.oMlWorkspaceWorkspaceID} | The ID for the provisioned ML workspace. |
| tokenKey | No | {Outputs.oMlWorkspaceToken} | The token used to Authorize operations on the ML workspace. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oMlApiUrl | The ML API URL, added as a field in the Application settings of the 'jobhost' App Service resource. |
| oMlApiKey | The ML API Key, added as a field in the Application settings of the 'jobhost' App Service resource. |
| oMLHelpLocation | The URI for help in regard to the Machine Learning Workspace. |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)
| Name | Value | Description |
| ---- | ----- | ----------- |
| trainingUri | Lengthy, see in code. | The URI for the 'Remaining Useful Life Engines 1' experiment, to copy into the ML workspace. |
| scoringUri | Lengthy, see in code. | The URI for the 'Remaining Useful Life Predictive Exp 2' experiment, to copy into the ML workspace. |
| ExperimentName | "Remaining Useful Life [Predictive Exp.]" | The name of the webService to be created for the ML workspace. (Not actually used in creation, but rather to pre-check for its existence. |




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
#### defaults.json
This ARM template generates names for the different resources that could be used in the deployment (i.e Storage Account, IoT Hub).
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| solutionNameD1 | No | {ResourceGroup.Name} | Partly used to generate unique identifiers for the resource names. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oPreStorageName | The name of the Storage Account |
| oIotHubName | The name of the IoT Hub |
| oPreMlStorageName | The name of the ML Storage Account |
| oMlWorkspaceName | The name of the ML Workspace |




#### predictivemaintenance.json
This Arm template deploys Predictive maintenance, provisioning Storage Accounts, the ML Workspace, and all other resources in a single template.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| aadTenant   | No | {TenantId} | The name of the service Tenant |
| suiteName   | No | {ResourceGroup.Name} | The name of the suite |
| ehName   | No | {ResourceGroup.Name} | The name of the eventHub |
| ownerId    | No | {UserId} | The owner of the machine learning workspace |
| aadClientId  | No | {Outputs.oAppId} | AAD application identifier (GUID) |
| mlLocation  | No | {Outputs.oMlLocation} | Location for ML storage account |
| storageName  | No | {Outputs.oStorageName} | The name of the storageAccount |
| mlStorageName  | No | {Outputs.oMlStorageName} | The name of the Machine Learning storageAccount |
| iotHubName  | No | {Outputs.oIotHubName} | The name of the iotHub |
| mlWorkspaceName  | No | {Outputs.oMlWorkspaceName} | The name of the machine learning workspace |
| aadInstance  | Yes | https://login.microsoftonline.com/{0} | Url of the AAD login page (example: https://login.microsoftonline.de/{0}) |
| packageUri  | Yes | http://aka.ms/azureiot/predictivemaintenance/web |  |
| webJobPackageUri  | Yes | http://aka.ms/azureiot/predictivemaintenance/webjob | |
| asaStartBehavior  | Yes | JobStartTime | The start behavior for Stream Analytics jobs [LastStopTime | JobStartTime (default)] |
| asaStartTime  | Yes | notused | The start time for Stream Analytics jobs |
| storageEndpointSuffix  | Yes | core.windows.net | Suffix added to Azure Storage hostname (examples: core.windows.net, core.cloudapi.de) |
| simulatorDataFileName  | Yes | data.csv  | The file name for the simulator data |
| mlApiUrl   | Yes | updateLater  | The machine learning API location |
| mlApiKey   | Yes | updateLater  | The machine learning API key |

###### Outputs
| Name | Description |
| ---- | ----------- |
| iotHubHostName   | The IoT Hub Hostname. |
| iotHubConnectionString   | The IoT Hub connection string. |
| storageConnectionString   | The Storage Account connection string. |
| ehDataName   | The Event Hub name. |
| ehConnectionString   | The Event Hub connection string. |
| azureWebsite   | The azureWebsite URL |
| TableName  | Value: "DeviceList" |
| ObjectTypePrefix   | Value: "" |
| oMlWorkspaceObject   | The ML Workspace Object |
| oMlWorkspaceToken   | The ML Workspace access token |
| oMlWorkspaceWorkspaceID   | The ML Workspace ID |
| oMlWorkspaceWorkspaceLink   | The ML Workspace URL |
| oStorageAccountId   | The Storage Account ID |
| oStorageAccountKey   | The Storage Account Primary Key |
| oMlStorageAccountKey   | The ML Storage Account Primary Key |
| oMlStorageAccountId   | The ML Storage Account ID |




#### updateTemplate.json
This Arm template updates the 'jobhost' App Service resource by adding the MlApiUrl and MlApiKey values to its Application Settings. This must be done in a separate ARM template because these values are obtained by running code to copy experiments over to the ML workspace after it is provisioned in the main ARM deployment.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| hostName  | No | {Outputs.iotHubHostName} | The IoT Hub Hostname|
| iotHubConnectionString  | No | {Outputs.iotHubConnectionString} | The IoT Hub Connection Sring |
| storageConnectionString  | No | {Outputs.storageConnectionString} | The Storage Account Connection String |
| ehDataName  | No | {Outputs.ehDataName} | The Event Hub Name |
| eventHubConnectionString  | No | {Outputs.ehConnectionString} | The Event Hub Connection String |
| simulatorDataFileName  | Yes | data.csv | The file name for the simulator data |
| mlApiUrl  | No | {Outputs.oMlApiUrl} | The machine learning API location |
| mlApiKey  | No | {Outputs.oMlApiKey} | The machine learning API key |
| suiteName  | No | {ResourceGroup.Name} | The name of the suite |

###### Outputs


----
#### Other
