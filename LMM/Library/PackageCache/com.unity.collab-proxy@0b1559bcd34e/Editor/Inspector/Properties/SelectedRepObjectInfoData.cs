using System;
using Codice.CM.Common;
using Codice.CM.Common.Mount;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Inspector.Properties
{
    internal class SelectedRepObjectInfoData : ScriptableObject
    {
        internal RepObjectInfo ObjectInfo { get; private set; }
        internal RepositorySpec RepSpec { get; private set; }
        internal MountPointWithPath MountPoint { get; private set; }

        internal static SelectedRepObjectInfoData Create(
            RepObjectInfo objectInfo,
            RepositorySpec repSpec,
            MountPointWithPath mountPoint = null)
        {
            SelectedRepObjectInfoData result = CreateInstance<SelectedRepObjectInfoData>();
            result.ObjectInfo = objectInfo;
            result.RepSpec = repSpec;
            result.MountPoint = mountPoint;

            return result;
        }
    }
}
