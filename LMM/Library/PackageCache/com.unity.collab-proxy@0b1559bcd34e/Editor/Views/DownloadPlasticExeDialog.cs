using System.Linq;
using System.Threading;

using UnityEngine;
using UnityEngine.UIElements;

using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.CM.Common;
using Codice.Utils;
using PlasticGui;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.UIElements;
using Unity.PlasticSCM.Editor.Views.Welcome;

namespace Unity.PlasticSCM.Editor.Views
{
    internal class DownloadPlasticExeDialog : PlasticDialog, DownloadAndInstallOperation.INotify
    {
        internal bool IsPlasticInstalling { get { return mIsPlasticInstalling; } }

        protected override Rect DefaultRect
        {
            get
            {
                var baseRect = base.DefaultRect;
                return new Rect(baseRect.x, baseRect.y, DIALOG_WIDTH, DIALOG_HEIGHT);
            }
        }

        internal static ProgressControlsForDialogs.Data Show(
            RepositorySpec repSpec,
            bool isGluonMode,
            string installCloudFrom,
            string installEnterpriseFrom,
            string cancelInstallFrom,
            ProgressControlsForDialogs.Data progressData)
        {
            DownloadPlasticExeDialog dialog;

            if (HasOpenInstances<DownloadPlasticExeDialog>())
            {
                dialog = GetWindow<DownloadPlasticExeDialog>(true);
            }
            else
            {
                dialog = Create(repSpec, isGluonMode, installCloudFrom, installEnterpriseFrom, cancelInstallFrom, progressData);

                dialog.RunUtility(focusedWindow);
            }

            return progressData != null ? progressData : dialog.mProgressControls.ProgressData;
        }

        static DownloadPlasticExeDialog Create(
            RepositorySpec repSpec,
            bool isGluonMode,
            string installCloudFrom,
            string installEnterpriseFrom,
            string cancelInstallFrom,
            ProgressControlsForDialogs.Data progressData)
        {
            var instance = CreateInstance<DownloadPlasticExeDialog>();
            instance.SetSizeToContent(SizeToContent.Manual);
            instance.mRepSpec = repSpec;
            instance.mInstallCloudFrom = installCloudFrom;
            instance.mInstallEnterpriseFrom = installEnterpriseFrom;
            instance.mCancelInstallFrom = cancelInstallFrom;
            instance.mIsGluonMode = isGluonMode;
            instance.mProgressData = progressData;
            instance.mIsCloudEdition = EditionToken.IsCloudEdition();
            instance.mTitle = PlasticLocalization.Name.UnityVersionControl.GetString();
            instance.mOkButtonText = string.Empty;
            instance.mCancelButtonText = string.Empty;
            return instance;
        }

        protected override string GetTitle()
        {
            return mTitle;
        }

        void OnEnable()
        {
            BuildComponents();
        }

        void OnDestroy()
        {
            Dispose();
        }

        protected override void DoComponentsArea()
        {
            if (InstallationFinished())
            {
                mMessageLabel.text = PlasticLocalization.Name.UnityVersionControlInstalled.GetString();
                mCancelButton.text = PlasticLocalization.Name.CloseButton.GetString();
                mConfirmMessageLabel.Collapse();
                mDownloadButton.Collapse();

                maxSize = new Vector2(DIALOG_WIDTH, REDUCED_DIALOG_HEIGHT);
                minSize = maxSize;

                return;
            }

            if (mProgressData != null)
            {
                if (mProgressControlsContainer.Children().OfType<ProgressControlsForDialogs>().Any())
                {
                    mProgressControlsContainer.RemoveAt(0);
                    mProgressLabel = new Label(GetMessageFromProgressData(mProgressData));
                    mProgressControlsContainer.Add(mProgressLabel);
                }

                mProgressLabel.text = GetMessageFromProgressData(mProgressData);
                UpdateButtonsStatuses();
            }
        }

        void CancelButton_Clicked()
        {
            if (!IsExeAvailable.ForMode(mIsGluonMode))
                TrackFeatureUseEvent.For(mRepSpec, mCancelInstallFrom);

            if (mDownloadCancellationTokenSource != null)
            {
                mDownloadCancellationTokenSource.Cancel();
                mDownloadCancellationTokenSource = null;
                return;
            }

            Close();
        }

        void DownloadButton_Clicked()
        {
            mDownloadCancellationTokenSource = new CancellationTokenSource();

            if (mIsCloudEdition)
            {
                TrackFeatureUseEvent.For(mRepSpec, mInstallCloudFrom);

                DownloadAndInstallOperation.Run(
                    Edition.Cloud,
                    mProgressControls,
                    mDownloadCancellationTokenSource.Token,
                    this);
            }
            else
            {
                TrackFeatureUseEvent.For(mRepSpec, mInstallEnterpriseFrom);

                DownloadAndInstallOperation.Run(
                    Edition.Enterprise,
                    mProgressControls,
                    mDownloadCancellationTokenSource.Token,
                    this);
            }
        }

