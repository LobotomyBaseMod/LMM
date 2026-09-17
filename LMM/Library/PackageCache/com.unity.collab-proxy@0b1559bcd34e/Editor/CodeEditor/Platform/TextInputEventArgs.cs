using System;

namespace Unity.CodeEditor.Platform
{
    internal class TextInputEventArgs : EventArgs
    {
        internal string Text { get; set; }
        internal bool Handled { get; set; }
    }
}