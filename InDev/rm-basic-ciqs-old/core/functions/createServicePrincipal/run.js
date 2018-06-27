/*

    SUMMARY:
    
    This AzureFunction creates ServicePrincipal given the required parameters.
    .
    .
    . add stuff later

*/


"use strict";
exports.__esModule = true;

var uuid = require('uuid');
var momemt = require('moment');

var adal = require('adal-node');

var msRestAzure = require('ms-rest-azure');
var AuthResponse = msRestAzure.AuthResponse;
var AzureEnvironment = msRestAzure.AzureEnvironment;
var DeviceTokenCredentials = msRestAzure.DeviceTokenCredentials;
var DeviceTokenCredentialsOptions = msRestAzure.DeviceTokenCredentialsOptions;
var LinkedSubscription = msRestAzure.LinkedSubscription;
var InteractiveLoginOptions = msRestAzure.InteractiveLoginOptions;
var interactiveLoginWithAuthResponse = msRestAzure.interactiveLoginWithAuthResponse;

var GraphRbacManagementClient = require('azure-graph');
var AuthorizationManagementClient = require('azure-arm-authorization');

var commander = require('commander');
var Command = commander.Command;

var packageJson = require('../package.json');










// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// ENTRY POINT
// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------

module.exports = function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');
    context.log('createServicePrincipal: Entry.');
    
    
    // ----- RETRIEVING INPUTS ---------------------------------------------------------
        
    var azureWebsiteName = context.bindings.req.body.azureWebsiteName;
    var subscriptionId = context.bindings.req.body.subscriptionId;
    var userId = context.bindings.req.body.userId;
    var tenantId = context.bindings.req.body.tenantId;
    var graphAccessToken = context.bindings.req.body.graphAccessToken;
    var accessToken = context.bindings.req.body.accessToken; // necessary for 'standard' solutionSku deployment
    var solutionSku = context.bindings.req.body.solutionSku;
    
    // ----- OTHER REQUIRED DATA -------------------------------------------------------
    
    var azureEnv = AzureEnvironment.Azure; // !!! May not be hardcoded in the future !!!
    var _authorityBaseURI = azureEnv.activeDirectoryEndpointUrl;
    var _authorityTenantID = tenantId;
    var _clientId = "04b07795-8ddb-461a-bbee-02f9e1bf7b46"; // Azure CLI default clientId

    // ----- PREPPING DATA -------------------------------------------------------------
    
    var options = {};
    
    options.environment = azureEnv;
    options.username = userId;
    options.domain = tenantId;
    options.tokenCache = new adal.MemoryCache();

    var entryGraphToken = {};    
    
    entryGraphToken.accessToken = graphAccessToken; // used for authorization 'https://management.core.windows.net/' (during createServicePrincipal())
    entryGraphToken.tokenType = "Bearer";
    entryGraphToken.userId = userId;
    entryGraphToken._clientId = _clientId;
    entryGraphToken.isMRRT = false; // basically says that this token entry does not contain a refresh token, meaning the accessToken should be refreshed before calling this func 
    entryGraphToken.resource = azureEnv.activeDirectoryGraphResourceId;

    entryGraphToken._authority = _authorityBaseURI + _authorityTenantID;
    entryGraphToken.expiresOn = momemt(new Date()).add(45, 'm').toDate(); // token should be valid for an hour, adding this field with 45 minutes so APIs don't assume the token is expired and try to refresh

    var entryResourceToken = {}; // used for authorization 'https://management.core.windows.net/' (during createRoleAssignmentWithRetry())
    
    entryResourceToken.accessToken = accessToken;
    entryResourceToken.tokenType = "Bearer";
    entryResourceToken.userId = userId;
    entryResourceToken._clientId = _clientId;
    entryResourceToken.isMRRT = false; // basically says that this token entry does not contain a refresh token, meaning the accessToken should be refreshed before calling this func 
    entryResourceToken.resource = azureEnv.activeDirectoryResourceId;

    entryResourceToken._authority = _authorityBaseURI + _authorityTenantID;
    entryResourceToken.expiresOn = momemt(new Date()).add(45, 'm').toDate(); // token should be valid for an hour, adding this field with 45 minutes so APIs don't assume the token is expired and try to refresh


    options.tokenCache._entries[0] = entryGraphToken;
    options.tokenCache._entries[1] = entryResourceToken;

    // ----- CALLING CREATE_SERVICE_PRINCIPAL ------------------------------------------
    
    createServicePrincipal(azureWebsiteName, subscriptionId, options, solutionSku).then(function(result) {
        
        // Output values in Promise returned from createServicePrincipal()
        context.bindings.res = {
            oGraphAccessToken: graphAccessToken,
            oSolutionSku: solutionSku,
            oAppId: result.appId,
            oDomainName: result.domainName,
            oObjectId: result.objectId,
            oServicePrincipalId: result.servicePrincipalId,
            oServicePrincipalSecret: result.servicePrincipalSecret
        };

        context.log('createServicePrincipal: Success.');
        context.done();
    })
    .catch(function (error) {
        // error occured, exit with error
        context.log('createServicePrincipal: Failed.');
        context.log(error);
        context.done(error);
    });

};










// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// ADAPTED CODE
// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------

var solutionSkus;
(function (solutionSkus) {
    solutionSkus[solutionSkus["basic"] = 0] = "basic";
    solutionSkus[solutionSkus["standard"] = 1] = "standard";
    solutionSkus[solutionSkus["local"] = 2] = "local";
})(solutionSkus || (solutionSkus = {}));

var gitHubUrl = 'https://github.com/Azure/pcs-cli';
var gitHubIssuesUrl = 'https://github.com/azure/pcs-cli/issues/new';

var MAX_RETRYCOUNT = 36;

var program = new Command(packageJson.name)
    .version(packageJson.version, '-v, --version')
    .option('-t, --type <type>', 'Solution Type: remotemonitoring', /^(remotemonitoring|test)$/i, 'remotemonitoring')
    .option('-s, --sku <sku>', 'SKU Type (only for Remote Monitoring): basic, standard, or local', /^(basic|standard|local)$/i, 'basic')
    .option('-e, --environment <environment>', 'Azure environments: AzureCloud or AzureChinaCloud', /^(AzureCloud|AzureChinaCloud)$/i, 'AzureCloud')
    .option('-r, --runtime <runtime>', 'Microservices runtime: dotnet or java', /^(dotnet|java)$/i, 'dotnet')
    .option('--servicePrincipalId <servicePrincipalId>', 'Service Principal Id')
    .option('--servicePrincipalSecret <servicePrincipalSecret>', 'Service Principal Secret')
    .option('--versionOverride <versionOverride>', 'Current accepted value is "master"')
    .on('--help', function () {
    console.log();
})
    .parse(process.argv);










// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// CREATE_SERVICE_PRINCIPAL
// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------

