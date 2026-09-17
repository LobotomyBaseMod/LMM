using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using UnityInternals = Unity.PlasticSCM.Editor.UnityInternals;

namespace Unity.Cloud.Collaborate
{
    [InitializeOnLoad]
    static class UnityInternalsInjector
    {
        static UnityInternalsInjector()
        {
            UnityInternals.UnityEditor.EditorGUI.ScrollableTextAreaInternal =
                (Rect position, string text, ref Vector2 scrollPosition, GUIStyle style) =>
                {
                    return EditorGUI.ScrollableTextAreaInternal(position, text, ref scrollPosition, style);
                };

            UnityInternals.UnityEditor.InternalEditorGUIUtility.HasCurrentWindowKeyFocus =
                () =>
                {
                    return EditorGUIUtility.HasCurrentWindowKeyFocus();
                };

            UnityInternals.UnityEditor.EditorGUI.DrawOutline =
                (Rect rect, float size, Color color) =>
                {
                    EditorGUI.DrawOutline(rect, size, color);
                };

            UnityInternals.UnityEditor.EditorGUI.RecycledTextEditor.InternalEndEditing =
                (UnityInternals.UnityEditor.EditorGUI.RecycledTextEditor recycledTextEditor) =>
                {
                    ((EditorGUI.RecycledTextEditor)recycledTextEditor.InternalObject).EndEditing();
                };

            UnityInternals.UnityEditor.SettingsWindow.ShowInternal =
                (SettingsScope scopes, string settingsPath) =>
                {
                    SettingsWindow settingsWindow = SettingsWindow.Show(scopes, settingsPath);

                    if (settingsWindow == null)
                        return null;

                    return new UnityInternals.UnityEditor.SettingsWindow(settingsWindow);
                };

            UnityInternals.UnityEditor.SettingsWindow.GetCurrentProviderInternal =
                (UnityInternals.UnityEditor.SettingsWindow settingsWindow) =>
                {
                    return ((SettingsWindow)settingsWindow.InternalObject).GetCurrentProvider();
                };

            UnityInternals.UnityEditor.DockArea.AddTabInternal =
                (UnityInternals.UnityEditor.DockArea dockArea, EditorWindow pane, bool sendPaneEvents) =>
                {
                    ((DockArea)dockArea.InternalObject).AddTab(pane, sendPaneEvents);
                };

            UnityInternals.UnityEditor.UnityEditorExtensions.m_ParentInternal =
                (EditorWindow editorWindow) =>
                {
                    HostView hostView = editorWindow.m_Parent;

                    if (hostView == null)
                        return null;

                    return new UnityInternals.UnityEditor.HostView(hostView);
                };

            UnityInternals.UnityEditor.HostView.GetActualViewInternal =
                (UnityInternals.UnityEditor.HostView hostView) =>
                {
                    FieldInfo actualViewField = typeof(HostView).GetField(
                        "m_ActualView",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    return actualViewField?.GetValue(hostView.InternalObject) as EditorWindow;
                };

            UnityInternals.UnityEditor.Menu.AddMenuItem =
                (string name, string shortcut, bool @checked, int priority, Action execute, Func<bool> validate) =>
                {
                    Menu.AddMenuItem(name, shortcut, @checked, priority, execute, validate);
                };

            UnityInternals.UnityEditor.Menu.RemoveMenuItem =
                (string name) =>
                {
                    Menu.RemoveMenuItem(name);
                };

            UnityInternals.UnityEditor.EditorUtility.Internal_UpdateAllMenus =
                () =>
                {
                    EditorUtility.Internal_UpdateAllMenus();
                };

            UnityInternals.UnityEditor.HostView.GetWindow =
                (UnityInternals.UnityEditor.HostView hostView) =>
                {
                    ContainerWindow containerWindow = ((HostView)hostView.InternalObject).window;

                    if (containerWindow == null)
                        return null;

                    return new UnityInternals.UnityEditor.ContainerWindow(containerWindow);
                };

            UnityInternals.UnityEditor.EditorWindow.Internal_MakeModal =
                (UnityInternals.UnityEditor.ContainerWindow containerWindow) =>
                {
                    EditorWindow.Internal_MakeModal((ContainerWindow)containerWindow.InternalObject);
                };

            UnityInternals.UnityEditor.EditorWindow.GetInspectorWindow =
                () =>
                {
                    return EditorWindow.GetWindow<UnityEditor.InspectorWindow>(
                        title: null, focus: false);
                };

            UnityInternals.UnityEditor.UnityEditorExtensions.ShowWithModeInternal =
                (EditorWindow editorWindow, int mode) =>
                {
                    editorWindow.ShowWithMode((ShowMode)mode);
                };

            UnityInternals.UnityEditor.SavedGUIState.Create =
                () =>
                {
                    return new UnityInternals.UnityEditor.SavedGUIState(SavedGUIState.Create());
                };

            UnityInternals.UnityEditor.SavedGUIState.ApplyAndForgetInternal =
                (UnityInternals.UnityEditor.SavedGUIState savedGUIState) =>
                {
                    ((SavedGUIState)savedGUIState.InternalObject).ApplyAndForget();
                };

            UnityInternals.UnityEditor.SplitterState.Constructor =
                (float[] relativeSizes, int[] minSizes, int[] maxSizes) =>
                {
                    return new SplitterState(relativeSizes, minSizes, maxSizes);
                };

            UnityInternals.UnityEditor.SplitterState.GetRelativeSizes =
                (UnityInternals.UnityEditor.SplitterState splitterState) =>
                {
                    return ((SplitterState)splitterState.InternalObject).relativeSizes;
                };

            UnityInternals.UnityEditor.SplitterGUILayout.BeginHorizontalSplit =
                (UnityInternals.UnityEditor.SplitterState splitterState,
                    GUILayoutOption[] guiLayoutOptions) =>
                {
                    SplitterGUILayout.BeginHorizontalSplit(
                        (SplitterState)splitterState.InternalObject, guiLayoutOptions);
                };

            UnityInternals.UnityEditor.SplitterGUILayout.EndHorizontalSplit =
                () =>
                {
                    SplitterGUILayout.EndHorizontalSplit();
                };

            UnityInternals.UnityEditor.SplitterGUILayout.BeginVerticalSplit =
                (UnityInternals.UnityEditor.SplitterState splitterState,
                    GUILayoutOption[] guiLayoutOptions) =>
                {
                    SplitterGUILayout.BeginVerticalSplit(
                        (SplitterState)splitterState.InternalObject, guiLayoutOptions);
                };

            UnityInternals.UnityEditor.SplitterGUILayout.EndVerticalSplit =
                () =>
                {
                    SplitterGUILayout.EndVerticalSplit();
                };

            UnityInternals.UnityEditor.SceneManagement.PrefabStageExtensions.SaveInternal =
                (PrefabStage prefabStage) =>
                {
                    return prefabStage.Save();
                };

            UnityInternals.UnityEditor.GenericMenuExtensions.GetMenuItems =
                (GenericMenu genericMenu) =>
                {
                    List<GenericMenu.MenuItem> menuItems = genericMenu.menuItems;

                    if (menuItems == null)
                        return null;

                    return menuItems.Select(menuItem =>
                        new UnityInternals.UnityEditor.GenericMenu.MenuItem(menuItem)).ToList();
                };

            UnityInternals.UnityEditor.GenericMenu.MenuItem.GetContent =
                (UnityInternals.UnityEditor.GenericMenu.MenuItem menuItem) =>
                {
                    return ((GenericMenu.MenuItem)menuItem.InternalObject).content;
                };

            UnityInternals.UnityEditor.GenericMenu.MenuItem.GetFunc =
                (UnityInternals.UnityEditor.GenericMenu.MenuItem menuItem) =>
                {
                    return ((GenericMenu.MenuItem)menuItem.InternalObject).func;
                };
        }
    }
}
