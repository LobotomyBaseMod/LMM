using System.Collections.Generic;

namespace Unity.PlasticSCM.Editor.UI.UndoRedo
{
    internal class UndoRedoHelper
    {
        internal interface IUndoRedoHost
        {
            UndoRedoState UndoRedoState { get; set; }
        }

        internal bool CanUndo
        {
            get { return mCurrentNode != null && mCurrentNode.Previous != null; }
        }

        internal bool CanRedo
        {
            get { return mCurrentNode != null && mCurrentNode.Next != null; }
        }

        internal UndoRedoHelper(IUndoRedoHost host)
        {
            mHost = host;
        }

        internal bool TryGetLastState(out UndoRedoState state)
        {
            state = null;
            if (!IsInLastState)
                return false;

            state = mCurrentNode.Value;
            return true;
        }

        internal void UpdateLastState()
        {
            UpdateLastState(mHost.UndoRedoState);
        }

        internal void Undo()
        {
            if (mCurrentNode != null && mCurrentNode.Previous != null)
            {
                mCurrentNode = mCurrentNode.Previous;
                mHost.UndoRedoState = mCurrentNode.Value;
            }
        }

        internal void Redo()
        {
            if (mCurrentNode != null && mCurrentNode.Next != null)
            {
                mCurrentNode = mCurrentNode.Next;
                mHost.UndoRedoState = mCurrentNode.Value;
            }
        }

        internal void Snapshot()
        {
            UndoRedoState current = mHost.UndoRedoState;
            if (mCurrentNode == null || !mCurrentNode.Value.Equals(current))
            {
                if (mCurrentNode != null && mCurrentNode.Next != null)
                    DiscardRedo();

                mStates.AddLast(current);
                mCurrentNode = mStates.Last;

                if (mStates.Count > UNDO_LIMIT)
                    mStates.RemoveFirst();
            }
        }

        bool IsInLastState
        {
            get { return mCurrentNode != null && mCurrentNode.Next == null; }
        }

        void UpdateLastState(UndoRedoState state)
        {
            if (mStates.Last != null)
                mStates.Last.Value = state;
        }

        void DiscardRedo()
        {
            while (mCurrentNode != null && mCurrentNode.Next != null)
                mStates.Remove(mCurrentNode.Next);
        }

        LinkedListNode<UndoRedoState> mCurrentNode;

        readonly IUndoRedoHost mHost;
        readonly LinkedList<UndoRedoState> mStates = new LinkedList<UndoRedoState>();

        const int UNDO_LIMIT = 250;
    }
}
