using Codice.CM.Common;
using Unity.PlasticSCM.Editor.Headless;
using Unity.PlasticSCM.Editor.Inspector.Properties.Branch;
using Unity.PlasticSCM.Editor.Inspector.Properties.Changeset;
using Unity.PlasticSCM.Editor.Inspector.Properties.Label;
using Unity.PlasticSCM.Editor.Tool;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using EditorWindowInternal = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorWindow;

namespace Unity.PlasticSCM.Editor.Inspector.Properties
{
    [CustomEditor(typeof(SelectedRepObjectInfoData))]
    internal class PropertiesViewer : UnityEditor.Editor
    {
        internal BranchPropertiesPanel BranchPropertiesPanel => mBranchPropertiesPanel;
        internal ChangesetPropertiesPanel ChangesetPropertiesPanel => mChangesetPropertiesPanel;

        public override bool UseDefaultMargins()
        {
            return false;
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (target == null)
                return null;

            if (mSelectedRepObjectInfoData == target)
                return mRootContainer;

            mRootContainer = new VisualElement();

            IMGUIContainer imguiContainer = new IMGUIContainer(DrawInspectorGUI);
            imguiContainer.style.flexGrow = 1;
            mRootContainer.Add(imguiContainer);

            mRootContainer.RegisterCallback<AttachToPanelEvent>(OnAttatchToPanel);

            SetSelectedObject(target as SelectedRepObjectInfoData);

            return mRootContainer;
        }

        internal void DisposeForTesting()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (mBranchPropertiesPanel != null)
                mBranchPropertiesPanel.ResetForTesting();

            if (mChangesetPropertiesPanel != null)
                mChangesetPropertiesPanel.ResetForTesting();

            if (mLabelPropertiesPanel != null)
                mLabelPropertiesPanel.ResetForTesting();
        }

        void OnAttatchToPanel(AttachToPanelEvent evt)
        {
            ScrollView parentScrollView = mRootContainer.GetFirstAncestorOfType<ScrollView>();

            if (parentScrollView == null)
                return;

            UpdateRootContainerHeight(parentScrollView.contentRect.height);
            parentScrollView.RegisterCallback<GeometryChangedEvent>(OnScrollGeometryChanged);
        }

