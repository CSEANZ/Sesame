using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TrustedConsoleApp.models
{
    public class VerifyResponse
    {
        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("jwt")]
        public JWTToken JwtToken { get; set; }

    }

    public class JWTToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("expires")]
        public string Expires { get; set; }
    }
}
