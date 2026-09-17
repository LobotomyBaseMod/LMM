using UnityEditor;
using UnityEngine;

using Codice.Client.Commands;
using Codice.CM.Common;
using PlasticGui;
using PlasticGui.WorkspaceWindow.PendingChanges.Changelists;
using Unity.PlasticSCM.Editor.UI;

#if !UNITY_6000_3_OR_NEWER
using EditorGUI = Unity.PlasticSCM.Editor.UnityInternals.UnityEditor.EditorGUI;
#endif

namespace Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs
{
    class CreateChangelistDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 710, 290);
            }
        }

        protected override string GetTitle()
        {
            return (mIsCreateMode ?
                PlasticLocalization.Name.CreateChangelistTitle :
                PlasticLocalization.Name.EditChangelistTitle).GetString();
        }

        protected override string GetExplanation()
        {
            return (mIsCreateMode ?
                PlasticLocalization.Name.CreateChangelistExplanation :
                PlasticLocalization.Name.EditChangelistExplanation).GetString();
        }

        protected override void DoComponentsArea()
        {
            DoNameFieldArea();

            GUILayout.Space(5);

            DoDescriptionFieldArea();

            GUILayout.Space(5);

            DoPersistentFieldArea();
        }

        internal static ChangelistCreationData CreateChangelist(
            WorkspaceInfo wkInfo,
            EditorWindow parentWindow)
        {
            CreateChangelistDialog dialog = Create(wkInfo);
            ResponseType dialogueResult = dialog.RunModal(parentWindow);

            ChangelistCreationData result = dialog.BuildCreationData();
            result.Result = dialogueResult == ResponseType.Ok;
            return result;
        }

        internal static ChangelistCreationData EditChangelist(
            WorkspaceInfo wkInfo,
            ChangeListInfo changelistToEdit,
            EditorWindow parentWindow)
        {
            CreateChangelistDialog dialog = Edit(wkInfo, changelistToEdit);
            ResponseType dialogueResult = dialog.RunModal(parentWindow);

            ChangelistCreationData result = dialog.BuildCreationData();
            result.Result = dialogueResult == ResponseType.Ok;
            return result;
        }

        void DoNameFieldArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    PlasticLocalization.GetString(PlasticLocalization.Name.ChangelistNameEntry),
                    GUILayout.Width(100));

                Rect nameRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textField,
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(NAME_FIELD_CONTROL_NAME);
                mChangelistName = UnityEditor.EditorGUI.TextField(nameRect, mChangelistName);

                FocusTextAreaIfNeeded(NAME_FIELD_CONTROL_NAME);

                GUILayout.Space(5);
            }
        }

        void DoDescriptionFieldArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(100)))
                {
                    GUILayout.Space(49);
                    GUILayout.Label(
                        PlasticLocalization.GetString(PlasticLocalization.Name.ChangelistDescriptionEntry),
                        GUILayout.Width(100));
                }

                Rect descriptionRect = GUILayoutUtility.GetRect(
                    new GUIContent(string.Empty),
                    EditorStyles.textArea,
                    GUILayout.Height(100),
                    GUILayout.ExpandWidth(true));

                GUI.SetNextControlName(DESCRIPTION_TEXTAREA_CONTROL_NAME);
                mChangelistDescription = EditorGUI.ScrollableTextAreaInternal(
                    descriptionRect,
                    mChangelistDescription,
                    ref mScrollPosition,
                    EditorStyles.textArea);

                GUILayout.Space(5);
            }
        }

        void DoPersistentFieldArea()
        {
            mIsPersistent = GUILayout.Toggle(
                mIsPersistent,
                PlasticLocalization.GetString(PlasticLocalization.Name.ChangelistPersistentCheckBoxEntry));
        }

        internal override void OkButtonAction()
        {
            ChangelistCreationValidation.Validation(
                mWkInfo,
                mChangelistName,
                mIsCreateMode || !mChangelistName.Equals(mChangelistToEdit.Name),
                this,
                mProgressControls);
        }

        static CreateChangelistDialog Create(WorkspaceInfo wkInfo)
        {
            var instance = CreateInstance<CreateChangelistDialog>();
            instance.IsResizable = false;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.AddControlConsumingEnterKey(DESCRIPTION_TEXTAREA_CONTROL_NAME);
            instance.mEscapeKeyAction = instance.CloseButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.CreateButton.GetString();
            instance.mWkInfo = wkInfo;
            instance.mChangelistToEdit = null;
            instance.mChangelistName = string.Empty;
            instance.mChangelistDescription = string.Empty;
            instance.mIsPersistent = false;
            instance.mIsCreateMode = true;
            return instance;
        }

        static CreateChangelistDialog Edit(
            WorkspaceInfo wkInfo,
            ChangeListInfo changelistToEdit)
        {
            var instance = CreateInstance<CreateChangelistDialog>();
            instance.IsResizable = false;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.AddControlConsumingEnterKey(DESCRIPTION_TEXTAREA_CONTROL_NAME);
            instance.mEscapeKeyAction = instance.CloseButtonAction;
            instance.mOkButtonText = PlasticLocalization.Name.EditButton.GetString();
            instance.mWkInfo = wkInfo;
            instance.mChangelistToEdit = changelistToEdit;
            instance.mChangelistName = changelistToEdit.Name;
            instance.mChangelistDescription = changelistToEdit.Description;
            instance.mIsPersistent = changelistToEdit.IsPersistent;
            instance.mIsCreateMode = false;
            return instance;
        }

        Vector2 mScrollPosition;

        ChangelistCreationData BuildCreationData()
        {
            ChangeListInfo changelistInfo = new ChangeListInfo();
            changelistInfo.Name = mChangelistName;
            changelistInfo.Description = mChangelistDescription;
            changelistInfo.IsPersistent = mIsPersistent;
            changelistInfo.Type = ChangeListType.UserDefined;

            return new ChangelistCreationData(changelistInfo);
        }

        WorkspaceInfo mWkInfo;
        ChangeListInfo mChangelistToEdit;

        string mChangelistName;
        string mChangelistDescription;
        bool mIsPersistent;

        bool mIsCreateMode;

        const string NAME_FIELD_CONTROL_NAME = "CreateChangelistNameField";
        const string DESCRIPTION_TEXTAREA_CONTROL_NAME = "CreateChangelistDescriptionTextArea";
    }
}
