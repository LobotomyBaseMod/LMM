using System;

using UnityEditor;
using UnityEngine;

using PlasticGui;
using Unity.PlasticSCM.Editor.UI.Tree;

namespace Unity.PlasticSCM.Editor.Toolbar.PopupWindow.BranchesList
{
    internal class NoBranchesEmptyState : CenteredContentPanel
    {
        internal NoBranchesEmptyState(Action createBranchAction, Action repaintAction)
            : base(repaintAction)
        {
            mCreateBranchAction = createBranchAction;
        }

        protected override void DrawGUI()
        {
            CenterContent(
                () =>
                {
                    GUILayout.Label(
                        PlasticLocalization.Name.NoBranchesMatchingFilters.GetString(),
                        EditorStyles.boldLabel);
                },
                () =>
                {
                    GUILayout.Label(
                        PlasticLocalization.Name.CreateANewBranchInstead.GetString(),
                        EditorStyles.label);
                },
                () =>
                {
                    if (GUILayout.Button(
                        PlasticLocalization.Name.CreateNewBranchButton.GetString()))
                    {
                        mCreateBranchAction();
                    }
                });
        }

        readonly Action mCreateBranchAction;
    }
}
