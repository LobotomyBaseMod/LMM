using System;
using System.ComponentModel;
using Unity.Plastic.Newtonsoft.Json;

// Placeholder. This isn't a public API.
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("OrganizationCredentials is deprecated and will be removed in a future release", false)]
public class OrganizationCredentials
{
    // Placeholder. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [JsonProperty("user")]
    public string User { get; set; }

    // Placeholder. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [JsonProperty("password")]
    public string Password { get; set; }
}

