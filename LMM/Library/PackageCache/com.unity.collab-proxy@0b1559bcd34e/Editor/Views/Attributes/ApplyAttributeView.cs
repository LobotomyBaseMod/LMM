using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Attributes;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Attributes
{
    internal class ApplyAttributeView : ApplyAttributeDialogOperations.IApplyAttributeDialog
    {
        internal string SelectedAttributeName => mSelectedAttributeName;
        internal string SelectedAttributeValue => mSelectedAttributeValue;

        internal ApplyAttributeView(
            RepositorySpec repSpec,
            IProgressControls progressControls,
            AttributeDataDialog parentWindow)
        {
            mRepSpec = repSpec;
            mProgressControls = progressControls;
            mParentWindow = parentWindow;
        }

        void AttributeNameComboBox_Changed(object selectedAttributeName)
        {
            mSelectedAttributeName = (string)selectedAttributeName;

            ApplyAttributeDialogOperations.GetValuesForAttribute(
                mRepSpec,
                (string)selectedAttributeName,
                mProgressControls,
                this);
        }

        internal void SetAttributeToEdit(AttributeRealizationInfo attribute)
        {
            mAttributeToEdit = attribute;

            AddAttribute(new AttributeInfo()
            {
                Name = attribute.AttributeName,
                FIdAcl = attribute.IdAttribute
            });
        }

        void ApplyAttributeDialogOperations.IApplyAttributeDialog.AddAttribute(
            AttributeInfo attrInfo)
        {
            AddAttribute(attrInfo);
        }

        void ApplyAttributeDialogOperations.IApplyAttributeDialog.FillAttributes(IList attributes)
        {
            mAttributeNames = new List<string>();

            foreach (AttributeInfo attribute in attributes)
                mAttributeNames.Add(attribute.Name);

            if (attributes.Count > 0)
            {
                mSelectedAttributeName = ((AttributeInfo)attributes[0]).Name;

                ApplyAttributeDialogOperations.GetValuesForAttribute(
                    mRepSpec, mSelectedAttributeName, mProgressControls, this);
            }
        }

        void ApplyAttributeDialogOperations.IApplyAttributeDialog.FillValues(IList values)
        {
            mAttributeValues = values.Cast<string>().ToList();

            if (mAttributeToEdit != null)
            {
                mSelectedAttributeValue = mAttributeToEdit.Value;
                return;
            }

            if (values.Count > 0)
                mSelectedAttributeValue = (string)values[0];
        }

        internal void OkButtonAction()
        {
            ApplyAttributeData applyAttributeData = new ApplyAttributeData(
                mSelectedAttributeName,
                mSelectedAttributeValue);

            ApplyAttributeValidation.Validation(applyAttributeData, mParentWindow, mProgressControls);
        }

        void NewAttributeButton_Clicked()
        {
            mParentWindow.ToggleCreateAttributeView(true);
        }

        void AddAttribute(AttributeInfo attrInfo)
        {
            mAttributeNames.Add(attrInfo.Name);

            mSelectedAttributeName = attrInfo.Name;

            ApplyAttributeDialogOperations.GetValuesForAttribute(
                mRepSpec, attrInfo.Name, mProgressControls, this);

            mParentWindow.ToggleCreateAttributeView(false);
        }

        internal void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DoAttributeNamesCombo();

                if (mAttributeNames == null && GUILayout.Button(PlasticLocalization.Name.New.GetString(), GUILayout.ExpandWidth(false)))
                {
                    NewAttributeButton_Clicked();
                }
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.Name.AttributeValueLabel.GetString(),
                    GUILayout.Width(LABEL_WIDTH));

                Rect valueRect = GUILayoutUtility.GetRect(
                    GUIContent.none,
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                mSelectedAttributeValue = DropDownTextField.DoDropDownTextField(
                    mSelectedAttributeValue,
                    DROPDOWN_CONTROL_NAME,
                    mAttributeValues,
                    (selectedAttributeValue) =>
                    {
                        mSelectedAttributeValue = (string)selectedAttributeValue;
                    },
                    valueRect);
            }
        }

        void DoAttributeNamesCombo()
        {
            GUI.enabled = mAttributeToEdit == null;

            GUILayout.Label(
                PlasticLocalization.Name.AttributeNameLabel.GetString(),
                GUILayout.Width(LABEL_WIDTH));

            Rect rect = GUILayoutUtility.GetRect(
                new GUIContent(), EditorStyles.popup, GUILayout.ExpandWidth(true));

            if (GUI.Button(rect, mSelectedAttributeName, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                foreach (string attributeName in mAttributeNames)
                {
                    menu.AddItem(
                        new GUIContent(UnityMenuItem.EscapedText(attributeName)),
                        false,
                        AttributeNameComboBox_Changed,
                        attributeName);
                }

                menu.DropDown(rect);
            }

            GUI.enabled = true;
        }

        AttributeRealizationInfo mAttributeToEdit;

        string mSelectedAttributeName;
        List<string> mAttributeNames = new List<string>();
        string mSelectedAttributeValue;
        List<string> mAttributeValues = new List<string>();

        readonly RepositorySpec mRepSpec;
        readonly IProgressControls mProgressControls;
        readonly AttributeDataDialog mParentWindow;

        const float LABEL_WIDTH = 100f;
        const string DROPDOWN_CONTROL_NAME = "ApplyAttributeDialog.AttributeValueDropdown";
    }
}
