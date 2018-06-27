# Device Simulation - CIQS
### Solution Description
This CIQS solution deploys both variants of Device Simulation; with an IoT Hub, or without an IoT Hub. It implements the variants functionality provided by CIQS where two Manifest.xml files are provided and one variant is selected for deployment on the initial deployment screen.

----
### Manifest(s)

#### Manifest.xml
- This is the default Manifest used for deployment. It deploys Device Simulation, provisioning a new IoT Hub.
- Flow
	+ ARM Deployment - [defaults.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#defaultsjson)
	+ Bootstrap Azure Function App
	+ Azure Function - [createServicePrincipal](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#createserviceprincipal-javascript)
	+ Azure Function - [createCertificate](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#createcertificate-javascript)
	+ Azure Function - [getDomain](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#getdomain-c)
	+ Azure Function - [generatePassword](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#generatepassword-c)
	+ Azure Function - [generateDeploymentId](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#generatedeploymentid-c)
	+ ARM Deployment - [DeviceSimulationWithIoTHub.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#devicesimulationwithiothubjson)
	+ Azure Function - [awaitWebsiteCSharp](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#awaitwebsitecsharp-c)
	+ Manual - Done


#### ManifestNoHub.xml
- This is the secondary Manifest used for deployment. It deploys Device Simulation, relying on the use of existing IoT Hubs.
- Flow
	+ ARM Deployment - [defaults.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#defaultsjson)
	+ Bootstrap Azure Function App
	+ Azure Function - [createServicePrincipal](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#createserviceprincipal-javascript)
	+ Azure Function - [createCertificate](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#createcertificate-javascript)
	+ Azure Function - [getDomain](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#getdomain-c)
	+ Azure Function - [generatePassword](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#generatepassword-c)
	+ Azure Function - [generateDeploymentId](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#generatedeploymentid-c)
	+ ARM Deployment - [DeviceSimulationNoIoTHub.json](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#devicesimulationnoiothubjson)
	+ Azure Function - [awaitWebsiteCSharp](https://github.com/cadunbar/ciqs-solution-accelerators-backup/blob/master/Solutions/ds-ciqs/README.md#awaitwebsitecsharp-c)
	+ Manual - Done
----
### Azure Functions
#### createServicePrincipal (JavaScript)
This Azure Function creates a service principal for the application and, if necessary, assigns the role of 'owner' under the specified subscription.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| azureWebsiteName | No | {Outputs.oAzureWebsiteName} | Used as the 'displayName' for the created application. Also used to  create the website URL, which is then used for the application's 'identifierUris' and 'reployUrls'. |
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




#### getDomain (C#)
This Azure Function gets the default domain for the given tenantId.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| tenantId | No | {TenantId} | The tenantId for which we are trying the find the default domain. |
| graphAccessToken | No | {GraphAuthorization} | The authorization for the operations that run in the function. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oDomain | The default domain of the given tenant ID. |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)




#### generatePassword (C#)
This Azure Function generates a 12 character password.
###### Inputs

###### Outputs
| Name | Description |
| ---- | ----------- |
| oPassword | A randomly generated password of length 12. |

###### Hardcoded values in code (Highlighted in case these should not be hardcoded)




#### generateDeploymentId (C#)
This Azure Function generates a guid to be used as the deploymentId.
###### Inputs

###### Outputs
| Name | Description |
| ---- | ----------- |
| oGenGuid | A generated guid. |

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
#### defaults.json
This ARM template generates names for the different resources that could be used in the deployment (i.e Storage Account, IoT Hub).
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| solutionNameD1 | No | {ResourceGroup.Name} | Partly used to generate unique identifiers for the resource names. |

###### Outputs
| Name | Description |
| ---- | ----------- |
| oStorageName | The name of the Storage Account (if used) |
| oIotHubName | The name of the IoT Hub (if used) |
| oDocumentDBName | The name of the Database (if used) |
| oVMName | The name of the VM (if used) |
| oAzureWebsiteName | The name of the Azure Website (if used) |




#### DeviceSimulationWithIoTHub.json
This Arm template deploys Device Simulation provisioning a new IoT Hub.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| aadTenantId  | No | {TenantId} | The AAD tenant identifier (GUID) |
| subscriptionId  | No | {SubscriptionId} | ID of the Azure Subscription where the solution is deployed |
| solutionName  | No | {ResourceGroup.Name} | The name of the solution |
| aadClientId  | No | {Outputs.oAppId} | AAD application identifier (GUID) |
| remoteEndpointSSLThumbprint  | No | {Outputs.certThumbprint} | This is the thumbprint of the HTTPS SSL Certificate |
| remoteEndpointCertificate  | No | {Outputs.cert} | The certificate that needs to be uploaded to the VM |
| remoteEndpointCertificateKey  | No | {Outputs.certPrivateKey} | The certificate key that needs to be uploaded to the VM |
| domain  | No | {Outputs.oDomain} | The Azure Active Directory domain used by the Azure Subscription where the solution is deployed |
| adminPassword  | No | {Outputs.oPassword} | User password for the Linux Virtual Machines, must between 12 and 72 characters long and have 3 of the following: 1 uppercase character, 1 lowercase character, 1 number and 1 special character that is not slash (\\) or dash (-) |
| deploymentId  | No | {Outputs.oGenGuid} | Unique Id of the deployment. |
| storageName  | No | {Outputs.oStorageName} | The name of the storageAccount |
| iotHubName  | No | {Outputs.oIotHubName} | The name of Azure IoT Hub |
| documentDBName  | No | {Outputs.oDocumentDBName} | The name of the CosmosDB |
| vmName  | No | {Outputs.oVMName} | The name of the Linux Virtual Machine |
| azureWebsiteName  | No | {Outputs.oAzureWebsiteName} | The name of the azure website that you want to create. It will be of format {azureWebsiteName}.azurewebsites.net |
| aadInstance  | Yes | https://login.microsoftonline.com/ | URL of the AAD login page (example: https://login.microsoftonline.com/) |
| storageSkuName  | Yes | Standard_LRS | The storage SKU name |
| storageEndpointSuffix  | Yes | core.windows.net | Suffix added to Azure Storage hostname |
| docDBConsistencyLevel  | Yes | Strong | The CosmosDB default consistency level for this account. |
| docDBMaxStalenessPrefix  | Yes | 10 | When CosmosDB consistency level is set to BoundedStaleness, then this value is required, otherwise it can be ignored. |
| docDBMaxIntervalInSeconds  | Yes | 5  | When CosmosDB consistency level is set to BoundedStaleness, then this value is required, otherwise it can be ignored. |
| iotHubSku  | Yes | S2  | The Azure IoT Hub SKU |
| iotHubTier  | Yes | Standard  | The Azure IoT Hub tier |
| iotHubUnits  | Yes | 1  | The number of IoT Hub units created |
| vmSize  | Yes | Standard_D4_v3 | The size of the Virtual Machine. |
| ubuntuOSVersion  | Yes | 16.04.0-LTS | The Ubuntu version for the Virtual Machine. |
| vmSetupScriptUri  | Yes | https://raw.githubusercontent.com/Azure/pcs-cli/DS-1.0.1/solutions/devicesimulation/single-vm/setup.sh | The URL of the script to setup a single VM deployment |
| vmFQDNSuffix  | Yes | cloudapp.azure.com |  |
| adminUsername  | Yes | dsadmin  | User name for the Linux Virtual Machine. |
| solutionType  | Yes | DeviceSimulation  | The type of the solution |
| diagnosticsEndpointUrl  | Yes | https://iotpcsdiagnostics-staging.azurewebsites.net/api/diagnosticsevents | Diagnostics service endpoint url |

###### Outputs
| Name | Description |
| ---- | ----------- |
| resourceGroup  | The resource group name. |
| iotHubConnectionString  | The connection string for the IoT Hub. |
| documentDBConnectionString  | The connection string for the database. |
| azureWebsite  | The Azure Website URL. |
| vmFQDN  |  |
| adminUsername  | The admin username for the VM. |




#### DeviceSimulationNoIoTHub.json
This Arm template deploys Device Simulation but does not provision a new IoT Hub.
###### Inputs
| Name | Hardcoded | Value | Description |
| ---- | --------- | ----- | ----------- |
| aadTenantId  | No | {TenantId} | The AAD tenant identifier (GUID) |
| subscriptionId  | No | {SubscriptionId} | ID of the Azure Subscription where the solution is deployed |
| solutionName  | No | {ResourceGroup.Name} | The name of the solution |
| aadClientId  | No | {Outputs.oAppId} | AAD application identifier (GUID) |
| remoteEndpointSSLThumbprint  | No | {Outputs.certThumbprint} | This is the thumbprint of the HTTPS SSL Certificate |
| remoteEndpointCertificate  | No | {Outputs.cert} | The certificate that needs to be uploaded to the VM |
| remoteEndpointCertificateKey  | No | {Outputs.certPrivateKey} | The certificate key that needs to be uploaded to the VM |
| domain  | No | {Outputs.oDomain} | The Azure Active Directory domain used by the Azure Subscription where the solution is deployed |
| adminPassword  | No | {Outputs.oPassword} | User password for the Linux Virtual Machines, must between 12 and 72 characters long and have 3 of the following: 1 uppercase character, 1 lowercase character, 1 number and 1 special character that is not slash (\\) or dash (-) |
| deploymentId  | No | {Outputs.oGenGuid} | Unique Id of the deployment. |
| storageName  | No | {Outputs.oStorageName} | The name of the storageAccount |
| documentDBName  | No | {Outputs.oDocumentDBName} | The name of the CosmosDB |
| vmName  | No | {Outputs.oVMName} | The name of the Linux Virtual Machine |
| azureWebsiteName  | No | {Outputs.oAzureWebsiteName} | The name of the azure website that you want to create. It will be of format {azureWebsiteName}.azurewebsites.net |
| aadInstance  | Yes | https://login.microsoftonline.com/ | URL of the AAD login page (example: https://login.microsoftonline.com/) |
| storageSkuName  | Yes | Standard_LRS | The storage SKU name |
| storageEndpointSuffix  | Yes | core.windows.net | Suffix added to Azure Storage hostname |
| docDBConsistencyLevel  | Yes | Strong | The CosmosDB default consistency level for this account. |
| docDBMaxStalenessPrefix  | Yes | 10 | When CosmosDB consistency level is set to BoundedStaleness, then this value is required, otherwise it can be ignored. |
| docDBMaxIntervalInSeconds  | Yes | 5  | When CosmosDB consistency level is set to BoundedStaleness, then this value is required, otherwise it can be ignored. |
| vmSize  | Yes | Standard_D4_v3 | The size of the Virtual Machine. |
| ubuntuOSVersion  | Yes | 16.04.0-LTS | The Ubuntu version for the Virtual Machine. |
| vmSetupScriptUri  | Yes | https://raw.githubusercontent.com/Azure/pcs-cli/DS-1.0.1/solutions/devicesimulation/single-vm/setup.sh | The URL of the script to setup a single VM deployment |
| vmFQDNSuffix  | Yes | cloudapp.azure.com |  |
| adminUsername  | Yes | dsadmin  | User name for the Linux Virtual Machine. |
| solutionType  | Yes | DeviceSimulation  | The type of the solution |
| diagnosticsEndpointUrl  | Yes | https://iotpcsdiagnostics-staging.azurewebsites.net/api/diagnosticsevents | Diagnostics service endpoint url |

###### Outputs
| Name | Description |
| ---- | ----------- |
| resourceGroup  | The resource group name. |
| documentDBConnectionString  | The connection string for the database. |
| azureWebsite  | The Azure Website URL. |
| vmFQDN  |  |
| adminUsername  | The admin username for the VM. |


----
#### Other
