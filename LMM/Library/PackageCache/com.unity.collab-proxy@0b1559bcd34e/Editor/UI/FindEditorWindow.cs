using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Unity.PlasticSCM.Editor.UnityInternals.UnityEditor;

using EditorWindow = UnityEditor.EditorWindow;
using HostView = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.HostView;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class FindEditorWindow
    {
        internal static EditorWindow ProjectWindow()
        {
            Type projectBrowserType = typeof(EditorWindow).Assembly.GetType(
                    "UnityEditor.ProjectBrowser");

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(
                 projectBrowserType);

            if (windows.Length == 0)
                return null;

            return windows[0] as EditorWindow;
        }

        internal static EditorWindow ToDock<T>()
        {
            List<EditorWindow> windows = GetAvailableWindows();

            IEnumerable<EditorWindow> candidateWindows = windows
                .Where(w => !(w is T))
                .Where(w => w.position.width > 400 && w.position.height > 300)
                .OrderByDescending(w => w.position.width * w.position.height);

            return candidateWindows.FirstOrDefault();
        }

        internal static EditorWindow FirstAvailableWindow()
        {
            List<EditorWindow> windows = GetAvailableWindows();

            if (windows == null || windows.Count == 0)
                return null;

            return windows[0];
        }

        static List<EditorWindow> GetAvailableWindows()
        {
            List<EditorWindow> result = new List<EditorWindow>();

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                HostView hostView = window.m_Parent();

                if (hostView == null)
                    continue;

                EditorWindow actualDrawnWindow = hostView.m_ActualView;

                if (actualDrawnWindow == null)
                    continue;

                if (result.Contains(actualDrawnWindow))
                    continue;

                result.Add(actualDrawnWindow);
            }

            return result;
        }
    }
}