        void DownloadAndInstallOperation.INotify.InstallationStarted()
        {
            mCancelButton.SetEnabled(false);
            mIsPlasticInstalling = true;
        }

        void DownloadAndInstallOperation.INotify.InstallationFinished()
        {
            mCancelButton.SetEnabled(true);
            mIsPlasticInstalling = false;
        }

        void DownloadAndInstallOperation.INotify.DownloadStarted()
        {
        }

        void DownloadAndInstallOperation.INotify.DownloadFinished()
        {
        }

        bool InstallationFinished()
        {
            return mCancelButton.enabledSelf && IsExeAvailable.ForMode(mIsGluonMode);
        }

        void UpdateButtonsStatuses()
        {
            if (!string.IsNullOrEmpty(mProgressData.StatusMessage))
            {
                mCancelButton.SetEnabled(true);
                mCancelButton.text = PlasticLocalization.Name.CloseButton.GetString();
                mDownloadButton.Collapse();
                return;
            }

            if (IsExeAvailable.ForMode(mIsGluonMode))
            {
                mCancelButton.SetEnabled(true);
                return;
            }

            mDownloadButton.SetEnabled(false);
            mCancelButton.SetEnabled(false);
        }

        static string GetMessageFromProgressData(ProgressControlsForDialogs.Data data)
        {
            if (!string.IsNullOrEmpty(data.StatusMessage))
            {
                return data.StatusMessage;
            }
            else
            {
                if (data.ProgressPercent >= 0)
                    return string.Format("{0} ({1}%)", data.ProgressMessage, (int)(data.ProgressPercent * 100));
                else
                    return data.ProgressMessage;
            }
        }

        void Dispose()
        {
            mDownloadButton.clicked -= DownloadButton_Clicked;
            mCancelButton.clicked -= CancelButton_Clicked;
        }

        void BuildComponents()
        {
            VisualElement root = rootVisualElement;
            root.Clear();

            InitializeLayoutAndStyles();

            mMessageLabel = root.Q<Label>("title");
            mConfirmMessageLabel = root.Q<Label>("message");
            mDownloadButton = root.Q<Button>("downloadButton");
            mCancelButton = root.Q<Button>("cancelButton");
            mProgressControlsContainer = root.Q<VisualElement>("progressControlsContainer");

            mMessageLabel.text = PlasticLocalization.Name.InstallUnityVersionControl.GetString();
            mConfirmMessageLabel.text = PlasticLocalization.Name.InstallConfirmationMessage.GetString();
            mDownloadButton.text = PlasticLocalization.Name.DownloadAndInstall.GetString();
            mDownloadButton.clicked += DownloadButton_Clicked;
            mCancelButton.text = PlasticLocalization.Name.CancelButton.GetString();
            mCancelButton.clicked += CancelButton_Clicked;

            mProgressControls = new ProgressControlsForDialogs(
                new VisualElement[] { mDownloadButton });

            mProgressControlsContainer.Add(mProgressControls);

            SwapButtonsOrderIfMacOS(
                root,
                mDownloadButton,
                mCancelButton);
        }

        void InitializeLayoutAndStyles()
        {
            rootVisualElement.LoadLayout(typeof(DownloadPlasticExeDialog).Name);
            rootVisualElement.LoadStyle(typeof(DownloadPlasticExeDialog).Name);
        }

        static void SwapButtonsOrderIfMacOS(
            VisualElement root,
            Button downloadButton,
            Button cancelButton)
        {
            if (!PlatformIdentifier.IsMac())
                return;

            var footer = root.Q<VisualElement>("footer");

            var cancelButtonIndex = footer.IndexOf(cancelButton);
            var downloadButtonIndex = footer.IndexOf(downloadButton);

            // Remove and re-insert in swapped order
            footer.Remove(downloadButton);
            footer.Remove(cancelButton);

            footer.Insert(downloadButtonIndex, cancelButton);
            footer.Insert(cancelButtonIndex, downloadButton);
        }

        string mTitle;

        Label mMessageLabel;
        Label mConfirmMessageLabel;
        Label mProgressLabel;
        Button mDownloadButton;
        Button mCancelButton;
        new ProgressControlsForDialogs mProgressControls;
        VisualElement mProgressControlsContainer;

        RepositorySpec mRepSpec;
        string mInstallCloudFrom;
        string mInstallEnterpriseFrom;
        string mCancelInstallFrom;
        bool mIsCloudEdition;
        bool mIsGluonMode;
        bool mIsPlasticInstalling;
        ProgressControlsForDialogs.Data mProgressData;
        CancellationTokenSource mDownloadCancellationTokenSource;

        const float DIALOG_WIDTH = 500;
        const float DIALOG_HEIGHT = 250;
        const float REDUCED_DIALOG_HEIGHT = 125;
    }
}
