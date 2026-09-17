using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.Common.Threading;
using Codice.LogWrapper;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;

namespace Unity.PlasticSCM.Editor.AssetsOverlays
{
    internal static class DrawProjectOverlay
    {
        internal static void Enable(
            string wkPath,
            IAssetStatusCache assetStatusCache)
        {
            if (mIsEnabled)
                return;

            mLog.Debug("Enable");

            mWkPath = wkPath;
            mAssetStatusCache = assetStatusCache;

            mIsEnabled = true;

            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

            RepaintEditor.ProjectWindow();
        }

        internal static void Disable()
        {
            mLog.Debug("Disable");

            mIsEnabled = false;

            EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemGUI;

            RepaintEditor.ProjectWindow();

            mWkPath = null;
            mAssetStatusCache = null;
        }

        static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            try
            {
                if (string.IsNullOrEmpty(guid))
                    return;

                if (Event.current.type != EventType.Repaint)
                    return;

                string fullPath = AssetsPath.GetFullPathUnderWorkspace.ForGuid(mWkPath, guid);

                if (fullPath == null)
                    return;

                AssetStatus assetStatus = mAssetStatusCache.GetStatus(fullPath);

                string tooltipText = AssetOverlay.GetTooltipText(
                    assetStatus,
                    mAssetStatusCache.GetLockStatusData(fullPath));

                DrawAssetOverlayIcon.ForStatus(
                    selectionRect,
                    assetStatus,
                    tooltipText);
            }
            catch (Exception ex)
            {
                ExceptionsHandler.LogException(typeof(DrawProjectOverlay).Name, ex);
            }
        }

        static bool mIsEnabled;
        static IAssetStatusCache mAssetStatusCache;
        static string mWkPath;

        static readonly ILog mLog = PlasticApp.GetLogger("DrawProjectOverlay");
    }
}
