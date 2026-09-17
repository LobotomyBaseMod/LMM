using System;
using System.ComponentModel;
using Unity.Plastic.Newtonsoft.Json;

using PlasticGui.WebApi.Responses;

namespace Unity.PlasticSCM.Editor.WebApi
{
    // Placeholder. This isn't a public API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("ChangesetFromCollabCommitResponse is deprecated and will be removed in a future release", false)]
    public class ChangesetFromCollabCommitResponse
    {
        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("error")]
        public ErrorResponse.ErrorFields Error { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("repId")]
        public uint RepId { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("repModuleId")]
        public uint RepModuleId { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("changesetId")]
        public long ChangesetId { get; set; }

        // Placeholder. This isn't a public API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonProperty("branchId")]
        public long BranchId { get; set; }
    }
}
