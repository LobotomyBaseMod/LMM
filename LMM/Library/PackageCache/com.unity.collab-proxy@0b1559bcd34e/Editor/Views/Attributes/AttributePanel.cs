using UnityEngine;
using UnityEditor;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Attributes;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.Properties;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.Attributes
{
    internal class AttributePanel : AttributePanelOperations.IAttributePanel
    {
        internal interface IAttributesPanel
        {
            internal void RemovePanel(AttributePanel attributePanel);
        }

        internal Vector2 DesiredSize  => mDesiredSize;

        internal AttributePanel(
            RepositorySpec repSpec,
            AttributeRealizationInfo attribute,
            IProgressControls progressControls,
            IAttributesPanel attributesPanel,
            IWorkspaceWindow workspaceWindow,
            EditorWindow window)
        {
            mRepSpec = repSpec;
            mAttribute = attribute;
            mProgressControls = progressControls;
            mAttributesPanel = attributesPanel;
            mWorkspaceWindow = workspaceWindow;
            mWindow = window;

            Color solidColor = AttributeColor.FromAttributeName(mAttribute.AttributeName);
            mNameForegroundColor = solidColor;
            mOutlineColor = new Color(solidColor.r, solidColor.g, solidColor.b, 0.5f);;
            mFillColor = new Color(solidColor.r, solidColor.g, solidColor.b, 0.1f);

            CalculateTextAndDesiredSize();
        }

        internal void Update()
        {
            if (!mbEditButtonClicked)
                return;

            mbEditButtonClicked = false;

            EditButton_Click();
        }

        internal void OnGUI(float availableWidth)
        {
            GUIStyle labelStyle = UnityStyles.AttributesPanel.AttributeLabel;
            GUIStyle editButtonStyle= UnityStyles.AttributesPanel.AttributeLabelButton;
            GUIStyle removeButtonStyle= UnityStyles.AttributesPanel.AttributeLabelRightButton;

            float buttonsWidth = editButtonStyle.fixedWidth +
                                 removeButtonStyle.fixedWidth +
                                 editButtonStyle.margin.horizontal +
                                 removeButtonStyle.margin.horizontal;

            string cutText = CutText(
                mPlainText,
                mRichText,
                labelStyle,
                availableWidth - buttonsWidth,
                mNameForegroundColor,
                out Vector2 textSize);

            Rect totalRect = GUILayoutUtility.GetRect(
                0,
                0,
                GUILayout.Width(textSize.x + buttonsWidth + labelStyle.margin.horizontal),
                GUILayout.Height(textSize.y + labelStyle.margin.vertical));

            Rect boxRect = new Rect(
                totalRect.x + labelStyle.margin.left,
                totalRect.y + labelStyle.margin.top,
                totalRect.width - labelStyle.margin.horizontal,
                totalRect.height - labelStyle.margin.vertical);

            Rect editButtonRect = new Rect(
                boxRect.x + textSize.x,
                boxRect.y + (boxRect.height - editButtonStyle.fixedHeight) / 2,
                editButtonStyle.fixedWidth,
                editButtonStyle.fixedHeight);

            Rect removeButtonRect = new Rect(
                boxRect.x + textSize.x + editButtonRect.width,
                boxRect.y + (boxRect.height - removeButtonStyle.fixedHeight) / 2,
                removeButtonStyle.fixedWidth,
                removeButtonStyle.fixedHeight);

            UnityEditor.EditorGUI.DrawRect(boxRect, mFillColor);
            EditorGUI.DrawOutline(boxRect, 1, mOutlineColor);

            GUI.Label(
                boxRect,
                new GUIContent(cutText, cutText != mRichText ? mRichText : string.Empty),
                labelStyle);

            if (GUI.Button(
                    editButtonRect,
                    Images.GetEditIcon(),
                    editButtonStyle))
            {
                mbEditButtonClicked = true;
            }

            if (GUI.Button(
                    removeButtonRect,
                    Images.GetCloseIcon(),
                    removeButtonStyle))
            {
                RemoveButton_Click();
            }
        }

        void AttributePanelOperations.IAttributePanel.Fill(string[] attValues)
        {
        }

        void AttributePanelOperations.IAttributePanel.DisableEditionMode()
        {
            mAttribute.Value = mEditAttributeValue;
            CalculateTextAndDesiredSize();
            PropertiesRefreshNotifier.Notify();
        }

        void AttributePanelOperations.IAttributePanel.Remove()
        {
            mAttributesPanel.RemovePanel(this);
            PropertiesRefreshNotifier.Notify();
        }

        void EditButton_Click()
        {
            ApplyAttributeData applyAttributeData = AttributeDataDialog.BuildForEditAttributeRealization(
                mAttribute, mRepSpec, mWorkspaceWindow, mWindow);

            if (!applyAttributeData.Result)
                return;

            mEditAttributeValue = applyAttributeData.AttributeValue;

            AttributePanelOperations.ChangeAttributeValue(
                mRepSpec, mAttribute, applyAttributeData.AttributeValue,
                mProgressControls, this);
        }

        void RemoveButton_Click()
        {
            AttributePanelOperations.DeleteAttribute(
                mRepSpec, mAttribute, mProgressControls, this);
        }

        void CalculateTextAndDesiredSize()
        {
            GUIStyle labelStyle = UnityStyles.AttributesPanel.AttributeLabel;
            GUIStyle editButtonStyle = UnityStyles.AttributesPanel.AttributeLabelButton;
            GUIStyle removeButtonStyle = UnityStyles.AttributesPanel.AttributeLabelRightButton;

            mPlainText = string.Format("{0}: {1}", mAttribute.AttributeName, mAttribute.Value);
            mRichText = string.Format(
                "<b><color={0}>{1}:</color></b> {2}",
                GetHexString(mNameForegroundColor),
                mAttribute.AttributeName,
                mAttribute.Value);
            Vector2 desiredTextSize = labelStyle.CalcSize(new GUIContent(mRichText));

            mDesiredSize =
                desiredTextSize +
                new Vector2(
                    labelStyle.margin.horizontal +
                    editButtonStyle.fixedWidth +
                    removeButtonStyle.fixedWidth +
                    editButtonStyle.margin.horizontal +
                    removeButtonStyle.margin.horizontal,
                    labelStyle.margin.vertical);
        }

        static string CutText(
            string plainText,
            string richText,
            GUIStyle style,
            float availableWidth,
            Color nameForegroundColor,
            out Vector2 usedSize)
        {
            usedSize = new Vector2();

            availableWidth -= style.margin.horizontal;

            Vector2 fullTextSize = style.CalcSize(new GUIContent(richText));

            if (fullTextSize.x < availableWidth)
            {
                usedSize = fullTextSize;
                return richText;
            }

            string textResult = string.Format("<b><color={0}>", GetHexString(nameForegroundColor));
            bool bReachedAttributeValue = false;

            foreach (char c in plainText)
            {
                Vector2 currentSize = style.CalcSize(
                    new GUIContent(
                        textResult + c + ELLIPSIS + (!bReachedAttributeValue ? "</color></b>" : string.Empty)));

                if (currentSize.x >= availableWidth)
                    return textResult + ELLIPSIS + (!bReachedAttributeValue ? "</color></b>" : string.Empty);

                usedSize = currentSize;
                textResult += c;

                if (!bReachedAttributeValue && c == ':')
                {
                    bReachedAttributeValue = true;
                    textResult += "</color></b>";
                }
            }

            return textResult + ELLIPSIS;
        }

        static string GetHexString(Color color)
        {
            return string.Format(
                "#{0}{1}{2}{3}",
                ((int)(color.r * 255)).ToString("X2"),
                ((int)(color.g * 255)).ToString("X2"),
                ((int)(color.b * 255)).ToString("X2"),
                ((int)(color.a * 255)).ToString("X2"));
        }

        string mRichText;
        string mPlainText;
        Vector2 mDesiredSize;
        string mEditAttributeValue;
        bool mbEditButtonClicked;

        readonly Color mNameForegroundColor;
        readonly Color mOutlineColor;
        readonly Color mFillColor;

        readonly RepositorySpec mRepSpec;
        readonly AttributeRealizationInfo mAttribute;
        readonly IProgressControls mProgressControls;
        readonly IAttributesPanel mAttributesPanel;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly EditorWindow mWindow;

        const string ELLIPSIS = "...";
    }
}
