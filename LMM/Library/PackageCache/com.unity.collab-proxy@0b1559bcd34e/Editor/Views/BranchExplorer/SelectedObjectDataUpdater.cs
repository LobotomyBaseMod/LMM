using System;

using Codice.CM.Common;
using Codice.Client.BaseCommands.BranchExplorer;
using Codice.Client.Common.Threading;
using Codice.CM.Common.Mount;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.Inspector.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class SelectedObjectDataUpdater
    {
        internal SelectedObjectDataUpdater(
            //WorkspaceInfo wkInfo,
            RepositorySpec repSpec//,
            //IRegisterDiffWindows window,
            //IPendingChangesUpdater pendingChangesUpdater,
            //IIncomingChangesUpdater incomingChangesUpdater,
            //IShelvedChangesUpdater shelvedChangesUpdater,
            /*IWorkspaceWindow workspaceWindow*/,
            EditorWindow window)
        {
            mRepSpec = repSpec;
            mWindow = window;
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;
            //mPropertiesPanel.UpdateRepositorySpec(repSpec);
            //mAttributesPanel.UpdateRepositorySpec(repSpec);
        }

        internal void UpdateDisplayData(ObjectDrawInfo focusedObjectDrawInfo)
        {
            if (focusedObjectDrawInfo == null)
            {
                lock (mLock)
                {
                    mFocusedRepObject = null;
                    mFocusedObjectDrawInfo = null;
                }

                if (EditorWindow.focusedWindow == mWindow)
                    Selection.activeObject = null;
                return;
            }

            mFocusedObjectDrawInfo = focusedObjectDrawInfo;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                threadOperationDelegate: delegate
                {
                    CalculateData();
                },
                afterOperationDelegate: delegate
                {
                    if(waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    UpdateResults();
                });
        }

        void CalculateData()
        {
            lock(mLock)
            {
                mFocusedRepObject = BranchExplorerObjectResolver.
                    GetRepObjectInfo(mRepSpec, mFocusedObjectDrawInfo);
            }
        }

        void UpdateResults()
        {
            lock(mLock)
            {
                if (EditorWindow.focusedWindow != mWindow)
                    return;

                if (IsMultiLabelObject(mFocusedObjectDrawInfo))
                {
                    Selection.activeObject = null;
                    return;
                }

                if (!IsSameObject(mFocusedObjectDrawInfo, mFocusedRepObject))
                    return;

                if (mFocusedRepObject == null)
                {
                    Selection.activeObject = null;
                    return;
                }

                MountPointWithPath mountPoint = MountPointWithPath.BuildWorkspaceRootMountPoint(mRepSpec);

                SelectedRepObjectInfoData selectedData = SelectedRepObjectInfoData.Create(
                    mFocusedRepObject,
                    mRepSpec,
                    mountPoint);

                Selection.activeObject = selectedData;
            }
        }

        static bool IsSameObject(
            ObjectDrawInfo objectDrawInfo,
            RepObjectInfo selectedObject)
        {
            if (objectDrawInfo == null || selectedObject == null)
                return true;

            if (objectDrawInfo is LabelDrawInfo)
                return IsSameLabelDraw((LabelDrawInfo)objectDrawInfo, selectedObject);

            // compare by guid for branches and changesets
            return objectDrawInfo.Guid == selectedObject.GUID;
        }

        static bool IsSameLabelDraw(
            LabelDrawInfo labelDrawInfo,
            RepObjectInfo selectedObject)
        {
            if (labelDrawInfo == null)
                return false;

            // compare by object id
            return labelDrawInfo.Labels.Length == 1
                && labelDrawInfo.Labels[0].Id == selectedObject.Id;
        }

        static bool IsMultiLabelObject(
            ObjectDrawInfo objectDrawInfo)
        {
            if (objectDrawInfo == null)
                return false;

            if (objectDrawInfo is not LabelDrawInfo)
                return false;

            return ((LabelDrawInfo)objectDrawInfo).Labels.Length > 1;
        }

        object mLock = new object();

        RepositorySpec mRepSpec;

        ObjectDrawInfo mFocusedObjectDrawInfo;
        RepObjectInfo mFocusedRepObject;

        //readonly WorkspaceInfo mWkInfo;
        readonly EditorWindow mWindow;
    }
}
