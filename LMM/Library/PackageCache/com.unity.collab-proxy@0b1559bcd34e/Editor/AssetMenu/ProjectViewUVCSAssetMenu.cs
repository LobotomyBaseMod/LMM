using UnityEditor.VersionControl;

using Codice.CM.Common;
using Codice.Client.Common.EventTracking;
using Codice.LogWrapper;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Tool;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal static class ProjectViewUVCSAssetMenu
    {
        internal static void Enable(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            IAssetStatusCache assetStatusCache)
        {
            if (mIsEnabled)
                return;

            mLog.Debug("Enable");

            mWkInfo = wkInfo;
            mPlasticAPI = plasticApi;
            mAssetStatusCache = assetStatusCache;

            mIsEnabled = true;

            mAssetSelection = new ProjectViewAssetSelection(UpdateFilterMenuItems);

            mAssetMenuCopyPathOperation = new AssetCopyPathOperation(
                wkInfo, mPlasticAPI, assetStatusCache, mAssetSelection);

            mFilterMenuBuilder = new AssetFilesFilterPatternsMenuBuilder(
                IGNORE_MENU_ITEMS_PRIORITY,
                HIDDEN_MENU_ITEMS_PRIORITY);

            AddMenuItems();
        }

        internal static void Disable()
        {
            mLog.Debug("Disable");

            mIsEnabled = false;

            RemoveMenuItems();

            if (mAssetSelection != null)
                mAssetSelection.Dispose();

            mWkInfo = null;
            mAssetStatusCache = null;
            mAssetSelection = null;
            mFilterMenuBuilder = null;
            mAssetMenuUVCSOperations = null;
            mAssetMenuCopyPathOperation = null;
        }

        internal static void BuildOperations(
            WorkspaceInfo wkInfo,
            IPlasticAPI plasticApi,
            GluonGui.ViewHost viewHost,
            WorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            PlasticGui.Gluon.IGluonViewSwitcher gluonViewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            IHistoryViewLauncher historyViewLauncher,
            IAssetStatusCache assetStatusCache,
            ISaveAssets saveAssets,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            PendingChangesUpdater pendingChangesUpdater,
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater,
            ShelvedChangesUpdater shelvedChangesUpdater,
            bool isGluonMode)
        {
            if (!mIsEnabled)
                Enable(wkInfo, plasticApi, assetStatusCache);

            AssetUVCSOperations assetUVCSOperations = new AssetUVCSOperations(
                wkInfo,
                plasticApi,
                viewHost,
                workspaceWindow,
                viewSwitcher,
                gluonViewSwitcher,
                mergeViewLauncher,
                historyViewLauncher,
                assetStatusCache,
                mAssetSelection,
                saveAssets,
                showDownloadPlasticExeWindow,
                workspaceOperationsMonitor,
                pendingChangesUpdater,
                developerIncomingChangesUpdater,
                gluonIncomingChangesUpdater,
                shelvedChangesUpdater,
                isGluonMode);

            mAssetMenuUVCSOperations = assetUVCSOperations;
            mFilterMenuBuilder.SetOperations(assetUVCSOperations);
        }

        static void RemoveMenuItems()
        {
            mFilterMenuBuilder.RemoveMenuItems();

            HandleMenuItem.RemoveMenuItem(
                PlasticLocalization.GetString(PlasticLocalization.Name.PrefixUnityVersionControlMenu));

            HandleMenuItem.UpdateAllMenus();
        }

        static void UpdateFilterMenuItems()
        {
            AssetList assetList = ((AssetUVCSOperations.IAssetSelection)
                mAssetSelection).GetSelectedAssets();

            SelectedPathsGroupInfo info = AssetsSelection.GetSelectedPathsGroupInfo(
                mWkInfo.ClientPath, assetList, mAssetStatusCache);

            FilterMenuActions actions =
                assetList.Count != info.SelectedCount ?
                new FilterMenuActions() :
                FilterMenuUpdater.GetMenuActions(info);

            mFilterMenuBuilder.UpdateMenuItems(actions);
        }

        static void AddMenuItems()
        {
            // TODO: Try removing this
            // Somehow first item always disappears. So this is a filler item
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.PendingChangesPlasticMenu),
                PENDING_CHANGES_MENU_ITEM_PRIORITY,
                PendingChanges, ValidatePendingChanges);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.PendingChangesPlasticMenu),
                PENDING_CHANGES_MENU_ITEM_PRIORITY,
                PendingChanges, ValidatePendingChanges);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.AddPlasticMenu),
                ADD_MENU_ITEM_PRIORITY,
                Add, ValidateAdd);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.CheckoutPlasticMenu),
                CHECKOUT_MENU_ITEM_PRIORITY,
                Checkout, ValidateCheckout);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.CheckinPlasticMenu),
                CHECKIN_MENU_ITEM_PRIORITY,
                Checkin, ValidateCheckin);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.UndoPlasticMenu),
                UNDO_MENU_ITEM_PRIORITY,
                Undo, ValidateUndo);

            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.CopyFilePathMenuItem),
                COPY_FILE_PATH_MENU_ITEM_PRIORITY,
                CopyFilePath,
                ValidateCopyFilePath);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.CopyRelativeFilePathMenuItem),
                COPY_RELATIVE_FILE_PATH_MENU_ITEM_PRIORITY,
                CopyRelativeFilePath,
                ValidateCopyFilePath);

            UpdateFilterMenuItems();

            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.DiffPlasticMenu),
                GetPlasticShortcut.ForAssetDiff(),
                DIFF_MENU_ITEM_PRIORITY,
                Diff, ValidateDiff);
            HandleMenuItem.AddMenuItem(
                GetPlasticMenuItemName(PlasticLocalization.Name.HistoryPlasticMenu),
                GetPlasticShortcut.ForHistory(),
                HISTORY_MENU_ITEM_PRIORITY,
                History, ValidateHistory);

            HandleMenuItem.UpdateAllMenus();
        }

        static void PendingChanges()
        {
            ShowWindow.UVCS();

            mAssetMenuUVCSOperations.ShowPendingChanges();
        }

        static bool ValidatePendingChanges()
        {
            return true;
        }

        static void Add()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Add();
        }

        static bool ValidateAdd()
        {
            return ValidateOperation(AssetMenuOperations.Add);
        }

        static void Checkout()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Checkout();
        }

        static bool ValidateCheckout()
        {
            return ValidateOperation(AssetMenuOperations.Checkout);
        }

        static void Checkin()
        {
            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.ProjectContextMenuCheckinOption);

            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Checkin();
        }

        static bool ValidateCheckin()
        {
            return ValidateOperation(AssetMenuOperations.Checkin);
        }

        static void Undo()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Undo();
        }

        static bool ValidateUndo()
        {
            return ValidateOperation(AssetMenuOperations.Undo);
        }

        static void CopyFilePath()
        {
            mAssetMenuCopyPathOperation.CopyFilePath(relativePath: false);
        }

        static void CopyRelativeFilePath()
        {
            mAssetMenuCopyPathOperation.CopyFilePath(relativePath: true);
        }

        static bool ValidateCopyFilePath()
        {
            return ValidateOperation(AssetMenuOperations.CopyFilePath);
        }

        static void Diff()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.ShowDiff();
        }

        static bool ValidateDiff()
        {
            return ValidateOperation(AssetMenuOperations.Diff);
        }

        static void History()
        {
            ShowWindow.UVCS();

            mAssetMenuUVCSOperations.ShowHistory();
        }

        static bool ValidateHistory()
        {
            return ValidateOperation(AssetMenuOperations.History);
        }

        static bool ValidateOperation(AssetMenuOperations operation)
        {
            return AssetMenuUpdater.ShouldMenuOperationBeEnabled(
                mWkInfo, mPlasticAPI, mAssetSelection, mAssetStatusCache, operation);
        }

        static string GetPlasticMenuItemName(PlasticLocalization.Name name)
        {
            return string.Format("{0}/{1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.PrefixUnityVersionControlMenu),
                PlasticLocalization.GetString(name));
        }

        static IAssetMenuUVCSOperations mAssetMenuUVCSOperations;
        static IAssetMenuCopyPathOperation mAssetMenuCopyPathOperation;

        static ProjectViewAssetSelection mAssetSelection;
        static AssetFilesFilterPatternsMenuBuilder mFilterMenuBuilder;

        static bool mIsEnabled;
        static IAssetStatusCache mAssetStatusCache;
        static IPlasticAPI mPlasticAPI;
        static WorkspaceInfo mWkInfo;

#if UNITY_6000_0_OR_NEWER
        // Puts Unity Version Control in a new section, as it precedes the Create menu with the old value
        internal const int BASE_MENU_ITEM_PRIORITY = 71;
#else
        // Puts Unity Version Control right below the Create menu
        internal const int BASE_MENU_ITEM_PRIORITY = 19;
#endif

        // incrementing the "order" param by 11 causes the menu system to add a separator
        const int PENDING_CHANGES_MENU_ITEM_PRIORITY = BASE_MENU_ITEM_PRIORITY;
        const int ADD_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 11;
        const int CHECKOUT_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 12;
        const int CHECKIN_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 13;
        const int UNDO_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 14;
        const int COPY_FILE_PATH_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 25;
        const int COPY_RELATIVE_FILE_PATH_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 26;
        const int IGNORE_MENU_ITEMS_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 37;
        const int HIDDEN_MENU_ITEMS_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 38;
        const int DIFF_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 49;
        const int HISTORY_MENU_ITEM_PRIORITY = PENDING_CHANGES_MENU_ITEM_PRIORITY + 50;

        static readonly ILog mLog = PlasticApp.GetLogger("ProjectViewAssetMenu");
    }
}
