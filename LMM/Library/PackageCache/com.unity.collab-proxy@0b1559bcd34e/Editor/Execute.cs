using System;

using UnityEditor;
using UnityEditorInternal;

namespace Unity.PlasticSCM.Editor
{
    internal static class Execute
    {
        // Executes action once when the editor is not updating or compiling.
        // Use this for quick actions that need to wait for the current import/compile to finish.
        internal static void WhenEditorIsReady(Action action)
        {
            if (PlasticApp.IsUnitTesting)
            {
                action();
                return;
            }

            EditorApplication.update += RunOnceWhenEditorIsReady;

            bool IsEditorBusy()
            {
                return EditorApplication.isUpdating ||
                       EditorApplication.isCompiling;
            }

            void RunOnceWhenEditorIsReady()
            {
                if (IsEditorBusy())
                    return;

                EditorApplication.update -= RunOnceWhenEditorIsReady;

                action();
            }
        }

        // Executes action once the editor has been idle for a sustained period of time.
        // Checks: isUpdating, isCompiling, Progress.running (background tasks),
        // and isApplicationActive (editor must be in foreground, otherwise background tasks are deferred).
        // Use this for operations that need all editor activity to settle first,
        // such as workspace creation from the Hub where late file modifications
        // could cause files to appear as "changed" in pending changes.
        internal static void AfterEditorIsIdleForSeconds(double delaySeconds, Action action)
        {
            if (PlasticApp.IsUnitTesting)
            {
                action();
                return;
            }

            double lastBusyTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += RunOnceAfterEditorIsIdle;

            bool IsEditorInBackground()
            {
                return !InternalEditorUtility.isApplicationActive;
            }

            bool IsEditorBusy()
            {
                return EditorApplication.isUpdating ||
                       EditorApplication.isCompiling ||
                       Progress.running;
            }

            void RunOnceAfterEditorIsIdle()
            {
                double currentTime = EditorApplication.timeSinceStartup;

                if (IsEditorInBackground() || IsEditorBusy())
                {
                    lastBusyTime = currentTime;
                    return;
                }

                if (currentTime - lastBusyTime < delaySeconds)
                    return;

                EditorApplication.update -= RunOnceAfterEditorIsIdle;

                action();
            }
        }
    }
}
