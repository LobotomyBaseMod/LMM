using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;

using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal static class HierarchyViewAssetMenu
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

            mAssetSelection = new HierarchyViewAssetSelection();

            mAssetMenuCopyPathOperation = new AssetCopyPathOperation(
                wkInfo, mPlasticAPI, assetStatusCache, mAssetSelection);

            SceneHierarchyHooks.addItemsToSceneHeaderContextMenu += OnSceneHeaderContextMenu;
            SceneHierarchyHooks.addItemsToSubSceneHeaderContextMenu += OnSubSceneHeaderContextMenu;
            SceneHierarchyHooks.addItemsToGameObjectContextMenu += OnGameObjectContextMenu;
        }

        internal static void Disable()
        {
            mLog.Debug("Enable");

            mIsEnabled = false;

            mWkInfo = null;
            mPlasticAPI = null;
            mAssetStatusCache = null;
            mAssetSelection = null;
            mAssetMenuUVCSOperations = null;
            mAssetMenuCopyPathOperation = null;

            SceneHierarchyHooks.addItemsToSceneHeaderContextMenu -= OnSceneHeaderContextMenu;
            SceneHierarchyHooks.addItemsToSubSceneHeaderContextMenu -= OnSubSceneHeaderContextMenu;
            SceneHierarchyHooks.addItemsToGameObjectContextMenu -= OnGameObjectContextMenu;
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
        }

        static void OnSceneHeaderContextMenu(GenericMenu menu, Scene scene)
        {
            LoadMenuForAssetPath(menu, scene.path);
        }

        static void OnSubSceneHeaderContextMenu(GenericMenu menu, SceneHierarchyHooks.SubSceneInfo subSceneInfo)
        {
            LoadMenuForAssetPath(menu, subSceneInfo.scene.path);
        }

        static void OnGameObjectContextMenu(GenericMenu menu, GameObject gameObject)
        {
            string prefabAssetPath;
            if (TryGetAssetPathForPrefab(gameObject, out prefabAssetPath))
            {
                LoadMenuForAssetPath(menu, prefabAssetPath);
            }
        }

        static bool TryGetAssetPathForPrefab(GameObject gameObject, out string assetPath)
        {
            assetPath = null;

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null && prefabStage.prefabContentsRoot == gameObject)
            {
                assetPath = prefabStage.assetPath;
            }

            return assetPath != null;
        }

        static void LoadMenuForAssetPath(GenericMenu menu, string assetPath)
        {
            string assetFullPath = AssetsPath.GetFullPathUnderWorkspace.ForAsset(mWkInfo.ClientPath, assetPath);

            if (assetFullPath == null)
                return;

            mAssetSelection.SetActiveAsset(new Asset(assetFullPath));

            AddItemsToMenu(menu);
        }

        static void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddSeparator(string.Empty);

            menu.AddItem(
                GetPlasticMenuItemContent(PlasticLocalization.Name.PendingChangesPlasticMenu),
                false,
                PendingChanges);

            menu.AddSeparator(GetSubMenuName());

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.AddPlasticMenu),
                Add,
                AssetMenuOperations.Add
            );

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.CheckoutPlasticMenu),
                Checkout,
                AssetMenuOperations.Checkout
            );

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.CheckinPlasticMenu),
                Checkin,
                AssetMenuOperations.Checkin
            );

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.UndoPlasticMenu),
                Undo,
                AssetMenuOperations.Undo
            );

            menu.AddSeparator(GetSubMenuName());

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.CopyFilePathMenuItem),
                CopyFilePath,
                AssetMenuOperations.CopyFilePath
            );

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.CopyRelativeFilePathMenuItem),
                CopyRelativeFilePath,
                AssetMenuOperations.CopyFilePath
            );

            menu.AddSeparator(GetSubMenuName());

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.DiffPlasticMenu),
                Diff,
                AssetMenuOperations.Diff
            );

            AddMenuItemForConditionalOperation(
                menu,
                GetPlasticMenuItemContent(PlasticLocalization.Name.HistoryPlasticMenu),
                History,
                AssetMenuOperations.History
            );
        }

        static void AddMenuItemForConditionalOperation(
            GenericMenu menu,
            GUIContent menuContent,
            GenericMenu.MenuFunction menuAction,
            AssetMenuOperations operation)
        {
            if (ValidateOperation(operation))
            {
                menu.AddItem(menuContent, false, menuAction);
            }
            else
            {
                menu.AddDisabledItem(menuContent);
            }
        }

        static void PendingChanges()
        {
            ShowWindow.UVCS();
            mAssetMenuUVCSOperations.ShowPendingChanges();
        }

        static void Add()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Add();
        }

        static void Checkout()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Checkout();
        }

        static void Checkin()
        {
            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                TrackFeatureUseEvent.Features.UnityPackage.HierarchyContextMenuCheckinOption);

            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Checkin();
        }

        static void Undo()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.Undo();
        }

        static void CopyFilePath()
        {
            mAssetMenuCopyPathOperation.CopyFilePath(relativePath: false);
        }

        static void CopyRelativeFilePath()
        {
            mAssetMenuCopyPathOperation.CopyFilePath(relativePath: true);
        }

        static void Diff()
        {
            if (mAssetMenuUVCSOperations == null)
                ShowWindow.UVCS();

            mAssetMenuUVCSOperations.ShowDiff();
        }

        static void History()
        {
            ShowWindow.UVCS();
            mAssetMenuUVCSOperations.ShowHistory();
        }

        static bool ValidateOperation(AssetMenuOperations operation)
        {
            return AssetMenuUpdater.ShouldMenuOperationBeEnabled(
                mWkInfo, mPlasticAPI, mAssetSelection, mAssetStatusCache, operation);
        }

        static GUIContent GetPlasticMenuItemContent(PlasticLocalization.Name name)
        {
            string menuText = string.Format("{0}{1}",
                GetSubMenuName(),
                name.GetString());

            return EditorGUIUtility.TrTextContent(menuText);
        }

        static string GetSubMenuName()
        {
            return PlasticLocalization.Name.UnityVersionControl.GetString() + "/";
        }

        static bool mIsEnabled;
        static WorkspaceInfo mWkInfo;
        static IPlasticAPI mPlasticAPI;
        static IAssetStatusCache mAssetStatusCache;
        static HierarchyViewAssetSelection mAssetSelection;
        static IAssetMenuUVCSOperations mAssetMenuUVCSOperations;
        static IAssetMenuCopyPathOperation mAssetMenuCopyPathOperation;

        static readonly ILog mLog = PlasticApp.GetLogger("HierarchyViewMenu");
    }
}
