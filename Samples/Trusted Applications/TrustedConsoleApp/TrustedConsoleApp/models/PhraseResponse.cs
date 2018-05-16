using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TrustedConsoleApp.models
{
    class PhraseResponse
    {
        [JsonProperty("phrase")]
        public string Phrase { get; set; }

}
}
