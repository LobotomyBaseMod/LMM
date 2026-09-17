using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Indentation;
using Unity.CodeEditor.Platform;
using Unity.CodeEditor.Rendering;
using Unity.CodeEditor.Utils;
using Unity.PlasticSCM.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Editing
{
    internal class TextArea : VisualElement
    {
        private bool _isFocused;
        private TextEditorOptions _options;
        private TextDocument _document;
        private IReadOnlySectionProvider _readOnlySectionProvider = NoReadOnlySections.Instance;
        private bool _rigthClickMovesCaret;
        private SelectionMouseHandler _selectionMouseHandler;
        private EditingCommandHandler _editingCommandHandler;
        private CaretNavigationCommandHandler _caretNavigationCommandHandler;
        private VisualElement _leftMarginsContainer;

        internal bool IsFocused => _isFocused;

        #region Caret handling on document changes

        private void OnDocumentChanging(object sender, DocumentChangeEventArgs e)
        {
            Caret.OnDocumentChanging();
        }

        private void OnDocumentChanged(object sender, DocumentChangeEventArgs e)
        {
            Caret.OnDocumentChanged(e);
            Selection = _selection.UpdateOnDocumentChange(e);
        }

        private void OnUpdateStarted(object sender, EventArgs e)
        {
            Document.UndoStack.PushOptional(new RestoreCaretAndSelectionUndoAction(this));
        }

        private void OnUpdateFinished(object sender, EventArgs e)
        {
            Caret.OnDocumentUpdateFinished();
        }

        private sealed class RestoreCaretAndSelectionUndoAction : IUndoableOperation
        {
            // keep textarea in weak reference because the IUndoableOperation is stored with the document
            private readonly WeakReference _textAreaReference;

            private readonly TextViewPosition _caretPosition;
            private readonly Selection _selection;

            internal RestoreCaretAndSelectionUndoAction(TextArea textArea)
            {
                _textAreaReference = new WeakReference(textArea);
                // Just save the old caret position, no need to validate here.
                // If we restore it, we'll validate it anyways.
                _caretPosition = textArea.Caret.NonValidatedPosition;
                _selection = textArea.Selection;
            }

            public void Undo()
            {
                var textArea = (TextArea)_textAreaReference.Target;
                if (textArea != null)
                {
                    textArea.Caret.Position = _caretPosition;
                    textArea.Selection = _selection;
                }
            }

            public void Redo()
            {
                // redo=undo: we just restore the caret/selection state
                Undo();
            }
        }
        #endregion

        #region TextView property

        /// <summary>
        /// Gets the text view used to display text in this text area.
        /// </summary>
        internal TextView TextView { get; }

        #endregion

        #region Document property
        /// <summary>
        /// Gets/Sets the document displayed by the text editor.
        /// </summary>
        internal TextDocument Document
        {
            get
            {
                return _document;
            }
            set
            {
                TextDocument oldValue = _document;
                _document = value;
                OnDocumentChanged(oldValue, value);
            }
        }

        /// <inheritdoc/>
        internal event EventHandler<DocumentChangedEventArgs> DocumentChanged;

        /// <summary>
        /// Gets if the the document displayed by the text editor is readonly
        /// </summary>
        internal bool IsReadOnly
        {
            get => ReadOnlySectionProvider == ReadOnlySectionDocument.Instance;
        }

        private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
        {
            if (oldValue != null)
            {
                TextDocumentWeakEventManager.Changing.RemoveHandler(oldValue, OnDocumentChanging);
                TextDocumentWeakEventManager.Changed.RemoveHandler(oldValue, OnDocumentChanged);
                TextDocumentWeakEventManager.UpdateStarted.RemoveHandler(oldValue, OnUpdateStarted);
                TextDocumentWeakEventManager.UpdateFinished.RemoveHandler(oldValue, OnUpdateFinished);
            }
            TextView.Document = newValue;
            if (newValue != null)
            {
                TextDocumentWeakEventManager.Changing.AddHandler(newValue, OnDocumentChanging);
                TextDocumentWeakEventManager.Changed.AddHandler(newValue, OnDocumentChanged);
                TextDocumentWeakEventManager.UpdateStarted.AddHandler(newValue, OnUpdateStarted);
                TextDocumentWeakEventManager.UpdateFinished.AddHandler(newValue, OnUpdateFinished);

                MarkDirtyRepaint();
            }
            // Reset caret location and selection: this is necessary because the caret/selection might be invalid
            // in the new document (e.g. if new document is shorter than the old document).
            Caret.Location = new TextLocation(1, 1);
            ClearSelection();
            DocumentChanged?.Invoke(this, new DocumentChangedEventArgs(oldValue, newValue));
            //CommandManager.InvalidateRequerySuggested();
        }
        #endregion

        #region Options property

        /// <summary>
        /// Options property.
        /// </summary>
        internal TextEditorOptions Options
        {
            get
            {
                return _options;
            }
            set
            {
                TextEditorOptions oldValue = _options;
                _options = value;
                OnOptionsChanged(oldValue, value);
            }
        }

        /// <summary>
        /// Occurs when a text editor option has changed.
        /// </summary>
        internal event PropertyChangedEventHandler OptionChanged;

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            OnOptionChanged(e);
        }

        /// <summary>
        /// Raises the <see cref="OptionChanged"/> event.
        /// </summary>
        protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
        {
            OptionChanged?.Invoke(this, e);
        }

        private void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
        {
            if (oldValue != null)
            {
                PropertyChangedWeakEventManager.RemoveHandler(oldValue, OnOptionChanged);
            }
            TextView.Options = newValue;
            if (newValue != null)
            {
                PropertyChangedWeakEventManager.AddHandler(newValue, OnOptionChanged);
            }
            OnOptionChanged(new PropertyChangedEventArgs(null));
        }
        #endregion

        /// <summary>
        /// Creates a new TextArea instance.
        /// </summary>
        internal TextArea() : this(new TextView())
        {
        }

        /// <summary>
        /// Creates a new TextArea instance.
        /// </summary>
        protected TextArea(TextView textView)
        {
            focusable = true;
            style.flexDirection = FlexDirection.Row;
            style.flexGrow = 1;
            style.backgroundColor = TextEditorColors.Background;

            _selectionMouseHandler = new SelectionMouseHandler(this);
            _editingCommandHandler = new EditingCommandHandler(this);
            _caretNavigationCommandHandler = new CaretNavigationCommandHandler(this);

            TextView = textView ?? throw new ArgumentNullException(nameof(textView));
            Options = textView.Options;

            _selection = EmptySelection = new EmptySelection(this);

            textView.Services.AddService(this);

            _selectionLayer = new SelectionLayer(this);
            textView.InsertLayer(_selectionLayer, KnownLayer.Selection, LayerInsertionPosition.Replace);

            Caret = new Caret(this);
            Caret.PositionChanged += (sender, e) => RequestSelectionValidation();
            Caret.PositionChanged += CaretPositionChanged;

            LeftMargins.CollectionChanged += LeftMargins_CollectionChanged;

            _leftMarginsContainer = new VisualElement();
            _leftMarginsContainer.style.flexDirection = FlexDirection.Row;
            _leftMarginsContainer.style.flexShrink = 0;
            Add(_leftMarginsContainer);
            Add(textView);

            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
            RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
            RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
        }

        internal void Dispose()
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
            UnregisterCallback<NavigationMoveEvent>(OnNavigationMoveEvent);
            UnregisterCallback<FocusInEvent>(OnFocusIn);
            UnregisterCallback<FocusOutEvent>(OnFocusOut);
            UnregisterCallback<PointerDownEvent>(OnPointerDownEvent);
            UnregisterCallback<PointerUpEvent>(OnPointerUpEvent);
            UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
        }

        private void OnKeyDownEvent(KeyDownEvent e)
        {
            _caretNavigationCommandHandler.OnKeyDown(e);
            _editingCommandHandler.OnKeyDown(e);
            ProcessTextInput(e);
        }

        private void OnNavigationMoveEvent(NavigationMoveEvent e)
        {
#if UNITY_2022_1_OR_NEWER
            if (IsReadOnly)
                return;

            // Tab and Shift+Tab generate a NavigationMoveEvent with direction
            // Next/Previous. Consuming it prevents the focus controller from
            // moving focus away from the text editor.
            if (e.direction == NavigationMoveEvent.Direction.Next ||
                e.direction == NavigationMoveEvent.Direction.Previous)
            {
                e.StopPropagation();
                #if UNITY_6000_0_OR_NEWER
                    focusController.IgnoreEvent(e);
                #else
                    e.PreventDefault();
                #endif
            }
#endif
        }

        private void OnFocusIn(FocusInEvent e)
        {
            OnFocus();
        }

        private void OnFocusOut(FocusOutEvent e)
        {
            OnLostFocus();
        }

        private void OnPointerDownEvent(PointerDownEvent e)
        {
            var localPos = e.localPosition;
            var textViewPos = TextView.WorldToLocal(e.position);
            TextView.OnPointerPressed(textViewPos);
            _selectionMouseHandler.OnPointerPressed(e);
            OnFocus();
            this.CapturePointer(e.pointerId);
        }

        private void OnPointerUpEvent(PointerUpEvent e)
        {
            var textViewPos = TextView.WorldToLocal(e.position);
            TextView.OnPointerReleased(textViewPos);
            _selectionMouseHandler.OnPointerReleased(e);
            this.ReleasePointer(e.pointerId);
        }

        private void OnPointerMoveEvent(PointerMoveEvent e)
        {
            if (this.HasPointerCapture(e.pointerId))
            {
                TextView.OnPointerMoved();
                _selectionMouseHandler.OnPointerMoved(e);
            }
        }

        #region Force caret to stay inside selection

        private bool _ensureSelectionValidRequested;
        private int _allowCaretOutsideSelection;

        private void RequestSelectionValidation()
        {
            if (!_ensureSelectionValidRequested && _allowCaretOutsideSelection == 0)
            {
                _ensureSelectionValidRequested = true;
                EditorDispatcher.Dispatch(EnsureSelectionValid);
            }
        }

        /// <summary>
        /// Code that updates only the caret but not the selection can cause confusion when
        /// keys like 'Delete' delete the (possibly invisible) selected text and not the
        /// text around the caret.
        ///
        /// So we'll ensure that the caret is inside the selection.
        /// (when the caret is not in the selection, we'll clear the selection)
        ///
        /// This method is invoked using the Dispatcher so that code may temporarily violate this rule
        /// (e.g. most 'extend selection' methods work by first setting the caret, then the selection),
        /// it's sufficient to fix it after any event handlers have run.
        /// </summary>
        private void EnsureSelectionValid()
        {
            _ensureSelectionValidRequested = false;
            if (_allowCaretOutsideSelection == 0)
            {
                if (!_selection.IsEmpty && !_selection.Contains(Caret.Offset))
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>
        /// Temporarily allows positioning the caret outside the selection.
        /// Dispose the returned IDisposable to revert the allowance.
        /// </summary>
        /// <remarks>
        /// The text area only forces the caret to be inside the selection when other events
        /// have finished running (using the dispatcher), so you don't have to use this method
        /// for temporarily positioning the caret in event handlers.
        /// This method is only necessary if you want to run the dispatcher, e.g. if you
        /// perform a drag'n'drop operation.
        /// </remarks>
        internal IDisposable AllowCaretOutsideSelection()
        {
            EditorDispatcher.VerifyMainThreadAccess();
            _allowCaretOutsideSelection++;
            return new CallbackOnDispose(
                delegate
                {
                    EditorDispatcher.VerifyMainThreadAccess();
                    _allowCaretOutsideSelection--;
                    RequestSelectionValidation();
                });
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the Caret used for this text area.
        /// </summary>
        internal Caret Caret { get; }

                /// <summary>
        /// Scrolls the text view so that the requested line is in the middle.
        /// If the textview can be scrolled.
        /// </summary>
        /// <param name="line">The line to scroll to.</param>
        internal void ScrollToLine(int line)
        {
            var viewPortLines = (int)TextView.ScrollViewport.y;

            if (viewPortLines < Document.LineCount)
            {
                ScrollToLine(line, 2, viewPortLines / 2);
            }
        }

        /// <summary>
        /// Scrolls the textview to a position with n lines above and below it.
        /// </summary>
        /// <param name="line">the requested line number.</param>
        /// <param name="linesEitherSide">The number of lines above and below.</param>
        internal void ScrollToLine(int line, int linesEitherSide)
        {
            ScrollToLine(line, linesEitherSide, linesEitherSide);
        }

        /// <summary>
        /// Scrolls the textview to a position with n lines above and below it.
        /// </summary>
        /// <param name="line">the requested line number.</param>
        /// <param name="linesAbove">The number of lines above.</param>
        /// <param name="linesBelow">The number of lines below.</param>
        internal void ScrollToLine(int line, int linesAbove, int linesBelow)
        {
            var offset = line - linesAbove;

            if (offset < 0)
            {
                offset = 0;
            }

            this.TextView.MakeVisible(new Rect(1, offset, 0, 1));

            offset = line + linesBelow;

            if (offset >= 0)
            {
                this.TextView.MakeVisible(new Rect(1, offset, 0, 1));
            }
        }

        private void CaretPositionChanged(object sender, EventArgs e)
        {
            // TODO: review!! ScrollToLine is not correct
            /*if (TextView == null)
                return;

            TextView.HighlightedLine = Caret.Line;

            ScrollToLine(Caret.Line, 2);*/
        }

        #endregion

        /// <summary>
        /// Gets the collection of margins displayed to the left of the text view.
        /// </summary>
        internal ObservableCollection<AbstractMargin> LeftMargins { get; } = new ObservableCollection<AbstractMargin>();

        private void LeftMargins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is ITextViewConnect c)
                        c.RemoveFromTextView(TextView);
                    if (item is VisualElement ve)
                        ve.RemoveFromHierarchy();
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ITextViewConnect c)
                        c.AddToTextView(TextView);
                    if (item is VisualElement ve && _leftMarginsContainer != null)
                        _leftMarginsContainer.Add(ve);
                }
            }
        }

        /// <summary>
        /// Gets/Sets an object that provides read-only sections for the text area.
        /// </summary>
        internal IReadOnlySectionProvider ReadOnlySectionProvider
        {
            get => _readOnlySectionProvider;
            set => _readOnlySectionProvider = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Determines whether caret position should be changed to the mouse position when you right click or not.
        /// </summary>
        internal bool RightClickMovesCaret
        {
            get => _rigthClickMovesCaret;
            set => _rigthClickMovesCaret = value;
        }

        internal readonly Selection EmptySelection;
        private Selection _selection;
        private SelectionLayer _selectionLayer;

        /// <summary>
        /// Occurs when the selection has changed.
        /// </summary>
        internal event EventHandler SelectionChanged;

        /// <summary>
        /// Gets/Sets the selection in this text area.
        /// </summary>

        internal Selection Selection
        {
            get => _selection;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.TextArea != this)
                    throw new ArgumentException("Cannot use a Selection instance that belongs to another text area.");
                if (!Equals(_selection, value))
                {
                    if (TextView != null)
                    {
                        var oldSegment = _selection.SurroundingSegment;
                        var newSegment = value.SurroundingSegment;
                        if (!Selection.EnableVirtualSpace && (_selection is SimpleSelection && value is SimpleSelection && oldSegment != null && newSegment != null))
                        {
                            // perf optimization:
                            // When a simple selection changes, don't redraw the whole selection, but only the changed parts.
                            var oldSegmentOffset = oldSegment.Offset;
                            var newSegmentOffset = newSegment.Offset;
                            if (oldSegmentOffset != newSegmentOffset)
                            {
                                TextView.Redraw(Math.Min(oldSegmentOffset, newSegmentOffset),
                                                Math.Abs(oldSegmentOffset - newSegmentOffset));
                            }
                            var oldSegmentEndOffset = oldSegment.EndOffset;
                            var newSegmentEndOffset = newSegment.EndOffset;
                            if (oldSegmentEndOffset != newSegmentEndOffset)
                            {
                                TextView.Redraw(Math.Min(oldSegmentEndOffset, newSegmentEndOffset),
                                                Math.Abs(oldSegmentEndOffset - newSegmentEndOffset));
                            }
                        }
                        else
                        {
                            TextView.Redraw(oldSegment);
                            TextView.Redraw(newSegment);
                        }
                    }
                    _selection = value;
                    _selectionLayer.InvalidateSelection();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                    // a selection change causes commands like copy/paste/etc. to change status
                    //CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        internal void ClearSelection()
        {
            Selection = EmptySelection;
        }

        Color mSelectionForeground;

        /// <summary>
        /// Gets/Sets the foreground brush used for selected text.
        /// </summary>
        internal Color SelectionForeground
        {
            get => mSelectionForeground;
            set
            {
                mSelectionForeground = value;
                TextView.Redraw();
            }
        }

        #region Focus Handling (Show/Hide Caret)

        internal void OnFocus()
        {
            _isFocused = true;
            Caret.Show();
        }

        internal void OnLostFocus()
        {
            _isFocused = false;
            Caret.Hide();
        }

        void ProcessTextInput(KeyDownEvent e)
        {
            if (e.character != 0 && TextView.Font != null && TextView.Font.HasCharacter(e.character))
            {
                TextInputEventArgs inputEventArgs = new TextInputEventArgs()
                {
                    Text = e.character.ToString(),
                };

                e.StopPropagation();
                PerformTextInput(inputEventArgs);
            }
        }

        #endregion

        #region OnTextInput / RemoveSelectedText / ReplaceSelectionWithText
        /// <summary>
        /// Occurs when the TextArea receives text input.
        /// but occurs immediately before the TextArea handles the TextInput event.
        /// </summary>
        internal event EventHandler<TextInputEventArgs> TextEntering;

        /// <summary>
        /// Occurs when the TextArea receives text input.
        /// but occurs immediately after the TextArea handles the TextInput event.
        /// </summary>
        internal event EventHandler<TextInputEventArgs> TextEntered;

        /// <summary>
        /// Raises the TextEntering event.
        /// </summary>
        protected virtual void OnTextEntering(TextInputEventArgs e)
        {
            TextEntering?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the TextEntered event.
        /// </summary>
        protected virtual void OnTextEntered(TextInputEventArgs e)
        {
            TextEntered?.Invoke(this, e);
        }

        /*protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if (!e.Handled && Document != null)
            {
                if (string.IsNullOrEmpty(e.Text) || e.Text == "\x1b" || e.Text == "\b" || e.Text == "\u007f")
                {
                    // TODO: check this
                    // ASCII 0x1b = ESC.
                    // produces a TextInput event with that old ASCII control char
                    // when Escape is pressed. We'll just ignore it.

                    // A deadkey followed by backspace causes a textinput event for the BS character.

                    // Similarly, some shortcuts like Alt+Space produce an empty TextInput event.
                    // We have to ignore those (not handle them) to keep the shortcut working.
                    return;
                }
                HideMouseCursor();
                PerformTextInput(e);
                e.Handled = true;
            }
        }*/

        /// <summary>
        /// Performs text input.
        /// This raises the <see cref="TextEntering"/> event, replaces the selection with the text,
        /// and then raises the <see cref="TextEntered"/> event.
        /// </summary>
        internal void PerformTextInput(string text)
        {
            var e = new TextInputEventArgs
            {
                Text = text,
            };
            PerformTextInput(e);
        }

        /// <summary>
        /// Performs text input.
        /// This raises the <see cref="TextEntering"/> event, replaces the selection with the text,
        /// and then raises the <see cref="TextEntered"/> event.
        /// </summary>
        internal void PerformTextInput(TextInputEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (Document == null)
                throw ThrowUtil.NoDocumentAssigned();
            OnTextEntering(e);
            if (!e.Handled)
            {
                if (e.Text == "\n" || e.Text == "\r" || e.Text == "\r\n")
                    ReplaceSelectionWithNewLine();
                else
                {
                    // TODO
                    //if (OverstrikeMode && Selection.IsEmpty && Document.GetLineByNumber(Caret.Line).EndOffset > Caret.Offset)
                    //    EditingCommands.SelectRightByCharacter.Execute(null, this);
                    ReplaceSelectionWithText(e.Text);
                }
                OnTextEntered(e);
                Caret.BringCaretToView();
            }
        }

        private void ReplaceSelectionWithNewLine()
        {
            var newLine = TextUtilities.GetNewLineFromDocument(Document, Caret.Line);
            using (Document.RunUpdate())
            {
                ReplaceSelectionWithText(newLine);
                if (IndentationStrategy != null)
                {
                    var line = Document.GetLineByNumber(Caret.Line);
                    var deletable = GetDeletableSegments(line);
                    if (deletable.Length == 1 && deletable[0].Offset == line.Offset && deletable[0].Length == line.Length)
                    {
                        // use indentation strategy only if the line is not read-only
                        IndentationStrategy.IndentLine(Document, line);
                    }
                }
            }
        }

        internal void ReplaceSelectionWithText(string newText)
        {
            if (newText == null)
                throw new ArgumentNullException(nameof(newText));
            if (Document == null)
                throw ThrowUtil.NoDocumentAssigned();
            _selection.ReplaceSelectionWithText(newText);
        }

        internal ISegment[] GetDeletableSegments(ISegment segment)
        {
            var deletableSegments = ReadOnlySectionProvider.GetDeletableSegments(segment);
            if (deletableSegments == null)
                throw new InvalidOperationException("ReadOnlySectionProvider.GetDeletableSegments returned null");
            var array = deletableSegments.ToArray();
            var lastIndex = segment.Offset;
            foreach (var t in array)
            {
                if (t.Offset < lastIndex)
                    throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
                lastIndex = t.EndOffset;
            }
            if (lastIndex > segment.EndOffset)
                throw new InvalidOperationException("ReadOnlySectionProvider returned incorrect segments (outside of input segment / wrong order)");
            return array;
        }
        #endregion

        #region IndentationStrategy property

        /// <summary>
        /// Gets/Sets the indentation strategy used when inserting new lines.
        /// </summary>
        internal IIndentationStrategy IndentationStrategy { get; set; }

        #endregion

        internal void RemoveSelectedText()
        {
            if (Document == null)
                throw ThrowUtil.NoDocumentAssigned();
            _selection.ReplaceSelectionWithText(string.Empty);
#if DEBUG
            if (!_selection.IsEmpty)
            {
                foreach (var s in _selection.Segments)
                {
                    Debug.Assert(!ReadOnlySectionProvider.GetDeletableSegments(s).Any());
                }
            }
#endif
        }

        /// <summary>
        /// Occurs when text inside the TextArea was copied.
        /// </summary>
        internal event EventHandler<TextEventArgs> TextCopied;

        internal void OnTextCopied(TextEventArgs e)
        {
            TextCopied?.Invoke(this, e);
        }

        /// <summary>
        /// EventArgs with text.
        /// </summary>
        internal class TextEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the text.
            /// </summary>
            internal string Text { get; }

            /// <summary>
            /// Creates a new TextEventArgs instance.
            /// </summary>
            internal TextEventArgs(string text)
            {
                Text = text ?? throw new ArgumentNullException(nameof(text));
            }
        }
    }
}
