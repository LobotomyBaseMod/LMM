using System;

using Codice.CM.Common;
using PlasticGui.WorkspaceWindow;
using Unity.PlasticSCM.Editor.UI;

using GluonIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.IncomingChangesUpdater;
using GluonCheckIncomingChanges = PlasticGui.Gluon.WorkspaceWindow.CheckIncomingChanges;

namespace Unity.PlasticSCM.Editor
{
    internal static class IncomingChanges
    {
        internal static IncomingChangesUpdater BuildUpdaterForDeveloper(
            WorkspaceInfo wkInfo,
            CheckIncomingChanges.IAutoRefreshIncomingChangesView autoRefreshIncomingChangesView,
            CheckIncomingChanges.IUpdateIncomingChanges updateIncomingChanges)
        {
            IncomingChangesUpdater updater = new IncomingChangesUpdater(
                wkInfo,
                new UnityPlasticTimerBuilder(),
                autoRefreshIncomingChangesView,
                new CheckIncomingChanges.CalculateIncomingChanges(),
                updateIncomingChanges);

            updater.Start();
            return updater;
        }

        internal static GluonIncomingChangesUpdater BuildUpdaterForGluon(
            WorkspaceInfo wkInfo,
            GluonCheckIncomingChanges.IAutoRefreshIncomingChangesView autoRefreshIncomingChangesView,
            GluonCheckIncomingChanges.IUpdateIncomingChanges updateIncomingChanges,
            GluonCheckIncomingChanges.ICalculateIncomingChanges calculateIncomingChanges)
        {
            GluonIncomingChangesUpdater updater = new GluonIncomingChangesUpdater(
                wkInfo,
                new UnityPlasticTimerBuilder(),
                autoRefreshIncomingChangesView,
                calculateIncomingChanges,
                updateIncomingChanges);

            updater.Start();
            return updater;
        }

        internal static void LaunchUpdater(
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater)
        {
            if (developerIncomingChangesUpdater != null)
            {
                developerIncomingChangesUpdater.Start();
                developerIncomingChangesUpdater.AutoUpdate();
            }

            if (gluonIncomingChangesUpdater != null)
            {
                gluonIncomingChangesUpdater.Start();
                gluonIncomingChangesUpdater.AutoUpdate();
            }
        }

        internal static void StopUpdater(
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater)
        {
            if (developerIncomingChangesUpdater != null)
                developerIncomingChangesUpdater.Stop();

            if (gluonIncomingChangesUpdater != null)
                gluonIncomingChangesUpdater.Stop();
        }

        internal static void DisposeUpdater(
            IncomingChangesUpdater developerIncomingChangesUpdater,
            GluonIncomingChangesUpdater gluonIncomingChangesUpdater)
        {
            if (developerIncomingChangesUpdater != null)
                developerIncomingChangesUpdater.Dispose();

            if (gluonIncomingChangesUpdater != null)
                gluonIncomingChangesUpdater.Dispose();
        }
    }
}
