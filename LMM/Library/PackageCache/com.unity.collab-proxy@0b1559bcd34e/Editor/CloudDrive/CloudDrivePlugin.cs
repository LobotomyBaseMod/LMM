using Codice.Client.Common;
using Codice.LogWrapper;
using PlasticGui;
using Unity.PlasticSCM.Editor.AssetMenu;
using Unity.PlasticSCM.Editor.Configuration;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.CloudDrive
{
    internal class CloudDrivePlugin
    {
        internal static CloudDrivePlugin Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new CloudDrivePlugin();

                return mInstance;
            }
        }

        internal static void InitializeIfNeeded()
        {
            if (!ToolConfig.EnableCloudDriveTokenExists())
                return;

            new DelayedActionBySecondsRunner(
                    Instance.Enable,
                    UnityConstants.PLUGIN_DELAYED_INITIALIZE_INTERVAL)
                .Run();
        }

        internal bool IsEnabled()
        {
            return mIsEnabled;
        }

        internal bool HasRunningOperation()
        {
            CloudDriveWindow window = GetWindowIfOpened.CloudDrive();

            if (window == null)
                return false;

            if (window.CloudWorkspacesView == null)
                return false;

            return window.CloudWorkspacesView.IsOperationRunning();
        }

        internal void Enable()
        {
            if (mIsEnabled)
                return;

            mIsEnabled = true;

            PlasticApp.InitializeIfNeeded();

            mLog.Debug("Enable");

            PlasticApp.Enable();

            CloudDriveMenuItem.AddMenuItem();

            ProjectViewCloudDriveAssetMenu.AddMenuItem();
        }

        internal void Disable()
        {
            if (!mIsEnabled)
                return;

            mLog.Debug("Disable");

            mIsEnabled = false;

            PlasticApp.DisposeIfNeeded();

            CloudDriveMenuItem.RemoveMenuItem();

            ProjectViewCloudDriveAssetMenu.RemoveMenuItem();
        }

        internal void Shutdown()
        {
            mLog.Debug("Shutdown");

            Disable();
        }

        internal void OnApplicationActivated()
        {
            mLog.Debug("OnApplicationActivated");

            CloudDriveWindow window = GetWindowIfOpened.CloudDrive();

            if (window == null)
                return;

            window.OnApplicationActivated();
        }

        internal bool OnEditorWantsToQuit()
        {
            mLog.Debug("OnEditorWantsToQuit");

            if (!HasRunningOperation())
                return true;

            return GuiMessage.ShowQuestion(
                PlasticLocalization.Name.OperationRunning.GetString(),
                PlasticLocalization.Name.ConfirmClosingRunningOperation.GetString(),
                PlasticLocalization.Name.YesButton.GetString());
        }

        CloudDrivePlugin()
        {
        }

        static CloudDrivePlugin mInstance;

        bool mIsEnabled;

        static readonly ILog mLog = PlasticApp.GetLogger("CloudDrivePlugin");
    }
}