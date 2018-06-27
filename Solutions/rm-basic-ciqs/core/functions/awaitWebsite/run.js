
"use strict";
exports.__esModule = true;

const fetch = require("node-fetch");
const MAX_RETRY = 36;

module.exports = function (context, req) {
    
    context.log('awaitWebsite: Entry.');
        
    var azureWebsite = context.bindings.req.body.azureWebsite;


    waitForWebsiteToBeReady(azureWebsite).then(function(result) {
        
        context.log('awaitWebsite: Success.');
        context.done();
    })
    .catch(function (error) {
        // error occured, exit with error
        context.log('awaitWebsite: Failed.');
        context.log(error);
        context.done(error);
    });
};

function waitForWebsiteToBeReady(url) {
    const status = url + '/ssl-proxy-status';
    const req = new fetch.Request(status, { method: 'GET' });
    let retryCount = 0;
    return new Promise((resolve, reject) => {
        const timer = setInterval(() => {
            fetch.default(req)
                .then((value) => {
                return value.json();
            })
                .then((body) => {
                if (body.Status.includes('Alive') || retryCount > MAX_RETRY) {
                    clearInterval(timer);
                    if (retryCount > MAX_RETRY) {
                        resolve(false);
                    }
                    else {
                        resolve(true);
                    }
                }
            })
                .catch((error) => {
                // Continue
                if (retryCount > MAX_RETRY) {
                    clearInterval(timer);
                    resolve(false);
                }
            });
            retryCount++;
        }, 10000);
    });
}
