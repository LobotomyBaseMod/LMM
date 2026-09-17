using System;
using UnityEngine.UIElements;

using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Configuration;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.DisplayOptions
{
    internal class DisplayOptionsPanel : VisualElement
    {
        internal DisplayOptionsPanel(
            WorkspaceInfo wkInfo,
            Func<BranchExplorerOptionsWindow.IBranchExplorerView> getBrExView)
        {
            mWkInfo = wkInfo;
            mGetBrExView = getBrExView;

            CreateGUI();
        }

        internal void SetWorkspaceUIConfiguration(
            WorkspaceUIConfiguration workspaceUIConfiguration)
        {
            mConfig = workspaceUIConfiguration;
        }

        internal void LoadConfiguration()
        {
            mIsLoadingConfiguration = true;

            try
            {
                mDisplayBranchesToggle.value =
                    mConfig.DisplayOptions.DisplayBranches;

                mDisplayFullBranchNamesToggle.value =
                    mConfig.DisplayOptions.DisplayFullBranchNames;

                mDisplayMergeLinksToggle.value =
                    mConfig.DisplayOptions.DisplayMergeLinks;

                mDisplayCrossBranchChangesetLinksToggle.value =
                    mConfig.DisplayOptions.DisplayCrossBranchChangesetLinks;

                mDisplayLabelsToggle.value =
                    mConfig.DisplayOptions.DisplayLabels;

                mDisplayBranchTaskInfoToggle.value =
                    mConfig.DisplayOptions.DisplayTaskInfoOnBranches;

                mDisplayChangesetCommentsToggle.value =
                    mConfig.DisplayOptions.DisplayChangesetComments;

                mDisplayUserAvatarToggle.value =
                    (mConfig.DisplayOptions.ChangesetColorMode & ChangesetColorMode.ByUser)
                    == ChangesetColorMode.ByUser;
            }
            finally
            {
                mIsLoadingConfiguration = false;
            }
        }

        internal void Dispose()
        {
            mDisplayBranchesToggle.UnregisterValueChangedCallback(
                OnDisplayBranchesChanged);
            mDisplayFullBranchNamesToggle.UnregisterValueChangedCallback(
                OnDisplayFullBranchNamesChanged);
            mDisplayMergeLinksToggle.UnregisterValueChangedCallback(
                OnDisplayMergeLinksChanged);
            mDisplayCrossBranchChangesetLinksToggle.UnregisterValueChangedCallback(
                OnDisplayCrossBranchChangesetLinksChanged);
            mDisplayLabelsToggle.UnregisterValueChangedCallback(
                OnDisplayLabelsChanged);
            mDisplayBranchTaskInfoToggle.UnregisterValueChangedCallback(
                OnDisplayBranchTaskInfoChanged);
            mDisplayChangesetCommentsToggle.UnregisterValueChangedCallback(
                OnDisplayChangesetCommentsChanged);
            mDisplayUserAvatarToggle.UnregisterValueChangedCallback(
                OnColorChangesetsByUserChanged);
        }

        void OnDisplayBranchesChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayBranches = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void OnDisplayFullBranchNamesChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayFullBranchNames = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.ClearSearchResults();
            mGetBrExView()?.Redraw();
        }

        void OnDisplayMergeLinksChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayMergeLinks = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void OnDisplayCrossBranchChangesetLinksChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayCrossBranchChangesetLinks = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void OnDisplayLabelsChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayLabels = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void OnDisplayBranchTaskInfoChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayTaskInfoOnBranches = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void OnDisplayChangesetCommentsChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            mConfig.DisplayOptions.DisplayChangesetComments = evt.newValue;
            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void OnColorChangesetsByUserChanged(ChangeEvent<bool> evt)
        {
            if (mIsLoadingConfiguration)
                return;

            if (evt.newValue)
                mConfig.DisplayOptions.ChangesetColorMode |= ChangesetColorMode.ByUser;
            else
                mConfig.DisplayOptions.ChangesetColorMode &= ~ChangesetColorMode.ByUser;

            mConfig.Save(mWkInfo);

            mGetBrExView()?.Redraw();
        }

        void CreateGUI()
        {
            style.flexGrow = 1;

            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingLeft = 4;
            scrollView.style.paddingTop = 8;
            scrollView.style.paddingRight = 8;

            mDisplayBranchesToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayBranches);
            mDisplayBranchesToggle.RegisterValueChangedCallback(
                OnDisplayBranchesChanged);

            mDisplayFullBranchNamesToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayFullBranchNames);
            mDisplayFullBranchNamesToggle.RegisterValueChangedCallback(
                OnDisplayFullBranchNamesChanged);

            mDisplayMergeLinksToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayMergeLinks);
            mDisplayMergeLinksToggle.RegisterValueChangedCallback(
                OnDisplayMergeLinksChanged);

            mDisplayCrossBranchChangesetLinksToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayCrossBranchCsetLinks);
            mDisplayCrossBranchChangesetLinksToggle.RegisterValueChangedCallback(
                OnDisplayCrossBranchChangesetLinksChanged);

            mDisplayLabelsToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayLabels);
            mDisplayLabelsToggle.RegisterValueChangedCallback(
                OnDisplayLabelsChanged);

            mDisplayBranchTaskInfoToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayBranchTaskInfo);
            mDisplayBranchTaskInfoToggle.RegisterValueChangedCallback(
                OnDisplayBranchTaskInfoChanged);

            mDisplayChangesetCommentsToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayChangesetComments);
            mDisplayChangesetCommentsToggle.RegisterValueChangedCallback(
                OnDisplayChangesetCommentsChanged);

            mDisplayUserAvatarToggle = CreateToggle(
                PlasticLocalization.Name.BrexDisplayUserAvatar);
            mDisplayUserAvatarToggle.RegisterValueChangedCallback(
                OnColorChangesetsByUserChanged);

            scrollView.Add(mDisplayBranchesToggle);
            scrollView.Add(mDisplayFullBranchNamesToggle);
            scrollView.Add(mDisplayMergeLinksToggle);
            scrollView.Add(mDisplayCrossBranchChangesetLinksToggle);
            scrollView.Add(mDisplayLabelsToggle);
            scrollView.Add(mDisplayBranchTaskInfoToggle);
            scrollView.Add(mDisplayChangesetCommentsToggle);
            scrollView.Add(mDisplayUserAvatarToggle);

            Add(scrollView);
        }

        static Toggle CreateToggle(
            PlasticLocalization.Name localizationName)
        {
            Toggle toggle = new Toggle(
                PlasticLocalization.GetString(localizationName));
            toggle.style.marginLeft = 10;
            toggle.style.marginBottom = 6;
            toggle.labelElement.style.minWidth = 250;
            return toggle;
        }

        Toggle mDisplayBranchesToggle;
        Toggle mDisplayFullBranchNamesToggle;
        Toggle mDisplayMergeLinksToggle;
        Toggle mDisplayCrossBranchChangesetLinksToggle;
        Toggle mDisplayLabelsToggle;
        Toggle mDisplayBranchTaskInfoToggle;
        Toggle mDisplayChangesetCommentsToggle;
        Toggle mDisplayUserAvatarToggle;

        WorkspaceUIConfiguration mConfig;
        bool mIsLoadingConfiguration;

        readonly WorkspaceInfo mWkInfo;
        readonly Func<BranchExplorerOptionsWindow.IBranchExplorerView> mGetBrExView;
    }
}
