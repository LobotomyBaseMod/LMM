using System;
using System.Collections.Generic;
using System.Text;

using UnityEditor;
using UnityEngine;

using Codice.CM.Common.Checkin.Partial;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs
{
    internal class CheckinConflictsDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 600, 418);
            }
        }

        internal static ResponseType Show(
            IList<CheckinConflict> conflicts,
            PlasticLocalization.Name dialogTitle,
            PlasticLocalization.Name dialogExplanation,
            PlasticLocalization.Name okButtonCaption,
            EditorWindow parentWindow)
        {
            CheckinConflictsDialog dialog = Create(
                PlasticLocalization.GetString(dialogTitle),
                PlasticLocalization.GetString(dialogExplanation),
                GetConflictsText(conflicts),
                PlasticLocalization.GetString(okButtonCaption));
            return dialog.RunModal(parentWindow);
        }

        protected override void DoComponentsArea()
        {
            Title(PlasticLocalization.GetString(PlasticLocalization.Name.ItemColumn));

            mScrollPosition = EditorGUILayout.BeginScrollView(
                   mScrollPosition, EditorStyles.helpBox, GUILayout.Height(205));

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                Paragraph(mConflictsText);
            }

            EditorGUILayout.EndScrollView();
        }

        protected override string GetTitle()
        {
            return mDialogTitle;
        }

        protected override string GetExplanation()
        {
            return mDialogExplanation;
        }

        static string GetConflictsText(IList<CheckinConflict> conflicts)
        {
            StringBuilder sb = new StringBuilder();

            foreach (CheckinConflict conflict in conflicts)
            {
                sb.AppendFormat(
                    "{0} {1}{2}",
                    conflict.Description,
                    conflict.ActionMessage,
                    Environment.NewLine);
            }

            return sb.ToString();
        }

        static CheckinConflictsDialog Create(
            string dialogTitle,
            string dialogExplanation,
            string conflictsText,
            string okButtonText)
        {
            var instance = CreateInstance<CheckinConflictsDialog>();
            instance.mDialogTitle = dialogTitle;
            instance.mDialogExplanation = dialogExplanation;
            instance.mConflictsText = conflictsText;
            instance.mOkButtonText = okButtonText;
            instance.mEnterKeyAction = instance.OkButtonAction;
            instance.mEscapeKeyAction = instance.CancelButtonAction;
            return instance;
        }

        Vector2 mScrollPosition;

        string mDialogTitle;
        string mDialogExplanation;
        string mConflictsText;
    }
}
