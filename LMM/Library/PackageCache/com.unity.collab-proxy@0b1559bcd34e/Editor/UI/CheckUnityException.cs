using System;

using UnityEngine;

namespace Unity.PlasticSCM.Editor.UI
{
    internal static class CheckUnityException
    {
        internal static bool IsExitGUIException(Exception ex)
        {
            return ex is ExitGUIException;
        }

        internal static bool IsIMGUIPaintException(Exception ex)
        {
            if (!(ex is ArgumentException))
                return false;

            return ex.Message.StartsWith("Getting control") &&
                   ex.Message.Contains("controls when doing repaint");
        }
    }
}
