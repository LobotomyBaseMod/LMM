using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

using Codice.Client.Common.Threading;
using Codice.Client.Common.WebApi.Responses;
using Codice.CM.Common;
using Codice.Utils;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.WebApi;
using ProgressControlsForDialogs = Unity.PlasticSCM.Editor.UI.UIElements.ProgressControlsForDialogs;

namespace Unity.PlasticSCM.Editor.Views.Welcome
{
    class DownloadAndInstallOperation
    {
        internal interface INotify
        {
            void DownloadStarted();
            void DownloadFinished();
            void InstallationStarted();
            void InstallationFinished();
        }

        internal static void Run(
            Edition plasticEdition,
            IProgressControls progressControls,
            CancellationToken downloadCancellationToken,
            INotify notify)
        {
            progressControls.ShowProgress(
                PlasticLocalization.GetString(PlasticLocalization.Name.DownloadingProgress));

            notify.DownloadStarted();

            NewVersionResponse plasticVersion = null;

            string installerDestinationPath = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    plasticVersion = PlasticGui.Plastic.WebRestAPI.GetNewVersion("9.0.0.0", plasticEdition);

                    if (plasticVersion == null)
                        return;

                    installerDestinationPath = DownloadInstaller(
                        plasticVersion.GetInstallerUrl(),
                        progressControls,
                        downloadCancellationToken);

                    if (downloadCancellationToken.IsCancellationRequested)
                        return;

                    if (!PlatformIdentifier.IsMac())
                        return;

                    installerDestinationPath = UnZipMacOsPackage(
                        installerDestinationPath);
                },
                /*afterOperationDelegate*/ delegate
                {
                    notify.DownloadFinished();
                    progressControls.HideProgress();

                    if (downloadCancellationToken.IsCancellationRequested)
                        return;

                    if (waiter.Exception != null)
                    {
                        progressControls.ShowError(
                            waiter.Exception.Message);
                        return;
                    }

                    if (plasticVersion == null)
                    {
                        progressControls.ShowError(
                            PlasticLocalization.GetString(PlasticLocalization.Name.ConnectingError));
                        return;
                    }

                    if (!File.Exists(installerDestinationPath))
                        return;

                    RunInstaller(
                        installerDestinationPath,
                        progressControls,
                        notify);
                });
        }

        static void RunInstaller(
            string installerPath,
            IProgressControls progressControls,
            INotify notify)
        {
            if (progressControls is ProgressControlsForDialogs)
                ((ProgressControlsForDialogs)progressControls).ProgressData.ProgressPercent = -1;
            else if (progressControls is ProgressControlsForViews)
                ((ProgressControlsForViews)progressControls).ProgressData.ProgressPercent = -1;

            progressControls.ShowProgress(
                PlasticLocalization.GetString(PlasticLocalization.Name.InstallingProgress));

            notify.InstallationStarted();

            MacOSConfigWorkaround configWorkaround = new MacOSConfigWorkaround();

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    configWorkaround.CreateClientConfigIfNeeded();

                    Process installerProcess =
                        LaunchInstaller.ForPlatform(installerPath);

                    if (installerProcess != null)
                        installerProcess.WaitForExit();

                    configWorkaround.DeleteClientConfigIfNeeded();
                },
                /*afterOperationDelegate*/ delegate
                {
                    notify.InstallationFinished();
                    progressControls.HideProgress();

                    if (waiter.Exception != null)
                    {
                        progressControls.ShowError(
                            waiter.Exception.Message);
                        return;
                    }

                    File.Delete(installerPath);
                });
        }

        static string DownloadInstaller(
            string url,
            IProgressControls progressControls,
            CancellationToken downloadCancellationToken)
        {
            WebResponse response = null;
            string destinationPath;

            try
            {
                WebRequest request = WebRequest.Create(url);
                response = request.GetResponse();

                destinationPath = Path.Combine(
                    Path.GetTempPath(),
                    Path.GetFileName(response.ResponseUri.AbsolutePath));

                long totalBytes = response.ContentLength;

                if (File.Exists(destinationPath) &&
                    new FileInfo(destinationPath).Length == totalBytes)
                {
                    UnityEngine.Debug.LogFormat(
                        PlasticLocalization.GetString(PlasticLocalization.Name.SkippingDownloadFileExists),
                        destinationPath);

                    return destinationPath;
                }

                if (downloadCancellationToken.IsCancellationRequested)
                    return null;

                using (Stream remoteStream = response.GetResponseStream())
                using (Stream localStream = File.Create(destinationPath))
                {
                    if (!CopyStreamTo(
                            remoteStream,
                            localStream,
                            totalBytes,
                            progressControls,
                            downloadCancellationToken))
                    {
                        File.Delete(destinationPath);
                    }
                }
            }
            finally
            {
                if (response != null)
                    response.Close();
            }

            return destinationPath;
        }

        static bool CopyStreamTo(
            Stream source,
            Stream destination,
            long totalBytes,
            IProgressControls progressControls,
            CancellationToken downloadCancellationToken)
        {
            byte[] buffer = new byte[100 * 1024];
            int bytesRead;
            int bytesProcessed = 0;

            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (downloadCancellationToken.IsCancellationRequested)
                    return false;

                destination.Write(buffer, 0, bytesRead);
                bytesProcessed += bytesRead;

                float progressPercent = GetProgressBarPercent.ForTransfer(
                    bytesProcessed,
                    totalBytes) / 100f;

                if (progressControls is ProgressControlsForDialogs)
                    ((ProgressControlsForDialogs)progressControls).ProgressData.ProgressPercent = progressPercent;
                else if (progressControls is ProgressControlsForViews)
                    ((ProgressControlsForViews)progressControls).ProgressData.ProgressPercent = progressPercent;
            }

            return true;
        }

        static string UnZipMacOsPackage(
            string zipInstallerPath)
        {
            try
            {
                string pkgInstallerPath = zipInstallerPath.Substring(
                    0, zipInstallerPath.Length - ".zip".Length);

                string unzipCommand = string.Format(
                    "unzip -p \"{0}\" > \"{1}\"",
                    zipInstallerPath, pkgInstallerPath);

                unzipCommand = unzipCommand.Replace("\"", "\"\"");

                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "/bin/bash";
                processStartInfo.Arguments = string.Format("-c \"{0}\"", unzipCommand);
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.CreateNoWindow = true;

                Process process = Process.Start(processStartInfo);
                process.WaitForExit();

                return pkgInstallerPath;
            }
            finally
            {
                File.Delete(zipInstallerPath);
            }
        }
    }
}
