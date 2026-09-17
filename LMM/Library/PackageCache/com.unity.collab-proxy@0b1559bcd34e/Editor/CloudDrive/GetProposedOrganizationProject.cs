using UnityEditor;

using Codice.CM.Common;

namespace Unity.PlasticSCM.Editor.CloudDrive
{
    internal static class GetProposedOrganizationProject
    {
        internal class Values
        {
            internal readonly string Organization;
            internal readonly string Project;

            internal Values(
                string organization,
                string project)
            {
                Organization = organization;
                Project = project;
            }
        }

        internal static Values FromCloudProjectSettings()
        {
            return GetProposedValuesFrom(
                CloudProjectSettings.organizationName,
                CloudProjectSettings.projectName);
        }

        static Values GetProposedValuesFrom(
            string cloudProjectOrganizationName,
            string cloudProjectProjectName)
        {
            if (string.IsNullOrEmpty(cloudProjectOrganizationName) ||
                string.IsNullOrEmpty(cloudProjectProjectName))
                return null;

            return new Values(
                string.Concat(
                    cloudProjectOrganizationName,
                    CloudServer.Separator,
                    CloudServer.UnityAlias),
                cloudProjectProjectName);
        }
    }
}
