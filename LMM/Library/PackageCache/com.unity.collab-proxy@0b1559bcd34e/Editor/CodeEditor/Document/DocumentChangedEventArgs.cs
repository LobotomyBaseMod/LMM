using System;

namespace Unity.CodeEditor.Document
{
    internal class DocumentChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the old TextDocument.
        /// </summary>
        internal TextDocument OldDocument { get; private set; }
        /// <summary>
        /// Gets the new TextDocument.
        /// </summary>
        internal TextDocument NewDocument { get; private set; }

        /// <summary>
        /// Provides data for the <see cref="ITextEditorComponent.DocumentChanged"/> event.
        /// </summary>
        internal DocumentChangedEventArgs(TextDocument oldDocument, TextDocument newDocument)
        {
            OldDocument = oldDocument;
            NewDocument = newDocument;
        }
    }
}
