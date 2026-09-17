using System;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands.BranchExplorer;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.Help;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BranchExplorer;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.Views.Labels;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Menu
{
    internal class BranchExplorerLabelMenu :
        BranchExplorerViewLabelMenuOperations.ISelectionResolver
        //BranchExplorerViewExternalToolsMenuOperations.ISelectionResolver
    {
        internal BranchExplorerLabelMenu(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            EditorWindow window,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher switcher,
            IMergeViewLauncher mergeViewLauncher,
            BranchExplorerView branchExplorerView,
            BranchExplorerSelection selectionHandler,
            IProgressControls progressControls,
            GuiHelpEvents guiHelpEvents,
            IAssetStatusCache assetStatusCache,
            IPendingChangesUpdater pendingChangesUpdater,
            IIncomingChangesUpdater incomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            LaunchTool.IProcessExecutor processExecutor,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow)
        {
            mRepSpec = repSpec;
            mSelectionHandler = selectionHandler;

            mLabelOperations = new BranchExplorerViewLabelMenuOperations(
                wkInfo,
                repSpec,
                window,
                workspaceWindow,
                switcher,
                mergeViewLauncher,
                branchExplorerView,
                selectionHandler,
                this,
                progressControls,
                guiHelpEvents,
                assetStatusCache,
                pendingChangesUpdater,
                incomingChangesUpdater,
                shelvedChangesUpdater,
                processExecutor,
                showDownloadPlasticExeWindow);

            /*
            mExternalToolsMenuOperations = new BranchExplorerViewExternalToolsMenuOperations(
                wkInfo, repSpec, selectionHandler, this, progressControls);
                */

            mLabelsViewMenu = LabelsViewMenu.BuildForContextMenu(mLabelOperations);
        }

        internal void BrowseRepositoryOnLabel()
        {
            mLabelOperations.BrowseRepositoryOnLabel();
        }

        internal void Popup()
        {
            mSubmenuLabel = null;

            if (!mSelectionHandler.HasSelectedLabels())
                return;

            LabelDrawInfo currentLabelDrawInfo = GetSelectedLabel(mSelectionHandler);

            if (currentLabelDrawInfo.Labels.Length == 1 ||
                mSelectionHandler.GetSelectedLabels().Count > 2)
            {
                mLabelsViewMenu.Popup();
                return;
            }

            PopupMultipleLabelMenu(currentLabelDrawInfo.Labels);
        }

        internal bool ProcessKeyActionIfNeeded(Event e)
        {
            return mLabelsViewMenu.ProcessKeyActionIfNeeded(e);
        }

        internal void UpdateRepositorySpec(RepositorySpec repSpec)
        {
            mRepSpec = repSpec;
            //mExternalToolsMenuOperations.UpdateRepositorySpec(repSpec);
        }

        void PopupMultipleLabelMenu(BrExLabel[] labels)
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < labels.Length; i++)
            {
                if (NeedsSeparator(
                        i, labels.Length, BrExDrawProperties.MaxLabelsPerChangeset))
                {
                    menu.AddSeparator(string.Empty);
                }

                string escapedLabelName = EscapeLabelName(labels[i].Name);
                BrExLabel label = labels[i];

                LabelSpecificMenuOperations labelSpecificOperations =
                    new LabelSpecificMenuOperations(
                        mLabelOperations, () => { mSubmenuLabel = label; });

                LabelsViewMenu labelsViewMenu = LabelsViewMenu.BuildForSubMenu(
                    labelSpecificOperations,
                    escapedLabelName);
                labelsViewMenu.UpdateMenuItems(menu);
            }

            menu.ShowAsContext();
        }

        static bool NeedsSeparator(
            int position,
            int labelCount,
            int maxLabelsForSeparator)
        {
            if (labelCount == maxLabelsForSeparator)
                return false;

            if (position >= labelCount || position == 0)
                return false;

            return position % maxLabelsForSeparator == 0;
        }

        MarkerExtendedInfo BranchExplorerViewLabelMenuOperations.ISelectionResolver.ResolveSelectedLabel()
        {
            LabelDrawInfo currentLabelDrawInfo = GetSelectedLabel(mSelectionHandler);

            return BranchExplorerObjectResolver.GetMarkerExtendedInfo(
                currentLabelDrawInfo,
                PlasticGui.Plastic.API.GetMarkerInfo(mRepSpec, GetCurrentLabel(currentLabelDrawInfo).Id));
        }

        MarkerExtendedInfo BranchExplorerViewLabelMenuOperations.ISelectionResolver.ResolveLabel(
            long labelId, LabelDrawInfo labelInfo)
        {
            return BranchExplorerObjectResolver.GetMarkerExtendedInfo(
                labelInfo, PlasticGui.Plastic.API.GetMarkerInfo(mRepSpec, labelId));
        }

        /*
        List<RepObjectInfo> BranchExplorerViewExternalToolsMenuOperations.ISelectionResolver.ResolveSelectedObjects()
        {
            return new List<RepObjectInfo>()
            {
                ((BranchExplorerViewLabelMenuOperations.ISelectionResolver)this)
                    .ResolveSelectedLabel()
            };
        }
        */

        BrExLabel GetCurrentLabel(LabelDrawInfo currentLabelDrawInfo)
        {
            return mSubmenuLabel ?? currentLabelDrawInfo.Labels[0];
        }

        static LabelDrawInfo GetSelectedLabel(BranchExplorerSelection selectionHandler)
        {
            return selectionHandler.GetSelectedLabels().
                Aggregate((l1, l2) => l1.Labels.Count() > l2.Labels.Count() ? l1 : l2);
        }

        static string EscapeLabelName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            return name.Replace("_", "__");
        }

        BrExLabel mSubmenuLabel;

        RepositorySpec mRepSpec;
        readonly BranchExplorerSelection mSelectionHandler;
        readonly BranchExplorerViewLabelMenuOperations mLabelOperations;
        //readonly BranchExplorerViewExternalToolsMenuOperations mExternalToolsMenuOperations;
        readonly LabelsViewMenu mLabelsViewMenu;
    }

    internal class LabelSpecificMenuOperations : PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations
    {
        internal LabelSpecificMenuOperations(
            BranchExplorerViewLabelMenuOperations labelOperations,
            Action selectSpecificLabel)
        {
            mLabelOperations = labelOperations;
            mSelectSpecificLabel = selectSpecificLabel;
        }

        int PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.GetSelectedLabelsCount()
        {
            mSelectSpecificLabel();
            return mLabelOperations.GetSelectedLabelsCount();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.CreateLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.CreateLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.ApplyLabelToWorkspace()
        {
            mSelectSpecificLabel();
            mLabelOperations.ApplyLabelToWorkspace();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.SwitchToLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.SwitchToLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.DiffWithAnotherLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.DiffWithAnotherLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.DiffSelectedLabels()
        {
            mSelectSpecificLabel();
            mLabelOperations.DiffSelectedLabels();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.MergeLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.MergeLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.MergeToLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.MergeToLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.CreateBranchFromLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.CreateBranchFromLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.RenameLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.RenameLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.DeleteLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.DeleteLabel();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.ViewPermissions()
        {
            mSelectSpecificLabel();
            mLabelOperations.ViewPermissions();
        }

        void PlasticGui.WorkspaceWindow.QueryViews.Labels.ILabelMenuOperations.BrowseRepositoryOnLabel()
        {
            mSelectSpecificLabel();
            mLabelOperations.ViewPermissions();
        }

        readonly Action mSelectSpecificLabel;
        readonly BranchExplorerViewLabelMenuOperations mLabelOperations;
    }
}
