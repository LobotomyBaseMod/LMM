using System.Reflection;

using UnityEditorWindow = UnityEditor.EditorWindow;

#if !UNITY_6000_3_OR_NEWER
using Unity.PlasticSCM.Editor.UnityInternals.UnityEditor;
using EditorWindow = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorWindow;
#else
using UnityEditor;
using EditorWindow = UnityEditor.EditorWindow;
#endif

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class RunModal
    {
        static RunModal()
        {
            InitializeInfo();
        }

        internal static void Dialog(UnityEditorWindow window)
        {
            ShowAsUtility(window);

            SavedGUIState savedGUIState = CreateSavedGUIState();
            PushDispatcherContext(window);

            MakeModal(window);

            PopDispatcherContext(window);
            ApplySavedGUIState(savedGUIState);
        }

        static void MakeModal(UnityEditorWindow window)
        {
            // MakeModal(m_Parent.window);
#if !UNITY_6000_3_OR_NEWER
            HostView hostView = window.m_Parent();
#else
            HostView hostView = window.m_Parent;
#endif
            ContainerWindow parentWindow = hostView.window;

            EditorWindow.Internal_MakeModal(parentWindow);
        }

        static void ShowAsUtility(UnityEditorWindow window)
        {
            // ShowWithMode(ShowMode.Utility);

#if !UNITY_6000_3_OR_NEWER
            window.ShowWithMode(2);
#else
            window.ShowWithMode((ShowMode)2);
#endif
        }

        static SavedGUIState CreateSavedGUIState()
        {
            // SavedGUIState guiState = SavedGUIState.Create();
            return SavedGUIState.Create();
        }

        static void ApplySavedGUIState(SavedGUIState savedGUIState)
        {
            // guiState.ApplyAndForget();
            savedGUIState.ApplyAndForget();
        }

        static void PopDispatcherContext(UnityEditorWindow window)
        {
            //UnityEngine.UIElements.EventDispatcher.editorDispatcher.PopDispatcherContext();

            object editorDispatcher = mEditorDispatcherProp2020.GetValue(null);
            mPopContextMethod2020.Invoke(editorDispatcher, null);
        }

        static void PushDispatcherContext(UnityEditorWindow window)
        {
            //UnityEngine.UIElements.EventDispatcher.editorDispatcher.PushDispatcherContext();

            object editorDispatcher = mEditorDispatcherProp2020.GetValue(null);
            mPushContextMethod2020.Invoke(editorDispatcher, null);
        }

        static void InitializeInfo()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

            mEditorDispatcherProp2020 = typeof(UnityEngine.UIElements.EventDispatcher).GetProperty("editorDispatcher", flags);
            mPushContextMethod2020 = mEditorDispatcherProp2020.PropertyType.GetMethod("PushDispatcherContext", flags);
            mPopContextMethod2020 = mEditorDispatcherProp2020.PropertyType.GetMethod("PopDispatcherContext", flags);
        }

        static PropertyInfo mEditorDispatcherProp2020;
        static MethodInfo mPushContextMethod2020;
        static MethodInfo mPopContextMethod2020;

        // // How ContainerWindows are visualized. Used with ContainerWindow.Show
        // internal enum ShowMode
        // {
        //     // Show as a normal window with max, min & close buttons.
        //     NormalWindow = 0,
        //     // Used for a popup menu. On mac this means light shadow and no titlebar.
        //     PopupMenu = 1,
        //     // Utility window - floats above the app. Disappears when app loses focus.
        //     Utility = 2,
        //     // Window has no shadow or decorations. Used internally for dragging stuff around.
        //     NoShadow = 3,
        //     // The Unity main window. On mac, this is the same as NormalWindow, except window doesn't have a close button.
        //     MainWindow = 4,
        //     // Aux windows. The ones that close the moment you move the mouse out of them.
        //     AuxWindow = 5,
        //     // Like PopupMenu, but without keyboard focus
        //     cm-help.es.txtTooltip = 6
        // }
    }
}
