using System;

using UnityEngine;
using UnityEngine.UIElements;

using PlasticGui;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class BranchExplorerEmptyStatePanel : VisualElement
    {
        internal BranchExplorerEmptyStatePanel(Action resetFiltersAction)
        {
            mResetFiltersAction = resetFiltersAction;

            CreateGUI();
        }

        internal void Dispose()
        {
            if (mResetFiltersButton != null)
                mResetFiltersButton.clicked -= OnResetFiltersClicked;
        }

        void CreateGUI()
        {
            style.flexGrow = 1;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            Label emptyStateLabel = new Label(
                PlasticLocalization.Name.NoBranchesMatchingFilters.GetString());
            emptyStateLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyStateLabel.style.whiteSpace = WhiteSpace.Normal;

            Add(emptyStateLabel);

            mResetFiltersButton = new Button();
            mResetFiltersButton.text =
                PlasticLocalization.Name.ResetFilters.GetString();
            mResetFiltersButton.style.marginTop = 20;
            mResetFiltersButton.clicked += OnResetFiltersClicked;
            Add(mResetFiltersButton);
        }

        void OnResetFiltersClicked()
        {
            mResetFiltersAction?.Invoke();
        }

        readonly Action mResetFiltersAction;
        Button mResetFiltersButton;
    }
}
