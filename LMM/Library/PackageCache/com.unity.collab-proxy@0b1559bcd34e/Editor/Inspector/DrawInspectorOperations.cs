using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;

using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.LogWrapper;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.AssetMenu;
using Unity.PlasticSCM.Editor.AssetsOverlays;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Tool;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;

namespace Unity.PlasticSCM.Editor.Inspector
{
    static class DrawInspectorOperations
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

            mAssetSelection = new InspectorAssetSelection();

            UnityEditor.Editor.finishedDefaultHeaderGUI +=
                Editor_finishedDefaultHeaderGUI;

            RepaintEditor.InspectorWindow();
        }

        internal static void Disable()
        {
            mLog.Debug("Disable");

            mIsEnabled = false;

            UnityEditor.Editor.finishedDefaultHeaderGUI -=
                Editor_finishedDefaultHeaderGUI;

            RepaintEditor.InspectorWindow();

            mWkInfo = null;
            mAssetStatusCache = null;
            mAssetSelection = null;
            mOperations = null;
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

            mOperations = new AssetUVCSOperations(
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
        }

        static void Editor_finishedDefaultHeaderGUI(UnityEditor.Editor inspector)
        {
            try
            {
                if (!mIsEnabled)
                    return;

                mAssetSelection.SetActiveInspector(inspector);

                AssetList assetList = ((AssetUVCSOperations.IAssetSelection)
                    mAssetSelection).GetSelectedAssets();

                if (assetList.Count == 0)
                    return;

                SelectedAssetGroupInfo selectedGroupInfo = SelectedAssetGroupInfo.
                    BuildFromAssetList(mWkInfo, assetList, mPlasticAPI, mAssetStatusCache);

                if (assetList.Count != selectedGroupInfo.SelectedCount)
                    return;

                AssetsOverlays.AssetStatus assetStatus;
                LockStatusData lockStatusData;
                GetAssetStatusToDraw(
                    assetList[0].path, assetList.Count,
                    mAssetStatusCache,
                    out assetStatus,
                    out lockStatusData);

                AssetMenuOperations assetOperations = AssetMenuUpdater.
                    GetAvailableMenuOperations(selectedGroupInfo);

                DrawAssetStatusHeader(assetStatus, lockStatusData, assetOperations);
            }
            catch (Exception ex)
            {
                ExceptionsHandler.LogException(typeof(DrawInspectorOperations).Name, ex);
            }
        }

        static void DrawAssetStatusHeader(
            AssetsOverlays.AssetStatus assetStatus,
            LockStatusData lockStatusData,
            AssetMenuOperations assetOperations)
        {
            bool guiEnabledBck = GUI.enabled;
            GUI.enabled = true;
            try
            {
                DrawBackRectangle(guiEnabledBck);

                GUILayout.BeginHorizontal();

                DrawStatusLabel(assetStatus, lockStatusData);

                DrawButtons(assetOperations);

                GUILayout.EndHorizontal();
            }
            finally
            {
                GUI.enabled = guiEnabledBck;
            }
        }

        static void DrawBackRectangle(bool isEnabled)
        {
            // when the inspector is disabled, there is a separator line
            // that breaks the visual style. Draw an empty rectangle
            // matching the background color to cover it

            GUILayout.Space(UnityConstants.INSPECTOR_ACTIONS_BACK_RECTANGLE_TOP_MARGIN);

            GUIStyle targetStyle = (isEnabled) ?
                UnityStyles.Inspector.HeaderBackgroundUnderlineStyle :
                UnityStyles.Inspector.DisabledHeaderBackgroundUnderlineStyle;

            Rect rect = GUILayoutUtility.GetRect(
                GUIContent.none, targetStyle);

            // extra space to cover the inspector full width
            rect.x -= 20;
            rect.width += 80;

            GUI.Box(rect, GUIContent.none, targetStyle);

            // now reset the space used by the rectangle
            GUILayout.Space(
                -UnityConstants.INSPECTOR_ACTIONS_HEADER_BACK_RECTANGLE_HEIGHT
                - UnityConstants.INSPECTOR_ACTIONS_BACK_RECTANGLE_TOP_MARGIN);
        }

        static void DrawButtons(AssetMenuOperations assetOperations)
        {
            var operationsAvailability = new Dictionary<AssetMenuOperations, bool>
            {
                { AssetMenuOperations.Add, assetOperations.HasFlag(AssetMenuOperations.Add) },
                { AssetMenuOperations.Checkout, assetOperations.HasFlag(AssetMenuOperations.Checkout) },
                { AssetMenuOperations.Checkin, assetOperations.HasFlag(AssetMenuOperations.Checkin) },
                { AssetMenuOperations.Undo, assetOperations.HasFlag(AssetMenuOperations.Undo) }
            };

            // GUILayout reserves space for controls, which might lead to unexpected layout behavior
            // when controls are hidden dynamically. We keep consistency by adding flex spaces for inactive operations.
            foreach (var unused in operationsAvailability.Values.Where(activeOperation => !activeOperation))
            {
                GUILayout.FlexibleSpace();
            }

            if (operationsAvailability[AssetMenuOperations.Add])
            {
                DoAddButton();
            }

            if (operationsAvailability[AssetMenuOperations.Checkout])
            {
                DoCheckoutButton();
            }

            if (operationsAvailability[AssetMenuOperations.Checkin])
            {
                DoCheckinButton();
            }

            if (operationsAvailability[AssetMenuOperations.Undo])
            {
                DoUndoButton();
            }
        }

        static void DrawStatusLabel(
            AssetsOverlays.AssetStatus assetStatus,
            LockStatusData lockStatusData)
        {
            string statusText = AssetOverlay.GetStatusString(assetStatus);
            string tooltipText = AssetOverlay.GetTooltipText(assetStatus, lockStatusData);

            Texture statusIcon = DrawAssetOverlayIcon.GetOverlayIcon(assetStatus);

            Rect selectionRect = GUILayoutUtility.GetRect(
                new GUIContent(statusText + EXTRA_SPACE, statusIcon),
                GUIStyle.none);

            selectionRect.height = UnityConstants.OVERLAY_STATUS_ICON_SIZE;

            if (statusIcon == null)
            {
                int labelMarginWithoutIcon = 5;
                selectionRect.x += labelMarginWithoutIcon;
                GUI.Label(
                    selectionRect,
                    new GUIContent(statusText, tooltipText));
                return;
            }

            Rect statusIconRect = new Rect(
                selectionRect.x + 3f,
                selectionRect.y - 1f,
                UnityConstants.INSPECTOR_STATUS_ICON_SIZE,
                UnityConstants.INSPECTOR_STATUS_ICON_SIZE);

            GUI.DrawTexture(
                statusIconRect,
                statusIcon,
                ScaleMode.ScaleToFit);

            int margin = 2;

            selectionRect.x += UnityConstants.INSPECTOR_STATUS_ICON_SIZE + margin;
            selectionRect.width -= UnityConstants.INSPECTOR_STATUS_ICON_SIZE;

            GUI.Label(
                selectionRect,
                new GUIContent(statusText, tooltipText));
        }

        static void DoAddButton()
        {
            string buttonText = PlasticLocalization.GetString(PlasticLocalization.Name.AddButton);
            if (GUILayout.Button(string.Format("{0}", buttonText), EditorStyles.miniButton))
            {
                if (mOperations == null)
                    ShowWindow.UVCS();

                mOperations.Add();
            }
        }

        static void DoCheckoutButton()
        {
            string buttonText = PlasticLocalization.GetString(PlasticLocalization.Name.CheckoutButton);
            if (GUILayout.Button(string.Format("{0}", buttonText), EditorStyles.miniButton))
            {
                if (mOperations == null)
                    ShowWindow.UVCS();

                mOperations.Checkout();
            }
        }

        static void DoCheckinButton()
        {
            string buttonText = PlasticLocalization.GetString(PlasticLocalization.Name.CheckinButton);
            if (GUILayout.Button(string.Format("{0}", buttonText), EditorStyles.miniButton))
            {
                if (mOperations == null)
                    ShowWindow.UVCS();

                mOperations.Checkin();
                EditorGUIUtility.ExitGUI();
            }
        }

        static void DoUndoButton()
        {
            string buttonText = PlasticLocalization.GetString(PlasticLocalization.Name.UndoButton);
            if (GUILayout.Button(string.Format("{0}", buttonText), EditorStyles.miniButton))
            {
                if (mOperations == null)
                    ShowWindow.UVCS();

                mOperations.Undo();
                EditorGUIUtility.ExitGUI();
            }
        }

        static void GetAssetStatusToDraw(
            string selectedPath,
            int selectedCount,
            IAssetStatusCache statusCache,
            out AssetsOverlays.AssetStatus assetStatus,
            out LockStatusData lockStatusData)
        {
            assetStatus = AssetsOverlays.AssetStatus.None;
            lockStatusData = null;

            if (selectedCount > 1)
                return;

            string selectedFullPath = AssetsPath.GetFullPath.ForPath(selectedPath);

            assetStatus = statusCache.GetStatus(selectedFullPath);
            lockStatusData = statusCache.GetLockStatusData(selectedFullPath);
        }

        static IAssetMenuUVCSOperations mOperations;
        static InspectorAssetSelection mAssetSelection;

        static bool mIsEnabled;
        static IAssetStatusCache mAssetStatusCache;
        static IPlasticAPI mPlasticAPI;
        static WorkspaceInfo mWkInfo;

        const string EXTRA_SPACE = "    ";

        static readonly ILog mLog = PlasticApp.GetLogger("DrawInspectorOperations");
    }
}
