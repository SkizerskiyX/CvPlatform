using System;
using System.Collections.Generic;
using System.Text;

namespace CvPlatform.Infrastructure.Integrations.Salesforce
{
    public class SalesforceOptions
    {
        public string Domain { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

    }

}

