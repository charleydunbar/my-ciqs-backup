
"use strict";
exports.__esModule = true;

const fs = require("fs");
const path = require("path");
const ssh2_1 = require("ssh2");
const msRestAzure = require("ms-rest-azure");
const k8s = require('@kubernetes/typescript-node');
const btoa = require('btoa');
const jsyaml = require("js-yaml");

var request = require('request');

var AzureEnvironment = msRestAzure.AzureEnvironment;
const MAX_RETRY = 36;
const DEFAULT_TIMEOUT = 10000;

class Config {
}

class K8sManager {
    constructor(namespace, kubeConfigFilePath, config) {
        this._retryCount = 0;
        this._namespace = namespace;
        this._configFilePath = kubeConfigFilePath;
        this._config = config;
        this._api = k8s.Config.fromFile(this._configFilePath);
        const kc = new k8s.KubeConfig();
        kc.loadFromFile(kubeConfigFilePath);
        this._betaApi = new k8s.Extensions_v1beta1Api(kc.getCurrentCluster().server);
        this._betaApi.authentications.default = kc;
        this._secret = new k8s.V1Secret();
        this._secret.apiVersion = 'v1';
        this._secret.metadata = new k8s.V1ObjectMeta();
        this._secret.metadata.name = 'tls-certificate';
        this._secret.metadata.namespace = this._namespace;
        this._secret.kind = 'Secret';
        this._secret.type = 'Opaque';
        this._secret.data = {};
    }
    createNamespace(name) {
        const ns = new k8s.V1Namespace();
        ns.apiVersion = 'v1';
        ns.kind = 'Namespace';
        ns.metadata = {};
        ns.metadata.name = this._namespace;
        return new Promise((resolve, reject) => {
            const timer = setInterval(() => {
                return this._api.createNamespace(ns)
                    .then((result) => {
                        clearInterval(timer);
                        resolve(result);
                    })
                    .catch((error) => {
                        if (error.code === 'ETIMEDOUT' && this._retryCount < MAX_RETRY) {
                            this._retryCount++;
                            // Create namespace: retrying
                        }
                        else {
                            let err = error;
                            if (error.code !== 'ETIMEDOUT') {
                                // Convert a response to properl format in case of json
                                err = JSON.stringify(error, null, 2);
                            }
                            clearInterval(timer);
                            reject(err);
                        }
                    });
            }, DEFAULT_TIMEOUT);
        });
    }
    setupAll() {
        // Setting up Kubernetes: Uploading secrets
        return this.setupSecrets()
            .then(() => {
                // Setting up Kubernetes: Uploading config map
                return this.setupConfigMap();
            })
            .then(() => {
                // Setting up Kubernetes: Starting web app and microservices
                return this.setupDeployment();
            });
    }
    setupSecrets() {
        this._secret.data['tls.crt'] = btoa(this._config.TLS.cert);
        this._secret.data['tls.key'] = btoa(this._config.TLS.key);
        return new Promise((resolve, reject) => {
            const timer = setInterval(() => {
                return this._api.createNamespacedSecret(this._namespace, this._secret)
                    .then((result) => {
                        clearInterval(timer);
                        resolve(result);
                    })
                    .catch((error) => {
                        if (error.code === 'ETIMEDOUT' && this._retryCount < MAX_RETRY) {
                            this._retryCount++;
                        }
                        else {
                            let err = error;
                            if (error.code !== 'ETIMEDOUT') {
                                // Convert a response to properl format in case of json
                                err = JSON.stringify(error, null, 2);
                            }
                            clearInterval(timer);
                            reject(err);
                        }
                    });
            }, DEFAULT_TIMEOUT);
        });
    }
    setupConfigMap() {
        const configPath = __dirname + path.sep + 'deployment-configmap.yaml';
        const configMap = jsyaml.safeLoad(fs.readFileSync(configPath, 'UTF-8'));
        configMap.metadata.namespace = this._namespace;
        configMap.data['security.auth.audience'] = this._config.ApplicationId;
        configMap.data['security.auth.issuer'] = 'https://sts.windows.net/' + this._config.AADTenantId + '/';
        configMap.data['security.application.secret'] = this.genPassword();
        configMap.data['azure.maps.key'] = this._config.AzureMapsKey ? this._config.AzureMapsKey : '';
        configMap.data['iothub.connstring'] = this._config.IoTHubConnectionString;
        configMap.data['docdb.connstring'] = this._config.DocumentDBConnectionString;
        configMap.data['iothubreact.hub.name'] = this._config.EventHubName;
        configMap.data['iothubreact.hub.endpoint'] = this._config.EventHubEndpoint;
        configMap.data['iothubreact.hub.partitions'] = this._config.EventHubPartitions;
        configMap.data['iothubreact.access.connstring'] = this._config.IoTHubConnectionString;
        configMap.data['iothubreact.azureblob.account'] = this._config.AzureStorageAccountName;
        configMap.data['iothubreact.azureblob.key'] = this._config.AzureStorageAccountKey;
        configMap.data['iothubreact.azureblob.endpointsuffix'] = this._config.AzureStorageEndpointSuffix;
        configMap.data['asa.eventhub.connstring'] = this._config.MessagesEventHubConnectionString;
        configMap.data['asa.eventhub.name'] = this._config.MessagesEventHubName;
        configMap.data['asa.azureblob.account'] = this._config.AzureStorageAccountName;
        configMap.data['asa.azureblob.key'] = this._config.AzureStorageAccountKey;
        configMap.data['asa.azureblob.endpointsuffix'] = this._config.AzureStorageEndpointSuffix;
        let deploymentConfig = configMap.data['webui-config.js'];
        deploymentConfig = deploymentConfig.replace('{TenantId}', this._config.AADTenantId);
        deploymentConfig = deploymentConfig.replace('{ApplicationId}', this._config.ApplicationId);
        deploymentConfig = deploymentConfig.replace('{AADLoginInstance}', this._config.AADLoginURL);
        configMap.data['webui-config.js'] = deploymentConfig;
        return this._api.createNamespacedConfigMap(this._namespace, configMap);
    }
    setupDeployment() {
        const promises = new Array();
        const allInOnePath = __dirname + path.sep + 'all-in-one.yaml';
        const data = fs.readFileSync(allInOnePath, 'UTF-8');
        const allInOne = jsyaml.safeLoadAll(data, (doc) => {
            doc.metadata.namespace = this._namespace;
            switch (doc.kind) {
                case 'Service':
                    if (doc.spec.type === 'LoadBalancer') {
                        doc.spec.loadBalancerIP = this._config.LoadBalancerIP;
                    }
                    promises.push(this._api.createNamespacedService(this._namespace, doc));
                    break;
                case 'ReplicationController':
                    promises.push(this._api.createNamespacedReplicationController(this._namespace, doc));
                    break;
                case 'Deployment':
                    let imageName = doc.spec.template.spec.containers[0].image;
                    if (imageName.includes('{runtime}')) {
                        doc.spec.template.spec.containers[0].image = imageName.replace('{runtime}', this._config.Runtime);
                    }
                    imageName = doc.spec.template.spec.containers[0].image;
                    if (imageName.includes('{dockerTag}')) {
                        doc.spec.template.spec.containers[0].image = imageName.replace('{dockerTag}', this._config.DockerTag);
                    }
                    promises.push(this._betaApi.createNamespacedDeployment(this._namespace, doc));
                    break;
                case 'Ingress':
                    doc.spec.rules[0].host = this._config.DNS;
                    doc.spec.tls[0].hosts[0] = this._config.DNS;
                    promises.push(this._betaApi.createNamespacedIngress(this._namespace, doc));
                    break;
                default:
                    console.log('Unexpected kind found in yaml file');
            }
        });
        return Promise.all(promises);
    }
    genPassword() {
        const chs = '0123456789-ABCDEVISFGHJKLMNOPQRTUWXYZ_abcdevisfghjklmnopqrtuwxyz'.split('');
        const len = chs.length;
        let result = '';
        for (let i = 0; i < 40; i++) {
            result += chs[Math.floor(len * Math.random())];
        }
        return result;
    }
}