        void OnScrollGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateRootContainerHeight(evt.newRect.height);
        }

        void UpdateRootContainerHeight(float parentScrollViewHeight)
        {
            // 7px is the magic number that makes our view to fit
            // the Inspector's scrollviewer viewport size
            // we need it to avoid the vertical scrollbar to appear
            mRootContainer.style.height = parentScrollViewHeight - 7;
        }

        void OnEnable()
        {
            if (mBranchPropertiesPanel != null)
                mBranchPropertiesPanel.OnEnable();

            if (mChangesetPropertiesPanel != null)
                mChangesetPropertiesPanel.OnEnable();

            if (mLabelPropertiesPanel != null)
                mLabelPropertiesPanel.OnEnable();

            EditorApplication.update += OnEditorUpdate;
        }

        void OnDestroy()
        {
            if (mRootContainer == null)
                return;

            mRootContainer.UnregisterCallback<AttachToPanelEvent>(OnAttatchToPanel);

            ScrollView parentScrollView = mRootContainer.GetFirstAncestorOfType<ScrollView>();
            parentScrollView?.UnregisterCallback<GeometryChangedEvent>(OnScrollGeometryChanged);

            if (target != null && !IsTargetUsedByOtherPropertiesViewer())
                DestroyImmediate(target);

            GetOrCreateBranchPropertiesPanel().ClearInfo();
            GetOrCreateLabelPropertiesPanel().ClearInfo();
            GetOrCreateChangesetPropertiesPanel().ClearInfo();
        }

        void OnDisable()
        {
            if (mBranchPropertiesPanel != null)
                mBranchPropertiesPanel.OnDisable();

            if (mChangesetPropertiesPanel != null)
                mChangesetPropertiesPanel.OnDisable();

            if (mLabelPropertiesPanel != null)
                mLabelPropertiesPanel.OnDisable();

            EditorApplication.update -= OnEditorUpdate;
        }

        void SetSelectedObject(SelectedRepObjectInfoData selectedRepObjectInfoData)
        {
            mSelectedRepObjectInfoData = selectedRepObjectInfoData;

            object objectInfo = selectedRepObjectInfoData?.ObjectInfo;
            mLastObjectInfo = objectInfo;
            if (objectInfo == null)
                return;

            if (objectInfo is BranchInfo)
            {
                GetOrCreateBranchPropertiesPanel().SetSelectedObject(selectedRepObjectInfoData);
                GetOrCreateLabelPropertiesPanel().ClearInfo();
                GetOrCreateChangesetPropertiesPanel().ClearInfo();
            }
            else if (objectInfo is MarkerInfo)
            {
                GetOrCreateLabelPropertiesPanel().SetSelectedObject(selectedRepObjectInfoData);
                GetOrCreateBranchPropertiesPanel().ClearInfo();
                GetOrCreateChangesetPropertiesPanel().ClearInfo();
            }
            else if (objectInfo is ChangesetInfo)
            {
                GetOrCreateChangesetPropertiesPanel().SetSelectedObject(selectedRepObjectInfoData);
                GetOrCreateBranchPropertiesPanel().ClearInfo();
                GetOrCreateLabelPropertiesPanel().ClearInfo();
            }
        }

        void DrawInspectorGUI()
        {
            if (mLastObjectInfo == null)
                return;

            if (mLastObjectInfo is BranchInfo)
            {
                GetOrCreateBranchPropertiesPanel().OnHeaderGUI();
                GetOrCreateBranchPropertiesPanel().OnInspectorGUI();
            }
            else if (mLastObjectInfo is MarkerInfo)
            {
                GetOrCreateLabelPropertiesPanel().OnHeaderGUI();
                GetOrCreateLabelPropertiesPanel().OnInspectorGUI();
            }
            else if (mLastObjectInfo is ChangesetInfo)
            {
                GetOrCreateChangesetPropertiesPanel().OnHeaderGUI();
                GetOrCreateChangesetPropertiesPanel().OnInspectorGUI();
            }
        }

        void OnEditorUpdate()
        {
            if (mLastObjectInfo == null)
                return;

            if (mLastObjectInfo is BranchInfo)
                GetOrCreateBranchPropertiesPanel().Update();
            else if (mLastObjectInfo is MarkerInfo)
                GetOrCreateLabelPropertiesPanel().Update();
            else if (mLastObjectInfo is ChangesetInfo)
                GetOrCreateChangesetPropertiesPanel().Update();
        }

        BranchPropertiesPanel GetOrCreateBranchPropertiesPanel()
        {
            if (mBranchPropertiesPanel == null)
            {
                EditorWindow inspectorWindow = EditorWindowInternal.GetInspectorWindow();

                mBranchPropertiesPanel = new BranchPropertiesPanel(
                    Repaint,
                    GetWorkspaceInfo(),
                    new HeadlessWorkspaceWindow(() => { }, info => { }),
                    new HeadlessViewSwitcher(),
                    new HeadlessHistoryViewLauncher(UVCSPlugin.Instance),
                    new HeadlessRefreshView(),
                    UVCSPlugin.Instance.AssetStatusCache,
                    mShowDownloadPlasticExeWindow,
                    UVCSPlugin.Instance.PendingChangesUpdater,
                    UVCSPlugin.Instance.DeveloperIncomingChangesUpdater,
                    UVCSPlugin.Instance.GluonIncomingChangesUpdater,
                    inspectorWindow,
                    PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
            }

            return mBranchPropertiesPanel;
        }

        LabelPropertiesPanel GetOrCreateLabelPropertiesPanel()
        {
            if (mLabelPropertiesPanel == null)
            {
                EditorWindow inspectorWindow = EditorWindowInternal.GetInspectorWindow();

                mLabelPropertiesPanel = new LabelPropertiesPanel(
                    Repaint,
                    GetWorkspaceInfo(),
                    new HeadlessWorkspaceWindow(() => { }, info => { }),
                    inspectorWindow);
            }

            return mLabelPropertiesPanel;
        }

        ChangesetPropertiesPanel GetOrCreateChangesetPropertiesPanel()
        {
            if (mChangesetPropertiesPanel == null)
            {
                EditorWindow inspectorWindow = EditorWindowInternal.GetInspectorWindow();

                mChangesetPropertiesPanel = new ChangesetPropertiesPanel(
                    Repaint,
                    GetWorkspaceInfo(),
                    new HeadlessWorkspaceWindow(() => { }, info => { }),
                    new HeadlessViewSwitcher(),
                    new HeadlessHistoryViewLauncher(UVCSPlugin.Instance),
                    new HeadlessRefreshView(),
                    UVCSPlugin.Instance.AssetStatusCache,
                    mShowDownloadPlasticExeWindow,
                    UVCSPlugin.Instance.PendingChangesUpdater,
                    UVCSPlugin.Instance.DeveloperIncomingChangesUpdater,
                    UVCSPlugin.Instance.GluonIncomingChangesUpdater,
                    inspectorWindow,
                    PlasticGui.Plastic.API.IsGluonWorkspace(mWkInfo));
            }

            return mChangesetPropertiesPanel;
        }

        WorkspaceInfo GetWorkspaceInfo()
        {
            if (mWkInfo == null)
            {
                mWkInfo = FindWorkspace.InfoForApplicationPath(
                    ApplicationDataPath.Get(), PlasticGui.Plastic.API);
            }

            return mWkInfo;
        }

        bool IsTargetUsedByOtherPropertiesViewer()
        {
            // check if the current target is displayed in other inspector
            PropertiesViewer[] allViewers =
                Resources.FindObjectsOfTypeAll<PropertiesViewer>();

            foreach (PropertiesViewer viewer in allViewers)
            {
                if (viewer == this)
                    continue;

                if (viewer.target == target)
                    return true;
            }

            return false;
        }

        protected override void OnHeaderGUI() { }

        public override void OnInspectorGUI() { }

        VisualElement mRootContainer;
        object mLastObjectInfo;

        WorkspaceInfo mWkInfo;
        SelectedRepObjectInfoData mSelectedRepObjectInfoData;

        BranchPropertiesPanel mBranchPropertiesPanel;
        LabelPropertiesPanel mLabelPropertiesPanel;
        ChangesetPropertiesPanel mChangesetPropertiesPanel;

        LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow =
            new LaunchTool.ShowDownloadPlasticExeWindow();
    }
}
