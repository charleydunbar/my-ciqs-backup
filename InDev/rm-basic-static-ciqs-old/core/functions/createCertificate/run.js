"use strict";
exports.__esModule = true;
var forge = require("node-forge");

module.exports = function createCertificate(context, req) {
    context.log('createCertificate: Entry.');
    
    var pki = forge.pki;
    // generate a keypair and create an X.509v3 certificate
    var keys = pki.rsa.generateKeyPair(2048);
    var certificate = pki.createCertificate();
    certificate.publicKey = keys.publicKey;
    certificate.serialNumber = '01';
    certificate.validity.notBefore = new Date(Date.now());
    certificate.validity.notAfter = new Date(Date.now());
    certificate.validity.notAfter.setFullYear(certificate.validity.notBefore.getFullYear() + 1);
    // self-sign certificate
    certificate.sign(keys.privateKey);
    var cert = forge.pki.certificateToPem(certificate);
    var thumbprint = forge.md.sha1.create().update(forge.asn1.toDer(pki.certificateToAsn1(certificate)).getBytes()).digest().toHex();

    context.res = {
        status: 200,
        body: {
            cert: cert,
            certThumbprint: thumbprint,
            certPrivateKey: forge.pki.privateKeyToPem(keys.privateKey)
        }
    };

    context.log('createCertificate: Success.');    
    context.done();
}