module.exports = function (context, req) {

    context.log('JavaScript HTTP trigger function processed a request.');

    var allInOneUri = context.bindings.req.body.allInOneUri;
    var deploymentConfigMapUri = context.bindings.req.body.deploymentConfigMapUri;
    var sshRSAPrivateKey = context.bindings.req.body.sshRSAPrivateKey;

    var allInOnePath = __dirname + "\\all-in-one.yaml";
    var deploymentConfigMapPath = __dirname + "\\deployment-configmap.yaml";

    request(allInOneUri).pipe(fs.createWriteStream(allInOnePath));
    request(deploymentConfigMapUri).pipe(fs.createWriteStream(deploymentConfigMapPath));

    var rsaPrivatePath = __dirname + "\\id_rsa";
    var remotePath = "https://raw.githubusercontent.com/Azure/pcs-cli/master/solutions/remotemonitoring/single-vm/setup.sh";

    fs.writeFile(rsaPrivatePath, sshRSAPrivateKey, function(err) {
    if(err) {
        return context.log(err);
    }

    context.log("file created successfully");
});    

    var deploymentProperties = {};
    var answers = {};
    var tempOuts = {};

    tempOuts.containerServiceName = context.bindings.req.body.containerServiceName;
    tempOuts.masterFQDN = context.bindings.req.body.masterFQDN;
    tempOuts.adminUsername = context.bindings.req.body.adminUsername;
    tempOuts.storageAccountKey = context.bindings.req.body.storageAccountKey;
    tempOuts.storageAccountName = context.bindings.req.body.storageAccountName;
    tempOuts.azureMapsKey = context.bindings.req.body.azureMapsKey;
    tempOuts.agentFQDN = context.bindings.req.body.agentFQDN;
    tempOuts.documentDBConnectionString = context.bindings.req.body.documentDBConnectionString;
    tempOuts.eventHubEndpoint = context.bindings.req.body.eventHubEndpoint;
    tempOuts.eventHubName = context.bindings.req.body.eventHubName;
    tempOuts.eventHubPartitions = context.bindings.req.body.eventHubPartitions;
    tempOuts.iotHubConnectionString = context.bindings.req.body.iotHubConnectionString;
    tempOuts.loadBalancerIp = context.bindings.req.body.loadBalancerIp;
    tempOuts.messagesEventHubConnectionString = context.bindings.req.body.messagesEventHubConnectionString;
    tempOuts.messagesEventHubName = context.bindings.req.body.messagesEventHubName;

    tempOuts.streamingJobsName = context.bindings.req.body.azureWebsiteName;
    tempOuts.azureWebsite = context.bindings.req.body.azureWebsiteName;

    deploymentProperties.outputs = tempOuts;

    var tempCertData = {};

    tempCertData.cert = context.bindings.req.body.cert;
    tempCertData.fingerPrint = context.bindings.req.body.fingerPrint;
    tempCertData.key = context.bindings.req.body.key;

    answers.sshFilePath = __dirname + "\\id_rsa.pub";
    answers.dockerTag = context.bindings.req.body.dockerTag;
    answers.version = context.bindings.req.body.version;

    answers.appId = context.bindings.req.body.appId;
    answers.solutionName = context.bindings.req.body.solutionName;
    answers.deploymentSku = context.bindings.req.body.deploymentSku;
    answers.aadTenantId = context.bindings.req.body.aadTenantId;
    answers.runtime = context.bindings.req.body.runtime;
    answers.certData = tempCertData;

    context.log(answers);

/*
    tempOuts.containerServiceName = "chpre162-cluster";
    tempOuts.masterFQDN = "chpre162-mgmt.eastus.cloudapp.azure.com";
    tempOuts.adminUsername = "rmadmin";
    tempOuts.storageAccountKey = "x4WKxfVm5JwhAC7E57MccUpRSU2ndS70Zmep/hQITJ/jTQDj4HAUDW49lIxmXF4mjhrBL+ZCUtJhHmZDOayCVw==";
    tempOuts.storageAccountName = "storagel3rnc";
    tempOuts.azureMapsKey = "Sd5S8Uhf7JxEheDMbQn7QOfeBRLR4YslK-DW44uoDek";
    tempOuts.agentFQDN = "agent-l3rnc.eastus.cloudapp.azure.com";
    tempOuts.documentDBConnectionString = "AccountEndpoint=https://documentdb-l3rnc.documents.azure.com:443/;AccountKey=qwSbHgBI8C28kIeLe59HXHSTXv9FjfbVSuInY3Ktj3yKMR0ggzfkowgIaXDsIIwb4eDjOnRGHuyby01NKUTFXQ==;";
    tempOuts.eventHubEndpoint = "sb://iothub-ns-iothub-l3r-492400-51c1e12ecd.servicebus.windows.net/";
    tempOuts.eventHubName = "iothub-l3rnc";
    tempOuts.eventHubPartitions = "4";
    tempOuts.iotHubConnectionString = "HostName=iothub-l3rnc.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=9IrYMjgb4G9sz7FJs1K7zfuvGue3amvuFzCFjMAZb5M=";
    tempOuts.loadBalancerIp = "168.62.175.189";
    tempOuts.messagesEventHubConnectionString = "Endpoint=sb://eventhubnamespace-l3rnc.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=ayDAyvkQ0uijGMGhpbs8cCse4XsdYxxAfrwdB7hyoO8=";
    tempOuts.messagesEventHubName = "eventhub-l3rnc";

    tempOuts.streamingJobsName = "streamingjobs-l3rnc";
    tempOuts.azureWebsite = "https://chpre162.azurewebsites.net";

    deploymentProperties.outputs = tempOuts;

    var tempCertData = {};

    tempCertData.cert = "-----BEGIN CERTIFICATE-----\r\nMIICeTCCAWGgAwIBAgIBATANBgkqhkiG9w0BAQUFADAAMB4XDTE4MDYwNDE4MjIz\r\nMVoXDTE5MDYwNDE4MjIzMVowADCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoC\r\nggEBALGWGZcGLUk2J10dJCcg35Kju8F4NWESU/Hsk0VxRpwYc093XGCNvfY2sBHU\r\n+ielYL4HbSLRICjJiXRY7OTsynl9y8kEIRSm6KnERmlhSQMAUJOKTv+qtnj0qeTQ\r\nLisGlYFasORpFP7YAsOxYhxy827KRfItxAoYRncBaq4S6zaaLaDd3YWW03MK+e1D\r\nAN2yMwTNrlSKdDqe7GzRLzF8msPyl/xxkeUBEIPnireHzkxXIJJBV4xkaN7LN3dG\r\nAs2AZmKBu9k/FIKqJjGBNpW5vyFuWSi3R8bP4msJ90anOFnb3LfCh4MOfm4AkhHJ\r\nIM/3L0prqB1BlxZrTV9tVZ1+9MsCAwEAATANBgkqhkiG9w0BAQUFAAOCAQEAXTMi\r\nzTXK3ZJ3Bxhtp1bzngDEf4UXDBoYGV2E5VgorV4anJ72vVLMT2HX10gc2J/J4XMB\r\nogj7mCKaTkgNzXcTVMJZiohrxEuczQHe4z4fSJx3bOgAwvLg/DY8/r6RlloW+u/X\r\nVEt2IYr/eX5LljwxLP+Iw5QvorwRRhIJYETv+CIRDVPd6BDo6427SY7zBU+mqsss\r\n95ZQIpNijnLlJOo8ovEPvWZaUFraiYx0TZaUHIx0sVL8N/Spcn0WrTF/oBhNYqFm\r\n4qFHbeeBsR/I5LbKIkIUAuWrt63lch1h/pa7reA7Jq9tNfUmWOL2JNO7VM24pVzb\r\nGNHSDo9CBAeBgJKH7Q==\r\n-----END CERTIFICATE-----\r\n";
    tempCertData.fingerPrint = "8f5c91ae79e1b9c6ad563365853cd3c854b1e6b8";
    tempCertData.key = "-----BEGIN RSA PRIVATE KEY-----\r\nMIIEpQIBAAKCAQEAsZYZlwYtSTYnXR0kJyDfkqO7wXg1YRJT8eyTRXFGnBhzT3dc\r\nYI299jawEdT6J6VgvgdtItEgKMmJdFjs5OzKeX3LyQQhFKboqcRGaWFJAwBQk4pO\r\n/6q2ePSp5NAuKwaVgVqw5GkU/tgCw7FiHHLzbspF8i3EChhGdwFqrhLrNpotoN3d\r\nhZbTcwr57UMA3bIzBM2uVIp0Op7sbNEvMXyaw/KX/HGR5QEQg+eKt4fOTFcgkkFX\r\njGRo3ss3d0YCzYBmYoG72T8UgqomMYE2lbm/IW5ZKLdHxs/iawn3Rqc4Wdvct8KH\r\ngw5+bgCSEckgz/cvSmuoHUGXFmtNX21VnX70ywIDAQABAoIBAGuf0iZrAesKvNR7\r\nortr+tL+E/3ugjswRlupyp8dRXO4hbm1VvDVNjkPb6l+75Qzb+v6yDN/lgPiEEHI\r\n2tjqgNMcX/KVZA8GEJ9CaoHXCc6d1Dd2bOYZabjoXkZjvHcq6FSax/XFkYnZE+PR\r\njuo66DlOsRFSlyqfB6V74FFa+d3+k3fyi6aOX2n+wWDHdkxXXQxVXi4P3l6868n5\r\ncKYbdHTrceC9Xo2LPPi1BBcSfQYht7SFd1Bk5oBsOBlH5iBvYSmCVmO/OW35Krhm\r\n7piylxfA7fRN2i6i6M28i4MuVlK7DYfioQdtaLPxipR3rdawwU0SXgoOVsxMZvXe\r\nkPq0MyECgYEA5FqAkQE6dEfVUr5dDK16nZIxHzNynAmqDrtu2INOrd+J6XwE/9dO\r\nyeGX7wf0KUbXFfONHstkm9mEE1D0Fz7XZbLIpS9K9rUF2+bOT3CCjdqAOBinavD7\r\nfpvjaClEDwdeomUNW1FLGN1TV5cnknWLNJitVAV4kVpeZfJGttkml4MCgYEAxxYj\r\nuxaBLOeluvyRHK4MFLv1PruGjHDFXBUMarc6ZmzO1jRSOeMKtDuOzpdjxGtvvE5p\r\nez73r80ld4SrGmM/MmnownkWNIj7KEnWX0cjAbhr3mIWf5EMvVG9TAT/r4kCO/eR\r\ncjRTYxTVGLe+vTI//uKM7GipAkNH6QyCz29L4xkCgYEAgdyN/Oir63DmefXUSN9n\r\nObDnyoyhgudkFJi3At45olvbvDJRTYWOQvTOSJtHWSn2K3+kI30brB3ZJHsHNSkB\r\nqc4wmO/6O67atCHf9gFP3YgDHuO2YfTFsUzJ2HSPRdS1FrlNDT9/65YCTW+ii4HZ\r\nNoIVIBE0bcTspiFP4bBAaC0CgYEAptcs9nqzong28W78BTbutOmXaw0ogsV2/+Y7\r\n06rd7Dw/Uk/ioNReghBvaz5/w3nt17c2uqxYUiHvxiuOYLzPl2YeQ+vJ6hjpsie+\r\n2XX6JlTxQRqelCVwsa+wneaKiAafsrWUVEr5ns00kFRcKp3T97zQMMa87EKKHwn3\r\nDfPNevECgYEA4X0O4n2amZslyOl2PuBqsi1KwWyh8xwnlo0gRHY8BQYXt7vNSN2M\r\nF7cDsU/aV9BrN0tGWyjl1/m0YsYG3R08YVEQqVcahAocGvPDh8X/Rp8E0rTWHnOf\r\n02Y+3jYheHSJ2c33l2lyv+FtGsyG4Eohlmh/DkshEy+G1plrsVKIzAs=\r\n-----END RSA PRIVATE KEY-----\r\n";

    answers.sshFilePath = __dirname + "\\id_rsa.pub";
    answers.dockerTag = "1.0.0";
    answers.version = "1.0.0";

    answers.appId = "85850bbc-22f3-4f36-a587-aaca8e58783c";
    answers.solutionName = "chpre162";
    answers.deploymentSku = "standard";
    answers.aadTenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
    answers.runtime = "dotnet";
    answers.certData = tempCertData;
*/
    //var kubeConfigPath = "C:\\Users\\v-chdunb\\.kube\\config-chpre80-cluster";

    var environment = AzureEnvironment.Azure;

    var activeDirectoryEndpointUrl = environment.activeDirectoryEndpointUrl;
    var storageEndpointSuffix = environment.storageEndpointSuffix;

    if (storageEndpointSuffix.startsWith('.')) {
        storageEndpointSuffix = storageEndpointSuffix.substring(1);
    }


    var counter = 0;

    const timer = setInterval(() => {
        counter++;

        if (fs.existsSync(rsaPrivatePath) && fs.existsSync(allInOnePath) && fs.existsSync(deploymentConfigMapPath)) {
            clearInterval(timer);

            context.log("file exists");

            runPostArm(deploymentProperties, answers, activeDirectoryEndpointUrl, storageEndpointSuffix, context).then(function(result) {
                context.log("We out");
                context.done();
            })
            .catch(function (error) {
                context.log(error);
                context.done();
            });
        } else if (counter > 60) {
            clearInterval(timer);
            throw "Waiting for file timed out";
        }
    }, 1000);


/*
    context.log('JavaScript HTTP trigger function processed a request.');
    context.log(__dirname);

    var dirpath = __dirname;
    const sshClient = new ssh2_1.Client();

    var localPath = dirpath + "\\test";
    var remotePath = "https://raw.githubusercontent.com/Azure/pcs-cli/master/solutions/remotemonitoring/single-vm/setup.sh";

    fs.writeFile(localPath, "Hey there!", function(err) {
    if(err) {
        return console.log(err);
    }

    console.log("The file was saved!");
});*/


};

