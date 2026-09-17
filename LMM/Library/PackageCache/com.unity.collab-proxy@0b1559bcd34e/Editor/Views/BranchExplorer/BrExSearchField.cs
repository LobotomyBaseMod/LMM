using System;
using Codice.Utils;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer
{
    internal class BrExSearchField : VisualElement
    {
        internal string Text
        {
            get { return mSearchField.value; }
            set { mSearchField.value = value; }
        }

        internal event Action DelayedSearchTextChanged;
        internal event Action NextSearchResultRequested;
        internal event Action PreviousSearchResultRequested;

        internal BrExSearchField()
        {
            CreateGUI();

            mSearchRunner = new DelayedActionBySecondsRunner(
                OnDelayedSearchChanged,
                UnityConstants.SEARCH_DELAYED_INPUT_ACTION_INTERVAL);
        }

        internal void Dispose()
        {
            mSearchField.UnregisterValueChangedCallback(OnSearchFieldChanged);
            mSearchField.UnregisterCallback<KeyDownEvent>(OnSearchFieldKeyDown, TrickleDown.TrickleDown);

            mPreviousButton.UnregisterCallback<MouseEnterEvent>(OnNavigationButtonMouseEnter);
            mPreviousButton.UnregisterCallback<MouseLeaveEvent>(OnNavigationButtonMouseLeave);

            mNextButton.UnregisterCallback<MouseEnterEvent>(OnNavigationButtonMouseEnter);
            mNextButton.UnregisterCallback<MouseLeaveEvent>(OnNavigationButtonMouseLeave);
        }

        internal void FocusSearchField()
        {
            mSearchField.Focus();
        }

        internal void UpdateSearchCounter(int currentIndex, int totalResults)
        {
            if (string.IsNullOrEmpty(mSearchField.value))
                return;

            if (totalResults == 0)
            {
                mSearchCounterLabel.text = "0/0";
                mPreviousButton.SetEnabled(false);
                mNextButton.SetEnabled(false);
            }
            else
            {
                mSearchCounterLabel.text = string.Format("{0}/{1}", currentIndex, totalResults);
                mPreviousButton.SetEnabled(true);
                mNextButton.SetEnabled(true);
            }
        }

        void OnDelayedSearchChanged()
        {
            DelayedSearchTextChanged?.Invoke();
        }

        void CreateGUI()
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            mSearchField = BuildSearchField();
            Add(mSearchField);

            var overlayContainer = BuildOverlayContainer();
            Add(overlayContainer);
        }

        ToolbarSearchField BuildSearchField()
        {
            var searchField = new ToolbarSearchField();
            searchField.style.width = 220;
            searchField.style.flexGrow = 1;
            searchField.tooltip = string.Format("{0}{1}{2}",
                PlasticLocalization.Name.BranchExplorerSearchFieldTooltip.GetString(),
                Environment.NewLine,
                GetSearchTextBoxShortcutsTooltipString());
            searchField.RegisterValueChangedCallback(OnSearchFieldChanged);
            searchField.RegisterCallback<KeyDownEvent>(OnSearchFieldKeyDown, TrickleDown.TrickleDown);

            var textField = searchField.Q<TextField>();
            if (textField != null)
            {
                textField.style.paddingRight = 55;
                textField.SetTextCursor();
            }

            return searchField;
        }

        VisualElement BuildOverlayContainer()
        {
            var container = new VisualElement();
            container.style.position = Position.Absolute;
            container.style.right = 18;
            container.style.top = 0;
            container.style.bottom = 0;
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.pickingMode = PickingMode.Ignore;

            mSearchCounterLabel = BuildSearchCounterLabel();
            container.Add(mSearchCounterLabel);

            mPreviousButton = CreateNavigationButton(
                OnPreviousButtonClicked,
                PlasticLocalization.Name.PreviousLabel.GetString(),
                Images.GetArrowCaretLeftIcon(),
                marginRight: 2);
            container.Add(mPreviousButton);

            mNextButton = CreateNavigationButton(
                OnNextButtonClicked,
                PlasticLocalization.Name.NextLabel.GetString(),
                Images.GetArrowCaretRightIcon());
            container.Add(mNextButton);

            return container;
        }

        Label BuildSearchCounterLabel()
        {
            var label = new Label();
            label.style.color = UnityStyles.Colors.SecondaryLabel;
            label.style.fontSize = 11;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginLeft = 4;
            label.style.marginRight = 0;
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        void OnNavigationButtonMouseEnter(MouseEnterEvent evt)
        {
            VisualElement button = (VisualElement)evt.currentTarget;
            button.style.backgroundColor = StyleKeyword.Null;
        }

        void OnNavigationButtonMouseLeave(MouseLeaveEvent evt)
        {
            VisualElement button = (VisualElement)evt.currentTarget;
            button.style.backgroundColor = Color.clear;
        }

        Button CreateNavigationButton(
            Action clickEvent,
            string tooltip,
            Texture2D image,
            float marginRight = 0)
        {
            Button result = new Button(clickEvent);

            result.tooltip = tooltip;
            result.style.backgroundImage = image;
            result.style.width = NAVIGATION_BUTTON_SIZE;
            result.style.height = NAVIGATION_BUTTON_SIZE;
            result.style.marginLeft = 0;
            result.style.marginRight = marginRight;
            result.style.paddingLeft = 0;
            result.style.paddingRight = 0;
            result.style.paddingTop = 0;
            result.style.paddingBottom = 0;
            result.style.borderLeftWidth = 0;
            result.style.borderRightWidth = 0;
            result.style.borderTopWidth = 0;
            result.style.borderBottomWidth = 0;
            result.visible = false;
            result.style.backgroundColor = Color.clear;

            result.SetMouseCursor(MouseCursor.Arrow);
            result.RegisterCallback<MouseEnterEvent>(OnNavigationButtonMouseEnter);
            result.RegisterCallback<MouseLeaveEvent>(OnNavigationButtonMouseLeave);

            return result;
        }

        void OnSearchFieldKeyDown(KeyDownEvent evt)
        {
            KeyboardActions.SearchAction searchAction =
                KeyboardActions.GetSearchAction(evt);

            if (searchAction == KeyboardActions.SearchAction.None)
                return;

            if (searchAction == KeyboardActions.SearchAction.Clean)
            {
                evt.StopPropagation();
                mSearchField.value = string.Empty;
                return;
            }

            if (searchAction == KeyboardActions.SearchAction.PreviousSearch)
            {
                evt.StopPropagation();
                PreviousSearchResultRequested?.Invoke();
                return;
            }

            if (searchAction == KeyboardActions.SearchAction.NextSearch)
            {
                evt.StopPropagation();
                NextSearchResultRequested?.Invoke();
                return;
            }
        }

        void OnSearchFieldChanged(ChangeEvent<string> evt)
        {
            mSearchRunner.Run();
            UpdateSearchControlsVisibility();
        }

        void UpdateSearchControlsVisibility()
        {
            if (string.IsNullOrEmpty(mSearchField.value))
            {
                mSearchCounterLabel.text = string.Empty;
                mSearchCounterLabel.visible = false;
                mPreviousButton.visible = false;
                mNextButton.visible = false;
                return;
            }

            mSearchCounterLabel.visible = true;
            mPreviousButton.visible = true;
            mNextButton.visible = true;
        }

        void OnNextButtonClicked()
        {
            NextSearchResultRequested?.Invoke();
        }

        void OnPreviousButtonClicked()
        {
            PreviousSearchResultRequested?.Invoke();
        }

        static string GetSearchTextBoxShortcutsTooltipString()
        {
            if (PlatformIdentifier.IsWindows())
                return PlasticLocalization.Name
                    .BranchExplorerSearchFieldShortcutsTooltipForWindows.GetString();

            if (PlatformIdentifier.IsMac())
                return PlasticLocalization.Name
                    .BranchExplorerSearchFieldShortcutsTooltipForMacOS.GetString();

            return PlasticLocalization.Name
                .BranchExplorerSearchFieldShortcutsTooltipForLinux.GetString();
        }

        ToolbarSearchField mSearchField;
        Label mSearchCounterLabel;
        Button mPreviousButton;
        Button mNextButton;

        readonly DelayedActionBySecondsRunner mSearchRunner;

        const float NAVIGATION_BUTTON_SIZE = 12f;
    }
}
