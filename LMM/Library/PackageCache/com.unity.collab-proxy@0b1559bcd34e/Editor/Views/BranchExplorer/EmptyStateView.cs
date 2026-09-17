using System;

using UnityEngine.UIElements;

using PlasticGui;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class EmptyStateView : VisualElement
    {
        internal EmptyStateView(Action onCreateWorkspaceClicked)
        {
            CreateGUI(onCreateWorkspaceClicked);
        }

        void CreateGUI(Action onCreateWorkspaceClicked)
        {
            style.flexGrow = 1;
            style.alignItems = Align.Center;
            style.justifyContent = Justify.Center;

            var emptyStateLabel = new Label(
                PlasticLocalization.Name.BranchExplorerNoWorkspaceMessage.GetString());
            emptyStateLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            emptyStateLabel.style.whiteSpace = WhiteSpace.Normal;

            var createWorkspaceButton = new Button(onCreateWorkspaceClicked)
            {
                text = PlasticLocalization.Name.CreateWorkspace.GetString()
            };
            createWorkspaceButton.style.marginTop = 10;

            Add(emptyStateLabel);
            Add(createWorkspaceButton);
        }
    }
}