function runPostArm(deploymentProperties, answers, activeDirectoryEndpointUrl, storageEndpointSuffix, context) {

    context.log("in runPostArm");

    return downloadKubeConfig(deploymentProperties.outputs, answers.sshFilePath, context)
        .then((kubeConfigPath) => {

            context.log(kubeConfigPath);

            const outputs = deploymentProperties.outputs;
            const config = new Config();
            config.AADTenantId = answers.aadTenantId;
            config.AADLoginURL = activeDirectoryEndpointUrl;
            config.ApplicationId = answers.appId;
            config.AzureStorageAccountKey = outputs.storageAccountKey;
            config.AzureStorageAccountName = outputs.storageAccountName;
            config.AzureStorageEndpointSuffix = storageEndpointSuffix;
            // If we are under the plan limit then we should have received a query key
            config.AzureMapsKey = outputs.azureMapsKey;
            config.DockerTag = answers.dockerTag;
            config.DNS = outputs.agentFQDN;
            config.DocumentDBConnectionString = outputs.documentDBConnectionString;
            config.EventHubEndpoint = outputs.eventHubEndpoint;
            config.EventHubName = outputs.eventHubName;
            config.EventHubPartitions = outputs.eventHubPartitions;
            config.IoTHubConnectionString = outputs.iotHubConnectionString;
            config.LoadBalancerIP = outputs.loadBalancerIp;
            config.Runtime = answers.runtime;
            config.TLS = answers.certData;
            config.MessagesEventHubConnectionString = outputs.messagesEventHubConnectionString;
            config.MessagesEventHubName = outputs.messagesEventHubName;
            const k8sMananger = new K8sManager('default', kubeConfigPath, config);

            return k8sMananger.setupAll();
        })
        .catch((error) => {
            let err = error.toString();
            console.log(err);
            if (err.includes('Entry not found in cache.')) {
                err = 'Session expired, Please run pcs login again.';
            }
        });
        
}

