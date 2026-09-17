using UnityEditor;
using UnityEngine;

using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using PlasticGui;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views
{
    internal class EnableSwitchAndShelveFeature :
        SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog
    {
        internal EnableSwitchAndShelveFeature(RepositorySpec repSpec, EditorWindow window)
        {
            mRepSpec = repSpec;
            mWindow = window;
        }

        bool SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog.Show()
        {
            bool result = false;

            GUIActionRunner.RunGUIAction(() =>
            {
                result = EnableSwitchAndShelveFeatureDialog.Show(mRepSpec, mWindow);
            });

            return result;
        }

        readonly EditorWindow mWindow;
        readonly RepositorySpec mRepSpec;
    }

    internal class EnableSwitchAndShelveFeatureDialog : PlasticDialog
    {
        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, 600, 320);
            }
        }

        internal static bool Show(RepositorySpec repSpec, EditorWindow window)
        {
            EnableSwitchAndShelveFeatureDialog dialog = CreateInstance<EnableSwitchAndShelveFeatureDialog>();
            dialog.mRepSpec = repSpec;
            dialog.mOkButtonText = PlasticLocalization.Name.EnableSwitchAndShelveYesEnableItLowerCase.GetString();
            dialog.mCancelButtonText = PlasticLocalization.Name.EnableSwitchAndShelveNotNow.GetString();
            ResponseType dialogResult = dialog.RunModal(window);
            return dialogResult == ResponseType.Ok;
        }

        protected override string GetTitle()
        {
            return PlasticLocalization.Name.EnableSwitchAndShelveTitle.GetString();
        }

        protected override string GetExplanation()
        {
            return PlasticLocalization.Name.EnableSwitchAndShelveMessage.GetString();
        }

        protected override void DoComponentsArea()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(20);
                using (new EditorGUILayout.VerticalScope())
                {
                    Paragraph(string.Concat(
                        PlasticLocalization.Name.EnableSwitchAndShelveLeaveChangesTitle.GetString(), "\n",
                        PlasticLocalization.Name.EnableSwitchAndShelveLeaveChangesDescription.GetString()));

                    Paragraph(string.Concat(
                        PlasticLocalization.Name.EnableSwitchAndShelveBringChangesTitle.GetString(), "\n",
                        PlasticLocalization.Name.EnableSwitchAndShelveBringChangesDescription.GetString()));
                }

                GUILayout.FlexibleSpace();
            }

            Paragraph(string.Concat(
                PlasticLocalization.Name.EnableSwitchAndShelveQuestionStart.GetString(), "\n",
                PlasticLocalization.Name.EnableSwitchAndShelveQuestionEnd.GetString()));
        }

        internal override void OkButtonAction()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.SwitchAndShelve.EnableFeatureYes);

            base.OkButtonAction();
        }

        internal override void CancelButtonAction()
        {
            TrackFeatureUseEvent.For(
                mRepSpec,
                TrackFeatureUseEvent.Features.SwitchAndShelve.EnableFeatureNo);

            base.CancelButtonAction();
        }

        RepositorySpec mRepSpec;
    }
}
