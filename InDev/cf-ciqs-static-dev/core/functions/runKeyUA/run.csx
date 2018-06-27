
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Opc.Ua;

// This Azure Function required more documentation
// **It appears to create certificates, runs some conversions, then returns values
//   necessary in the ARM template
public static async Task<object> Run (HttpRequestMessage req, TraceWriter log, ExecutionContext context) {

    log.Info ("runKeyUA: Entry");

    var uaSecretPassword = "password";
    var appName = "UA Web Client";
    var appUrn = "urn:localhost:Contoso:FactorySimulation:UA Web Client";

    // create temp folder and get path
    var certFolder = CreateTempFolder (context.FunctionDirectory);
    
    // create certificates?
    CreateCerts (certFolder, appName, appUrn);

    // do some manipulations?
    var x509Collection = new X509Certificate2Collection ();
    
    var certFilePath = $"{certFolder}\\private\\{appName.Replace(" ", "")}.pfx";
    x509Collection.Import (certFilePath, uaSecretPassword, X509KeyStorageFlags.Exportable);

    var keyVaultSecretBaseName = appName.Replace (" ", "");
    var keyVaultVmSecret = System.Convert.ToBase64String (x509Collection.Export (X509ContentType.Cert, uaSecretPassword));
    var keyVaultWebsiteSecret = System.Convert.ToBase64String (x509Collection.Export (X509ContentType.Pkcs12));
    var uaSecretThumbprint = x509Collection[0].Thumbprint;
    
    // delete temp folder
    ClearTempFolder (certFolder);

    return new {

        oKeyVaultSecretBaseName = keyVaultSecretBaseName,
        oKeyVaultVmSecret = keyVaultVmSecret,
        oKeyVaultWebsiteSecret = keyVaultWebsiteSecret,
        oUaSecretThumbprint = uaSecretThumbprint,
        oUaSecretPassword = uaSecretPassword

    };
    
}

// This function is meant to replicate the CreateCerts.exe execution, because this is an Azure Function
private static void CreateCerts (string outputPath, string appName, string applicationUri) {
    
    // cleanup previous runs
    try {
        Directory.Delete (outputPath + Path.DirectorySeparatorChar + "certs", true);
        Directory.Delete (outputPath + Path.DirectorySeparatorChar + "private", true);
    } catch (Exception) {
        // do nothing
    }

    // create certs
    string storeType = "Directory";
    string storePath = outputPath;
    string password = "password";
    string applicationURI = applicationUri;
    string applicationName = appName;
    string subjectName = applicationName;
    List<string> domainNames = null; // not used
    const ushort keySizeInBits = 2048;
    DateTime startTime = DateTime.Now;
    const ushort lifetimeInMonths = 120;
    const ushort hashSizeInBits = 256;
    bool isCA = false;
    X509Certificate2 issuerCAKeyCert = null; // not used

    if (!applicationURI.StartsWith ("urn:")) {
        applicationURI = "urn:" + applicationURI;
    }

    CertificateFactory.CreateCertificate (
        storeType,
        storePath,
        password,
        applicationURI,
        applicationName,
        subjectName,
        domainNames,
        keySizeInBits,
        startTime,
        lifetimeInMonths,
        hashSizeInBits,
        isCA,
        issuerCAKeyCert);

    // rename cert files to something we can copy easily
    //DirectoryInfo dir = new DirectoryInfo(args[0] + Path.DirectorySeparatorChar + "certs");
    DirectoryInfo dir = new DirectoryInfo (outputPath + Path.DirectorySeparatorChar + "certs");
    foreach (FileInfo file in dir.EnumerateFiles ()) {
        if (file.Extension == ".der") {
            //File.Move(file.FullName, file.DirectoryName + Path.DirectorySeparatorChar + args[1].Replace(" ", "") + file.Extension);
            File.Move (file.FullName, file.DirectoryName + Path.DirectorySeparatorChar + appName.Replace (" ", "") + file.Extension);
        }
    }
    //dir = new DirectoryInfo(args[0] + Path.DirectorySeparatorChar + "private");
    dir = new DirectoryInfo (outputPath + Path.DirectorySeparatorChar + "private");
    foreach (FileInfo file in dir.EnumerateFiles ()) {
        if (file.Extension == ".pfx") {
            //File.Move(file.FullName, file.DirectoryName + Path.DirectorySeparatorChar + args[1].Replace(" ", "") + file.Extension);
            File.Move (file.FullName, file.DirectoryName + Path.DirectorySeparatorChar + appName.Replace (" ", "") + file.Extension);
        }
    }
}

// creates and returns the path of a temporary folder
private static string CreateTempFolder (string basePath) {

    var path = Path.Combine (basePath, Guid.NewGuid ().ToString ());
    
    Directory.CreateDirectory (path);

    return path;
}

// deletes the folder indicated by 'path'
private static void ClearTempFolder (string path) {

    Directory.Delete (path, true);
}