function downloadKubeConfig(outputs, sshFilePath, context) {

    context.log(sshFilePath);
    context.log(outputs);

    const localKubeConfigPath = __dirname + path.sep + 'config' + '-' + outputs.containerServiceName;
    const remoteKubeConfig = '.kube/config';
    const sshDir = sshFilePath.substring(0, sshFilePath.lastIndexOf(path.sep));
    const sshPrivateKeyPath = sshDir + path.sep + 'id_rsa';

    context.log(sshPrivateKeyPath);

    const pk = fs.readFileSync(sshPrivateKeyPath, 'UTF-8');

    context.log("pk: " + pk);
    context.log("pkts: " + pk);

    const sshClient = new ssh2_1.Client();
    const config = {
        host: outputs.masterFQDN,
        port: 22,
        privateKey: pk,
        username: outputs.adminUsername
    };
    return new Promise((resolve, reject) => {
        let retryCount = 0;
        const timer = setInterval(() => {
            // First remove all listeteners so that we don't have duplicates
            sshClient.removeAllListeners();
            sshClient
                .on('ready', (message) => {
                    sshClient.sftp((error, sftp) => {
                        if (error) {
                            sshClient.end();
                            reject(error);
                            clearInterval(timer);
                            return;
                        }
                        sftp.fastGet(remoteKubeConfig, localKubeConfigPath, (err) => {
                            sshClient.end();
                            clearInterval(timer);
                            if (err) {
                                reject(err);
                                return;
                            }
                            resolve(localKubeConfigPath);
                        });
                    });
                })
                .on('error', (err) => {
                    if (retryCount++ > MAX_RETRY) {
                        clearInterval(timer);
                        reject(err);
                    }
                })
                .on('timeout', () => {
                    if (retryCount++ > MAX_RETRY) {
                        clearInterval(timer);
                        reject(new Error('Failed after maximum number of tries'));
                    }
                })
                .connect(config);
        }, 5000);
    });
}
