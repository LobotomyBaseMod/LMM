using System;
using System.ComponentModel;
using Unity.Plastic.Newtonsoft.Json;

using PlasticGui.WebApi.Responses;

namespace Unity.PlasticSCM.Editor.WebApi
{
    // Placeholder. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("IsCollabProjectMigratedResponse is deprecated and will be removed in a future release", false)]
    public class IsCollabProjectMigratedResponse
    {
        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("error")]
        public ErrorResponse.ErrorFields Error { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("IsMigrated")]
        public bool IsMigrated { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("WebServerUri")]
        public string WebServerUri { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("PlasticCloudOrganizationName")]
        public string PlasticCloudOrganizationName { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("Credentials")]
        public CredentialsResponse Credentials { get; set; }
    }
}
