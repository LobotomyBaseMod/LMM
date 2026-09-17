using Codice.Utils;
using Unity.PlasticSCM.Editor.UI.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal static class KeyboardActions
    {
        internal enum SearchAction
        {
            None,
            Clean,
            NextSearch,
            PreviousSearch
        }

        internal enum NavigationAction
        {
            None,
            MoveUp,
            MoveDown,
            MoveRight,
            MoveLeft,
            Escape,
            Home,
            End
        }

        internal static SearchAction GetSearchAction(KeyDownEvent evt)
        {
            if (KeyboardEvents.IsEscapePressed(evt))
                return SearchAction.Clean;

            if (PlatformIdentifier.IsMac() &&
                evt.commandKey &&
                KeyboardEvents.IsEnterPressed(evt))
                return SearchAction.PreviousSearch;

            if (PlatformIdentifier.IsWindows() &&
                KeyboardEvents.IsControlPressed(evt) &&
                KeyboardEvents.IsEnterPressed(evt))
                return SearchAction.PreviousSearch;

            if (KeyboardEvents.IsEnterPressed(evt))
                return SearchAction.NextSearch;

            return SearchAction.None;
        }

        internal static NavigationAction GetNavigationAction(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    return NavigationAction.MoveUp;
                case KeyCode.DownArrow:
                    return NavigationAction.MoveDown;
                case KeyCode.RightArrow:
                    return NavigationAction.MoveRight;
                case KeyCode.LeftArrow:
                    return NavigationAction.MoveLeft;
                case KeyCode.Escape:
                    return NavigationAction.Escape;
                case KeyCode.Home:
                    return NavigationAction.Home;
                case KeyCode.End:
                    return NavigationAction.End;
            }

            return NavigationAction.None;
        }
    }
}
