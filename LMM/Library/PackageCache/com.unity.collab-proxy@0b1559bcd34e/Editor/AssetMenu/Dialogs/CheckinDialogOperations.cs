using System;
using System.Collections.Generic;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Commands.CheckIn;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.Common.Checkin.Partial;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.PendingChanges;

namespace Unity.PlasticSCM.Editor.AssetMenu.Dialogs
{
    internal static class CheckinDialogOperations
    {
        internal static void CheckinPaths(
            WorkspaceInfo wkInfo,
            List<string> paths,
            string comment,
            IWorkspaceWindow workspaceWindow,
            CheckinDialog dialog,
            GuiMessage.IGuiMessage guiMessage,
            IProgressControls progressControls,
            IMergeViewLauncher mergeViewLauncher,
            IPendingChangesUpdater pendingChangesUpdater)
        {
            BaseCommandsImpl baseCommands = new BaseCommandsImpl();

            progressControls.ShowProgress(
                PlasticLocalization.Name.CheckinInFilesProgress.GetString());

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(50);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    ApplyLocalChanges(wkInfo, paths);

                    CheckinParams ciParams = new CheckinParams();
                    ciParams.paths = paths.ToArray();
                    ciParams.comment = comment;
                    ciParams.time = DateTime.MinValue;
                    ciParams.flags = CheckinFlags.Recurse | CheckinFlags.ProcessSymlinks;

                    baseCommands.CheckIn(wkInfo, ciParams);
                },
                /*afterOperationDelegate*/ delegate
                {
                    progressControls.HideProgress();
                    ((IPlasticDialogCloser)dialog).CloseDialog();

                    if (waiter.Exception is CmClientMergeNeededException)
                    {
                        // we need to explicitly call EditorWindow.Close() to ensure
                        // that the dialog is closed before asking the user
                        dialog.Close();

                        if (!UserWantsToShowIncomingView(guiMessage))
                            return;

                        ShowIncomingChanges.FromCheckin(
                            wkInfo,
                            mergeViewLauncher,
                            progressControls);

                        return;
                    }

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    if (pendingChangesUpdater != null)
                        pendingChangesUpdater.Update(DateTime.Now);

                    workspaceWindow.RefreshView(ViewType.HistoryView);
                    workspaceWindow.RefreshView(ViewType.BranchesView);
                    workspaceWindow.RefreshView(ViewType.ChangesetsView);
                    workspaceWindow.RefreshView(ViewType.LocksView);
                    workspaceWindow.RefreshView(ViewType.BranchExplorerView);
                });
        }

        internal static void CheckinPathsPartial(
            WorkspaceInfo wkInfo,
            List<string> paths,
            string comment,
            ViewHost viewHost,
            CheckinDialog dialog,
            GuiMessage.IGuiMessage guiMessage,
            IProgressControls progressControls,
            IGluonViewSwitcher gluonViewSwitcher,
            IPendingChangesUpdater pendingChangesUpdater)
        {
            BaseCommandsImpl baseCommands = new BaseCommandsImpl();

            progressControls.ShowProgress(
                PlasticLocalization.Name.CheckinInFilesProgress.GetString());

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(50);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    ApplyLocalChanges(wkInfo, paths);

                    baseCommands.PartialCheckin(wkInfo, paths, comment);
                },
                /*afterOperationDelegate*/ delegate
                {
                    progressControls.HideProgress();

                    ((IPlasticDialogCloser)dialog).CloseDialog();

                    if (waiter.Exception is CheckinConflictsException)
                    {
                        // we need to explicitly call EditorWindow.Close() to ensure
                        // that the dialog is closed before asking the user
                        dialog.Close();

                        if (!UserWantsToShowIncomingView(guiMessage))
                            return;

                        gluonViewSwitcher.ShowIncomingChangesView();
                        return;
                    }

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    if (pendingChangesUpdater != null)
                        pendingChangesUpdater.Update(DateTime.Now);

                    viewHost.RefreshView(ViewType.HistoryView);
                    viewHost.RefreshView(ViewType.LocksView);
                });
        }

        static bool UserWantsToShowIncomingView(GuiMessage.IGuiMessage guiMessage)
        {
            GuiMessage.GuiMessageResponseButton result = guiMessage.ShowQuestion(
                PlasticLocalization.GetString(PlasticLocalization.Name.CheckinConflictsTitle),
                PlasticLocalization.GetString(PlasticLocalization.Name.UnityCheckinConflictsExplanation),
                PlasticLocalization.GetString(PlasticLocalization.Name.CheckinShowIncomingChangesView),
                PlasticLocalization.GetString(PlasticLocalization.Name.CancelButton),
                null);

            return result == GuiMessage.GuiMessageResponseButton.Positive;
        }

        static void ApplyLocalChanges(WorkspaceInfo wkInfo, List<string> paths)
        {
            ApplyLocalChangesOptions options = new ApplyLocalChangesOptions();
            options.bIncludeDependencies = true;
            options.MatchingOptions = new MovedMatchingOptions();
            options.TypesToApply =
                ChangeTypes.Changed |
                ChangeTypes.Private |
                ChangeTypes.LocallyDeleted |
                ChangeTypes.LocallyMoved;
            options.Operation = DependenciesOperation.Checkin;
            options.bProcessSymlinks = true;

            ApplyChanges.Apply(wkInfo, paths, options, null, out _, out _);
        }
    }
}
