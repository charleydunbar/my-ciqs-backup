#load "..\CiqsHelpers\All.csx"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public static async Task<object> Run (HttpRequestMessage req, TraceWriter log) {

    log.Info ("generatePassword: Entry.");

    string retPassword = GeneratePassword ();

    log.Info ("generatePassword: Success.");

    return new {

        oPassword = retPassword

    };
}

private static string GeneratePassword (int minLength = 12, int maxLength = 12, string specialCharSet = null) {

    const string LOWER = "abcdefghijklmnopqrstuvwxyz";
    const string UPPER = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const string NUMBER = "0123456789";
    const string SPECIAL = @"~!@#$%^&()-_+=|<>\/;:,";

    if (specialCharSet == null) {
        specialCharSet = SPECIAL;
    }

    var random = new Random (Guid.NewGuid ().GetHashCode ());
    var length = Math.Max (4, random.Next (minLength, maxLength));

    return $"{RandomString(random, UPPER, 1)}{RandomString(random, NUMBER, 1)}{RandomString(random, specialCharSet, 1)}{RandomString(random, LOWER, length - 3)}";
}

private static string RandomString (Random random, string chars, int length) {

    return new string (Enumerable.Repeat (chars, length).Select (s => s[random.Next (s.Length)]).ToArray ());
}