using System;

namespace Unity.PlasticSCM.Editor.UI.UndoRedo
{
    internal class UndoRedoState : IEquatable<UndoRedoState>
    {
        internal string Text { get; private set; }

        internal int CaretPosition { get; private set;}

        internal UndoRedoState(string text, int caretPosition)
        {
            Text = text;
            CaretPosition = caretPosition;
        }

        bool IEquatable<UndoRedoState>.Equals(UndoRedoState other)
        {
            return ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UndoRedoState))
                return false;

            return ((IEquatable<UndoRedoState>)this).Equals((UndoRedoState)obj);
        }

        public override int GetHashCode()
        {
            if (Text == null)
                return 0;

            return Text.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Text: {0}, CaretPosition: {1}", Text, CaretPosition);
        }
    }
}
