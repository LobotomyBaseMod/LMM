using System;

using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList
{
    internal class LoadingEmptyState : CenteredContentPanel
    {
        public LoadingEmptyState(Action repaintAction)
            : base(repaintAction)
        {
        }

        protected override void DrawGUI()
        {
            CenterContent(() =>
            {
                GUILayout.Label(PlasticLocalization.Name.
                    LoadingBranches.GetString());
            });
        }
    }
}
