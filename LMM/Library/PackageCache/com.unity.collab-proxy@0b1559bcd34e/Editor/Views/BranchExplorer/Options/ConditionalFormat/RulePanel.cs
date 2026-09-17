using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using CodiceApp;
using Codice.Client.BaseCommands.BranchExplorer.Layout;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Configuration;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Options.ConditionalFormat
{
    internal class RulePanel : VisualElement
    {
        internal Rule.Type RuleType { get { return mRuleData.Type; } }
        internal Rule.FormattedObject FormatTarget { get { return mRuleData.FormatTarget; } }

        internal RulePanel(
            Rule.Type type,
            Rule.FormattedObject formatTarget,
            Action onRuleChanged,
            Action onRuleColorChanged)
        {
            mRuleData = new RuleData(type, formatTarget);
            mOnRuleChanged = onRuleChanged;
            mOnRuleColorChanged = onRuleColorChanged;
        }

        internal RulePanel(
            Rule rule,
            Action onRuleChanged,
            Action onRuleColorChanged)
        {
            mRuleData = new RuleData(rule);
            mOnRuleChanged = onRuleChanged;
            mOnRuleColorChanged = onRuleColorChanged;
        }

        internal BrExLayoutFilter GetFilter(WorkspaceInfo wkInfo, RepositorySpec repSpec)
        {
            return mRuleData.GetFilter(wkInfo, repSpec);
        }

        internal Rule GetRule()
        {
            Rule rule = new Rule();
            rule.RuleType = mRuleData.Type;
            rule.Enabled = GetEnabledFromField();
            rule.Description = GetDescriptionFromField();
            rule.Condition = GetConditionFromField();
            rule.Color = GetColorFromColorField();
            rule.FormatTarget = mRuleData.FormatTarget;
            rule.Options = mRuleData.Options;
            return rule;
        }

        internal void CreateView(VisualElement parentPanel)
        {
            if (mbInitialized)
                return;

            mParentPanel = parentPanel;

            DoCreateView();

            mbInitialized = true;
        }

        internal void OpenEditionPanel()
        {
            EnableEdition();
        }

        internal void Dispose()
        {
            if (mDescriptionToggle != null)
                mDescriptionToggle.UnregisterValueChangedCallback(OnDescriptionToggleValueChanged);

            if (mColorField != null)
                mColorField.UnregisterValueChangedCallback(OnColorValueChanged);
        }

        void DoCreateView()
        {
            style.paddingTop = 4;
            style.paddingBottom = 4;
            style.paddingLeft = 4;
            style.paddingRight = 4;

            VisualElement mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.alignItems = Align.FlexStart;

            VisualElement leftBox = CreateLeftIndicator(
                RuleType, FormatTarget, mRuleData.GetColor());

            VisualElement fieldArea = new VisualElement();
            fieldArea.style.flexGrow = 1;
            fieldArea.style.marginLeft = 4;

            mDescriptionRow = new VisualElement();
            mDescriptionRow.style.flexDirection = FlexDirection.Row;
            mDescriptionRow.style.alignItems = Align.Center;
            mDescriptionRow.style.marginBottom = 2;

            mDescriptionToggle = new Toggle();
            mDescriptionToggle.style.marginLeft = 0;
            mDescriptionToggle.style.marginTop = 3;
            mDescriptionToggle.style.marginRight = 0;
            mDescriptionToggle.style.marginBottom = 0;
            mDescriptionToggle.value = mRuleData.IsEnabled;
            mDescriptionToggle.RegisterValueChangedCallback(
                OnDescriptionToggleValueChanged);

            mDescriptionLabel = new Label(mRuleData.Description);
            mDescriptionLabel.style.marginLeft = 6;
            mDescriptionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            mDescriptionRow.Add(mDescriptionToggle);
            mDescriptionRow.Add(mDescriptionLabel);

            mConditionLabel = new Label(mRuleData.Condition);
            mConditionLabel.style.fontSize = 11;
            mConditionLabel.style.marginLeft = 20;
            mConditionLabel.style.overflow = Overflow.Hidden;
            mConditionLabel.style.textOverflow = TextOverflow.Ellipsis;
            mConditionLabel.style.color = new StyleColor(
                UnityStyles.Colors.SecondaryLabel);

            mDescriptionEdit = new TextField();
            mDescriptionEdit.value = mRuleData.Description;
            mDescriptionEdit.style.marginBottom = 4;
            mDescriptionEdit.style.display = DisplayStyle.None;

            mConditionEdit = new TextField();
            mConditionEdit.value = mRuleData.Condition;
            mConditionEdit.style.display = DisplayStyle.None;

            fieldArea.Add(mDescriptionRow);
            fieldArea.Add(mConditionLabel);
            fieldArea.Add(mDescriptionEdit);
            fieldArea.Add(mConditionEdit);

            VisualElement buttonArea = new VisualElement();
            buttonArea.style.flexDirection = FlexDirection.Row;
            buttonArea.style.alignItems = Align.FlexStart;

            bool isEditable = mRuleData.Type != Rule.Type.NonIntegrated
                && mRuleData.Type != Rule.Type.CurrentBranchFormat;

            int buttonSize = 22;

            if (isEditable)
            {
                mEditButton = ControlBuilder.Button.CreateImageButton(
                    Images.GetEditIcon(),
                    PlasticLocalization.GetString(
                        PlasticLocalization.Name.EditButton),
                    OnEditRuleClicked);
                mEditButton.style.marginRight = 2;
                mEditButton.style.width = buttonSize;
                mEditButton.style.height = buttonSize;
                mEditButton.style.paddingLeft = 0;
                mEditButton.style.paddingRight = 0;
                buttonArea.Add(mEditButton);
            }

            mDeleteButton = ControlBuilder.Button.CreateImageButton(
                Images.GetIconCloseButton(),
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.RemoveButton),
                OnDeleteRuleClicked);
            mDeleteButton.style.width = buttonSize;
            mDeleteButton.style.height = buttonSize;
            mDeleteButton.style.paddingLeft = 0;
            mDeleteButton.style.paddingRight = 0;
            buttonArea.Add(mDeleteButton);

            mainRow.Add(leftBox);
            mainRow.Add(fieldArea);
            mainRow.Add(buttonArea);

            Add(mainRow);

            mEditionPanel = CreateEditionPanel();
            mEditionPanel.style.display = DisplayStyle.None;
            Add(mEditionPanel);

            VisualElement separator = new VisualElement();
            separator.style.height = 1;
            separator.style.marginTop = 4;
            separator.style.backgroundColor = new StyleColor(
                UnityStyles.Colors.SplitLineColor);
            Add(separator);
        }

        VisualElement CreateLeftIndicator(
            Rule.Type type,
            Rule.FormattedObject formatTarget,
            ColorRGB defaultColor)
        {
            VisualElement container = new VisualElement();
            container.style.width = 20;
            container.style.alignItems = Align.Center;
            container.style.marginTop = 2;

            if (type == Rule.Type.InclusionRule)
            {
                Image icon = new Image();
                icon.image = Images.GetInclusionRuleIcon();
                icon.style.width = 16;
                icon.style.height = 16;
                container.Add(icon);
                return container;
            }

            if (type == Rule.Type.ExclusionRule)
            {
                Image icon = new Image();
                icon.image = Images.GetExclusionRuleIcon();
                icon.style.width = 16;
                icon.style.height = 16;
                container.Add(icon);
                return container;
            }

            Texture2D targetIcon = GetFormatTargetIcon(formatTarget);
            if (targetIcon != null)
            {
                Image iconImage = new Image();
                iconImage.image = targetIcon;
                iconImage.style.width = 16;
                iconImage.style.height = 16;
                iconImage.style.marginBottom = 2;
                container.Add(iconImage);
            }

            mColorField = new ColorField();
            mColorField.style.width = 16;
            mColorField.style.height = 16;
            mColorField.showAlpha = false;
            mColorField.showEyeDropper = false;
            mColorField.value = new Color(defaultColor.R, defaultColor.G, defaultColor.B, defaultColor.A);
            mColorField.RegisterValueChangedCallback(
                OnColorValueChanged);
            container.Add(mColorField);

            return container;
        }

        static Texture2D GetFormatTargetIcon(Rule.FormattedObject formatTarget)
        {
            switch (formatTarget)
            {
                case Rule.FormattedObject.Branch:
                    return Images.GetBranchIcon();
                case Rule.FormattedObject.Changeset:
                    return Images.GetChangesetsIcon();
                case Rule.FormattedObject.Label:
                    return Images.GetLabelIcon();
                default:
                    return null;
            }
        }

        VisualElement CreateEditionPanel()
        {
            VisualElement panel = new VisualElement();
            panel.style.marginLeft = 24;
            panel.style.marginTop = 8;
            panel.style.marginBottom = 4;

            Label titleText = new Label(
                ConditionalFormatRuleText.ForTitle(RuleType, FormatTarget));
            titleText.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleText.style.marginBottom = 4;
            panel.Add(titleText);

            Label helpText = new Label(
                ConditionalFormatRuleText.ForHelp(FormatTarget));
            helpText.style.fontSize = 11;
            helpText.style.marginBottom = 8;
            panel.Add(helpText);

            if (mRuleData.Type == Rule.Type.InclusionRule
                || mRuleData.Type == Rule.Type.ExclusionRule)
            {
                VisualElement checksContainer = CreateCheckBoxesContainer();
                panel.Add(checksContainer);
            }

            VisualElement buttonsPanel = new VisualElement();
            buttonsPanel.style.flexDirection = FlexDirection.Row;
            buttonsPanel.style.justifyContent = Justify.FlexEnd;
            buttonsPanel.style.marginTop = 8;

            Button saveButton = new Button(OnSaveClicked);
            saveButton.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.SaveButton);
            saveButton.style.marginRight = 4;
            buttonsPanel.Add(saveButton);

            Button cancelButton = new Button(OnCancelClicked);
            cancelButton.text = PlasticLocalization.GetString(
                PlasticLocalization.Name.CancelButton);
            buttonsPanel.Add(cancelButton);

            panel.Add(buttonsPanel);

            return panel;
        }

        VisualElement CreateCheckBoxesContainer()
        {
            VisualElement container = new VisualElement();
            container.style.marginTop = 4;
            container.style.marginBottom = 4;

            mAddChildBranches = CreateCheckToggle(
                PlasticLocalization.Name.AddChildBranches,
                RelatedBranchFlags.IncludeChildBranches);

            mAddParentBranches = CreateCheckToggle(
                PlasticLocalization.Name.AddParentBranches,
                RelatedBranchFlags.IncludeParentBranches);

            mAddBranchesSource = CreateCheckToggle(
                PlasticLocalization.Name.AddBranchesSourceOfMerge,
                RelatedBranchFlags.IncludeMergeSourceBranches);

            mAddBranchesDestination = CreateCheckToggle(
                PlasticLocalization.Name.AddBranchesDestinationOfMerge,
                RelatedBranchFlags.IncludeMergeDestinationBranches);

            container.Add(mAddChildBranches);
            container.Add(mAddParentBranches);
            container.Add(mAddBranchesSource);
            container.Add(mAddBranchesDestination);

            return container;
        }

        Toggle CreateCheckToggle(
            PlasticLocalization.Name localizationName,
            RelatedBranchFlags flag)
        {
            Toggle toggle = new Toggle(
                PlasticLocalization.GetString(localizationName));
            toggle.value = (mRuleData.Options & flag) == flag;
            toggle.style.marginBottom = 2;
            toggle.labelElement.style.minWidth = 230;
            return toggle;
        }

        void EnableEdition()
        {
            mEditionPanel.style.display = DisplayStyle.Flex;
            mDescriptionEdit.style.display = DisplayStyle.Flex;
            mConditionEdit.style.display = DisplayStyle.Flex;
            mDescriptionRow.style.display = DisplayStyle.None;
            mConditionLabel.style.display = DisplayStyle.None;

            mDescriptionEdit.value = mRuleData.Description;
            mConditionEdit.value = mRuleData.Condition;
            SetFieldsFromOptions(mRuleData.Options);

            mDescriptionEdit.Focus();
        }

        void DisableEdition()
        {
            if (!mbInitialized)
                return;

            mEditionPanel.style.display = DisplayStyle.None;
            mDescriptionEdit.style.display = DisplayStyle.None;
            mConditionEdit.style.display = DisplayStyle.None;
            mDescriptionRow.style.display = DisplayStyle.Flex;
            mConditionLabel.style.display = DisplayStyle.Flex;
        }

        void OnEditRuleClicked()
        {
            OpenEditionPanel();
        }

        void OnDescriptionToggleValueChanged(ChangeEvent<bool> evt)
        {
            mOnRuleChanged?.Invoke();
        }

        void OnColorValueChanged(ChangeEvent<Color> evt)
        {
            mOnRuleColorChanged?.Invoke();
        }

        void OnDeleteRuleClicked()
        {
            mParentPanel.Remove(this);
            Dispose();
            mOnRuleChanged?.Invoke();
        }

        void OnSaveClicked()
        {
            mRuleData.Description = GetDescriptionFromField();
            mRuleData.Condition = mConditionEdit.value;
            mRuleData.Options = GetOptionsFromFields();
            mDescriptionLabel.text = mDescriptionEdit.value;
            mConditionLabel.text = mConditionEdit.value;

            DisableEdition();
            mOnRuleChanged?.Invoke();
        }

        void OnCancelClicked()
        {
            DisableEdition();

            SetFieldsFromOptions(mRuleData.Options);
            mDescriptionEdit.value = mDescriptionLabel.text;
            mConditionEdit.value = mConditionLabel.text;
        }

        ColorRGB GetColorFromColorField()
        {
            if (mRuleData.Type == Rule.Type.InclusionRule
                || mRuleData.Type == Rule.Type.ExclusionRule)
            {
                return null;
            }

            if (mColorField == null)
                return mRuleData.GetColor();

            Color color = mColorField.value;
            return new ColorRGB(color.r, color.g, color.b, color.a);
        }

        string GetConditionFromField()
        {
            if (string.IsNullOrEmpty(mConditionEdit.value))
                return null;

            return mConditionEdit.value;
        }

        string GetDescriptionFromField()
        {
            if (DescriptionEditContainsDefaultText())
                return string.Empty;

            return mDescriptionEdit.value;
        }

        bool GetEnabledFromField()
        {
            return mDescriptionToggle.value;
        }

        RelatedBranchFlags GetOptionsFromFields()
        {
            RelatedBranchFlags result = RelatedBranchFlags.None;

            if (mAddChildBranches != null && mAddChildBranches.value)
                result |= RelatedBranchFlags.IncludeChildBranches;

            if (mAddParentBranches != null && mAddParentBranches.value)
                result |= RelatedBranchFlags.IncludeParentBranches;

            if (mAddBranchesSource != null && mAddBranchesSource.value)
                result |= RelatedBranchFlags.IncludeMergeSourceBranches;

            if (mAddBranchesDestination != null && mAddBranchesDestination.value)
                result |= RelatedBranchFlags.IncludeMergeDestinationBranches;

            return result;
        }

        void SetFieldsFromOptions(RelatedBranchFlags flags)
        {
            if (mAddChildBranches != null)
                mAddChildBranches.value =
                    (flags & RelatedBranchFlags.IncludeChildBranches) == RelatedBranchFlags.IncludeChildBranches;

            if (mAddParentBranches != null)
                mAddParentBranches.value =
                    (flags & RelatedBranchFlags.IncludeParentBranches) == RelatedBranchFlags.IncludeParentBranches;

            if (mAddBranchesSource != null)
                mAddBranchesSource.value =
                    (flags & RelatedBranchFlags.IncludeMergeSourceBranches) == RelatedBranchFlags.IncludeMergeSourceBranches;

            if (mAddBranchesDestination != null)
                mAddBranchesDestination.value =
                    (flags & RelatedBranchFlags.IncludeMergeDestinationBranches) == RelatedBranchFlags.IncludeMergeDestinationBranches;
        }

        bool DescriptionEditContainsDefaultText()
        {
            return mDescriptionEdit.value ==
                PlasticLocalization.GetString(PlasticLocalization.Name.TypeDescription);
        }

        VisualElement mEditionPanel;
        VisualElement mDescriptionRow;
        TextField mDescriptionEdit;
        TextField mConditionEdit;
        Toggle mDescriptionToggle;
        Label mDescriptionLabel;
        Label mConditionLabel;
        ColorField mColorField;
        Button mEditButton;
        Button mDeleteButton;
        Toggle mAddChildBranches;
        Toggle mAddParentBranches;
        Toggle mAddBranchesSource;
        Toggle mAddBranchesDestination;

        VisualElement mParentPanel;

        bool mbInitialized;
        readonly RuleData mRuleData;
        readonly Action mOnRuleChanged;
        readonly Action mOnRuleColorChanged;
    }
}
