using System;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

using Codice.Client.Common.Threading;
using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;

using Object = UnityEngine.Object;

namespace Unity.PlasticSCM.Editor.AssetsOverlays
{
    internal static class DrawHierarchyOverlay
    {
        internal interface IGetAssetPathFromInstance
        {
            bool TryGetAssetPath(UnityObjectInstance instance, out string assetPath);
        }

        internal static IGetAssetPathFromInstance GetSubSceneAssetPath;

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

#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUIByEntityId;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
#endif

            RepaintEditor.HierarchyWindow();
        }

        internal static void Disable()
        {
            mLog.Debug("Disable");

            mIsEnabled = false;

#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyGUIByEntityId;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
#endif

            RepaintEditor.HierarchyWindow();

            mWkPath = null;
            mAssetStatusCache = null;
        }

#if UNITY_6000_4_OR_NEWER
        static void OnHierarchyGUIByEntityId(EntityId entityId, Rect selectionRect)
        {
            OnHierarchyGUI(UnityObjectInstance.FromEntityId(entityId), selectionRect);
        }
#else
        static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            OnHierarchyGUI(UnityObjectInstance.FromInstanceId(instanceId), selectionRect);
        }
#endif

        static void OnHierarchyGUI(UnityObjectInstance instance, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            try
            {
                string assetPath = GetAssetPathFromInstance(instance);

                if (assetPath == null)
                    return;

                string assetFullPath = AssetsPath.GetFullPathUnderWorkspace.ForAsset(mWkPath, assetPath);

                if (assetFullPath == null)
                    return;

                DrawOverlayForAsset(assetFullPath, selectionRect, mAssetStatusCache);
            }
            catch (Exception ex)
            {
                ExceptionsHandler.LogException(typeof(DrawHierarchyOverlay).Name, ex);
            }
        }

        static string GetAssetPathFromInstance(UnityObjectInstance instance)
        {
            string sceneAssetPath;
            if (TryGetAssetPathForScene(instance, out sceneAssetPath))
            {
                return sceneAssetPath;
            }

            string subSceneAssetPath;
            if (TryGetAssetPathForSubScene(instance, out subSceneAssetPath))
            {
                return subSceneAssetPath;
            }

            string prefabAssetPath;
            if (TryGetAssetPathForPrefab(instance, out prefabAssetPath))
            {
                return prefabAssetPath;
            }

            return null;
        }

        static bool TryGetAssetPathForScene(UnityObjectInstance instance, out string assetPath)
        {
            assetPath = null;

            if (instance.FindObject() != null)
                return false;

            assetPath = FindScenePathForHandle(instance);

            return assetPath != null;
        }

        static string FindScenePathForHandle(UnityObjectInstance instance)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (instance.MatchesScene(scene) && scene.path != null)
                {
                    return scene.path;
                }
            }

            return null;
        }

        static bool TryGetAssetPathForSubScene(UnityObjectInstance instance, out string assetPath)
        {
            assetPath = null;

            string subSceneAssetPath;
            if (GetSubSceneAssetPath != null &&
                GetSubSceneAssetPath.TryGetAssetPath(instance, out subSceneAssetPath))
            {
                assetPath = subSceneAssetPath;
            }

            return assetPath != null;
        }

        static bool TryGetAssetPathForPrefab(UnityObjectInstance instance, out string assetPath)
        {
            assetPath = null;

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage == null)
                return false;

            Object hierarchyObject = instance.FindObject();

            if (hierarchyObject == null)
                return false;

            if (prefabStage.prefabContentsRoot == hierarchyObject)
                assetPath = prefabStage.assetPath;

            return assetPath != null;
        }

        static void DrawOverlayForAsset(
            string assetFullPath,
            Rect selectionRect,
            IAssetStatusCache assetStatusCache)
        {
            AssetStatus assetStatus = assetStatusCache.GetStatus(assetFullPath);

            string tooltipText = AssetOverlay.GetTooltipText(
                assetStatus,
                assetStatusCache.GetLockStatusData(assetFullPath));

            DrawAssetOverlayIcon.ForStatus(
                selectionRect,
                assetStatus,
                tooltipText);
        }

        static bool mIsEnabled;
        static IAssetStatusCache mAssetStatusCache;
        static string mWkPath;

        static readonly ILog mLog = PlasticApp.GetLogger("DrawHierarchyOverlay");
    }
}
