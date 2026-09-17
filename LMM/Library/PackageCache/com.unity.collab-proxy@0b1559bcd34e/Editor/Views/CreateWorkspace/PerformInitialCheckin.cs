using System;
using System.Collections;
using System.Linq;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Commands.CheckIn;
using Codice.Client.Common;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.Common.Checkin.Partial;
using PlasticGui;
using PlasticGui.Help.Conditions;
using PlasticGui.WorkspaceWindow;

namespace Unity.PlasticSCM.Editor.Views.CreateWorkspace
{
    internal static class PerformInitialCheckin
    {
        internal static void IfRepositoryIsEmpty(
            WorkspaceInfo wkInfo,
            string repository,
            bool isGluonWorkspace,
            IPlasticAPI plasticApi,
            IProgressControls progressControls,
            CreateWorkspaceView.ICreateWorkspaceListener createWorkspaceListener,
            UVCSWindow uvcsWindow)
        {
            RepositoryInfo repInfo = null;
            bool isEmptyRepository = false;

            progressControls.ShowProgress(string.Empty);

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
            /*threadOperationDelegate*/ delegate
            {
                RepositorySpec repSpec = new SpecGenerator().
                    GenRepositorySpec(false, repository, CmConnection.Get().UnityOrgResolver);

                repInfo = plasticApi.GetRepositoryInfo(repSpec);

                isEmptyRepository = IsEmptyRepositoryCondition.
                    Evaluate(wkInfo, repSpec, plasticApi);
            },
            /*afterOperationDelegate*/ delegate
            {
                progressControls.HideProgress();

                if (waiter.Exception != null)
                {
                    DisplayException(progressControls, waiter.Exception);
                    return;
                }

                if (!isEmptyRepository)
                {
                    uvcsWindow.RefreshWorkspaceUI();
                    return;
                }

                CheckinProjectFiles(
                    wkInfo, isGluonWorkspace, plasticApi,
                    progressControls, createWorkspaceListener);
            });
        }

        internal static void ForWorkspace(
            WorkspaceInfo wkInfo,
            bool isGluonWorkspace,
            IPlasticAPI plasticApi)
        {
            InitIgnoredRules.ForUnityWorkspace(wkInfo);

            string[] paths = new string[] { wkInfo.ClientPath };

            string comment = PlasticLocalization.GetString(
                PlasticLocalization.Name.UnityInitialCheckinComment);

            PerformAdd(wkInfo, paths, plasticApi);

            PerformCheckinForMode(wkInfo, paths, comment, isGluonWorkspace);
        }

        static void CheckinProjectFiles(
            WorkspaceInfo wkInfo,
            bool isGluonWorkspace,
            IPlasticAPI plasticApi,
            IProgressControls progressControls,
            CreateWorkspaceView.ICreateWorkspaceListener createWorkspaceListener)
        {
            progressControls.ShowProgress(PlasticLocalization.GetString(
                PlasticLocalization.Name.UnityInitialCheckinProgress));

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
            /*threadOperationDelegate*/ delegate
            {
                ForWorkspace(wkInfo, isGluonWorkspace, plasticApi);
            },
            /*afterOperationDelegate*/ delegate
            {
                progressControls.HideProgress();

                if (waiter.Exception != null &&
                    !IsMergeNeededException(waiter.Exception))
                {
                    DisplayException(progressControls, waiter.Exception);
                    return;
                }

                createWorkspaceListener.OnWorkspaceCreated(wkInfo, isGluonWorkspace);
            });
        }

        static void PerformAdd(
            WorkspaceInfo wkInfo,
            string[] paths,
            IPlasticAPI plasticApi)
        {
            AddOptions options = new AddOptions();
            options.AddPrivateParents = true;
            options.CheckoutParent = true;
            options.Recurse = true;
            options.SearchForPrivatePaths = true;
            options.SkipIgnored = true;

            IList checkouts;
            plasticApi.Add(wkInfo, paths, options, out checkouts);
        }

        static void PerformCheckinForMode(
            WorkspaceInfo wkInfo,
            string[] paths,
            string comment,
            bool isGluonWorkspace)
        {
            if (isGluonWorkspace)
            {
                new BaseCommandsImpl().PartialCheckin(wkInfo, paths.ToList(), comment);
                return;
            }

            CheckinParams ciParams = new CheckinParams();
            ciParams.paths = paths;
            ciParams.comment = comment;
            ciParams.time = DateTime.MinValue;
            ciParams.flags = CheckinFlags.Recurse | CheckinFlags.ProcessSymlinks;

            new BaseCommandsImpl().CheckIn(wkInfo, ciParams);
        }

        static bool IsMergeNeededException(Exception exception)
        {
            if (exception == null)
                return false;

            // Check the check-in exception for gluon
            if (exception is CheckinConflictsException)
                return true;

            // Check the check-in exceptions for plastic
            return exception is CmClientMergeNeededException;
        }

        static void DisplayException(
            IProgressControls progressControls,
            Exception ex)
        {
            ExceptionsHandler.LogException("PerformInitialCheckin", ex);

            progressControls.ShowError(ExceptionsHandler.GetCorrectExceptionMessage(ex));
        }
    }
}
