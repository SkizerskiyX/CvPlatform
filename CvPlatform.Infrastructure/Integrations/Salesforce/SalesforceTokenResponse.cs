using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CvPlatform.Infrastructure.Integrations.Salesforce
{
    public class SalesforceTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("instance_url")]
        public string InstanceUrl { get; set; } = string.Empty;
       
    }
}