function createServicePrincipal(azureWebsiteName, subscriptionId, options, solutionSku) {

    var homepage = getWebsiteUrl(azureWebsiteName);
    var graphOptions = options;
    graphOptions.tokenAudience = 'graph';
    var baseUri = options.environment ? options.environment.activeDirectoryGraphResourceId : undefined;
    var graphClient = new GraphRbacManagementClient(new DeviceTokenCredentials(graphOptions), options.domain ? options.domain : '', baseUri);
    var startDate = new Date(Date.now());
    var endDate = new Date(startDate.toISOString());
    var m = momemt(endDate);
    m.add(1, 'years');
    endDate = new Date(m.toISOString());
    var identifierUris = [homepage];
    var replyUrls = [homepage];
    var newServicePrincipalSecret = uuid.v4();
    var existingServicePrincipalSecret = program.servicePrincipalSecret;
    // Allowing Graph API to sign in and read user profile for newly created application
    var requiredResourceAccess = [{
            resourceAccess: [
                {
                    // This guid represents Sign in and read user profile
                    // http://www.cloudidentity.com/blog/2015/09/01/azure-ad-permissions-summary-table/
                    id: '311a71cc-e848-46a1-bdf8-97ff7156d8e6',
                    type: 'Scope'
                }
            ],
            // This guid represents Directory Graph API ID
            resourceAppId: '00000002-0000-0000-c000-000000000000'
        }];
    var applicationCreateParameters = {
        availableToOtherTenants: false,
        displayName: azureWebsiteName,
        homepage: homepage,
        identifierUris: identifierUris,
        oauth2AllowImplicitFlow: true,
        passwordCredentials: [{
                endDate: endDate,
                keyId: uuid.v1(),
                startDate: startDate,
                value: newServicePrincipalSecret
            }
        ],
        replyUrls: replyUrls,
        requiredResourceAccess: requiredResourceAccess
    };

    var objectId = '';
    return graphClient.applications.create(applicationCreateParameters)
        .then(function (result) {
        var servicePrincipalCreateParameters = {
            accountEnabled: true,
            appId: result.appId
        };
        objectId = result.objectId;
        return graphClient.servicePrincipals.create(servicePrincipalCreateParameters);
    })
        .then(function (sp) {
            
            if (solutionSku === solutionSkus[solutionSkus.basic] || solutionSku === solutionSkus[solutionSkus.local] ||
            (program.servicePrincipalId && program.servicePrincipalSecret)) {
                return sp.appId;
            }
            
            // Create role assignment only for standard deployment since ACS requires it
            return createRoleAssignmentWithRetry(subscriptionId, sp.objectId, sp.appId, options);
            
    })
        .then(function (appId) {
        return graphClient.domains.list()
            .then(function (domains) {
            var domainName = '';
            var servicePrincipalId = program.servicePrincipalId ? program.servicePrincipalId : appId;
            var servicePrincipalSecret = existingServicePrincipalSecret ? existingServicePrincipalSecret : newServicePrincipalSecret;
            domains.forEach(function (value) {
                if (value.isDefault) {
                    domainName = value.name;
                }
            });
            return {
                appId: appId,
                domainName: domainName,
                objectId: objectId,
                servicePrincipalId: servicePrincipalId,
                servicePrincipalSecret: servicePrincipalSecret
            };
        });
    })
        .catch(function (error) {
        throw error;
    });
}

// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// END CREATE_SERVICE_PRINCIPAL
// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------










function createRoleAssignmentWithRetry(subscriptionId, objectId, appId, options) {
    var roleId = '8e3af657-a8ff-443c-a75c-2fe8c4bcb635'; // that of a owner
    var scope = '/subscriptions/' + subscriptionId; // we shall be assigning the sp, a 'contributor' role at the subscription level
    var roleDefinitionId = scope + '/providers/Microsoft.Authorization/roleDefinitions/' + roleId;
    // clearing the token audience
    options.tokenAudience = undefined;
    var baseUri = options.environment ? options.environment.resourceManagerEndpointUrl : undefined;
    var authzClient = new AuthorizationManagementClient(new DeviceTokenCredentials(options), subscriptionId, baseUri);
    var assignmentGuid = uuid.v1();
    var roleCreateParams = {
        properties: {
            principalId: objectId,
            // have taken this from the comments made above
            roleDefinitionId: roleDefinitionId,
            scope: scope
        }
    };
    var retryCount = 0;
    var promise = new Promise(function (resolve, reject) {
        var timer = setInterval(function () {
            retryCount++;
            return authzClient.roleAssignments.create(scope, assignmentGuid, roleCreateParams)
                .then(function (roleResult) {
                clearInterval(timer);
                resolve(appId);
            })
                .catch(function (error) {
                if (retryCount >= MAX_RETRYCOUNT) {
                    clearInterval(timer);
                    console.log(error);
                    reject(error);
                }
            });
        }, 5000);
    });
    return promise;
}

function getDomain() {
    var domain = '.azurewebsites.net';
    switch (program.environment) {
        case AzureEnvironment.Azure.name:
            domain = '.azurewebsites.net';
            break;
        case AzureEnvironment.AzureChina.name:
            domain = '.chinacloudsites.cn';
            break;
        case AzureEnvironment.AzureGermanCloud.name:
            domain = '.azurewebsites.de';
            break;
        case AzureEnvironment.AzureUSGovernment.name:
            domain = '.azurewebsites.us';
            break;
        default:
            domain = '.azurewebsites.net';
            break;
    }
    return domain;
}

function getWebsiteUrl(hostName) {
    var domain = getDomain();
    return "https://" + hostName + domain;
}
