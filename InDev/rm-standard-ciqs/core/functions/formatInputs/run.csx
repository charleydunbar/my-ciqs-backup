#load "..\CiqsHelpers\All.csx"

using System;
using System.Net;
using System.Text;

public static async Task<object> Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("formatInput: Entry.");

    var parametersReader = await CiqsInputParametersReader.FromHttpRequestMessage (req);

    string sshRSAPrivateKey = parametersReader.GetParameter<string> ("sshRSAPrivateKey");

    StringBuilder sb = new StringBuilder ();

    var array = sshRSAPrivateKey.Split (' ');

    for (int i = 0; i < array.Count (); i++) {
        if (i < 4) {
            sb.Append (" " + array[i]);
        } else if (i == array.Count () - 4) {
            sb.Append ("\r\n" + array[i] + " ");
        } else if (i > array.Count () - 4) {
            sb.Append (array[i] + " ");
        } else {
            sb.Append ("\r\n" + array[i]);
        }
    }

    log.Info ("formatInput: Success.");

    return new {

        oSshRSAPrivateKey = sb.ToString ().Trim () + "\r\n"

    };
}