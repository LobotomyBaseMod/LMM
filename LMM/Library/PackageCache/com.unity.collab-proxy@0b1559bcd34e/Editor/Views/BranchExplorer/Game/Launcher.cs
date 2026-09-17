using UnityEngine.UIElements;

namespace Unity.PlasticSCM.Editor.Views.BranchExplorer.Game
{
    internal class Launcher
    {
        internal static Launcher Install(
            VisualElement parent,
            BranchExplorerView branchExplorerView)
        {
            return new Launcher(parent, branchExplorerView);
        }

        internal void Dispose()
        {
            Stop();

            mParent.UnregisterCallback<KeyDownEvent>(
                OnKeyDown, TrickleDown.TrickleDown);
        }

        Launcher(
            VisualElement parent,
            BranchExplorerView branchExplorerView)
        {
            mParent = parent;
            mBranchExplorerView = branchExplorerView;
            mDetector = new EasterEggDetector();

            mParent.RegisterCallback<KeyDownEvent>(
                OnKeyDown, TrickleDown.TrickleDown);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (mGame != null)
                return;

            if (mDetector.ProcessKeyCode(evt.keyCode))
                Start();
        }

        void Start()
        {
            if (mBranchExplorerView == null)
                return;

            BranchExplorerViewer viewer = mBranchExplorerView.BranchExplorerViewer;
            if (viewer == null || viewer.ExplorerLayout == null)
                return;

            mGame = new BranchRunnerGame();
            mGame.OnExit += Stop;
            mParent.Add(mGame);
            mGame.StartWithLayout(
                viewer.ExplorerLayout,
                viewer.Zoom.Offset,
                viewer.Zoom.ZoomLevel);
        }

        void Stop()
        {
            if (mGame == null)
                return;

            mGame.Stop();
            mGame.OnExit -= Stop;
            mParent.Remove(mGame);
            mGame = null;
            mDetector.Reset();
        }

        readonly VisualElement mParent;
        readonly BranchExplorerView mBranchExplorerView;
        readonly EasterEggDetector mDetector;
        BranchRunnerGame mGame;
    }
}
