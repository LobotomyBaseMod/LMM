using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Editing;
using Unity.CodeEditor.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Selection = Unity.CodeEditor.Editing.Selection;

namespace Unity.CodeEditor
{
    internal class TextEditor : VisualElement
    {
        private TextEditorOptions _options;
        private TextDocument _document;
        private bool _wordWrap;
        private bool _showLineNumbers;
        private bool _isModified;
        private bool _isReadonly;
        private readonly TextArea _textArea;
        private readonly Scroller _verticalScroller;
        private readonly Scroller _horizontalScroller;
        private readonly VisualElement _contentArea;
        private ScrollBarVisibility _verticalScrollBarVisibility = ScrollBarVisibility.Auto;
        private ScrollBarVisibility _horizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        private ScrollBarVisibility _horizontalScrollBarVisibilityBck;

        #region Constructors

        /// <summary>
        /// Creates a new TextEditor instance.
        /// </summary>
        internal TextEditor() : this(new TextArea())
        {
        }

        /// <summary>
        /// Creates a new TextEditor instance.
        /// </summary>
        protected TextEditor(TextArea textArea) : this(textArea, new TextDocument())
        {
        }

        protected TextEditor(TextArea textArea, TextDocument document)
        {
            style.flexGrow = 1;
            style.flexDirection = FlexDirection.Column;

            _textArea = textArea ?? throw new ArgumentNullException(nameof(textArea));

            var mainRow = new VisualElement();
            mainRow.style.flexGrow = 1;
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.overflow = Overflow.Hidden;
            Add(mainRow);

            _contentArea = new VisualElement();
            _contentArea.style.flexGrow = 1;
            _contentArea.style.overflow = Overflow.Hidden;
            mainRow.Add(_contentArea);
            _contentArea.Add(_textArea);

            _verticalScroller = new Scroller(0, 100, OnVerticalScrollChanged, SliderDirection.Vertical);
            _verticalScroller.style.position = Position.Relative;
            mainRow.Add(_verticalScroller);

            _horizontalScroller = new Scroller(0, 100, OnHorizontalScrollChanged, SliderDirection.Horizontal);
            _horizontalScroller.style.position = Position.Relative;
            Add(_horizontalScroller);

            _contentArea.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            _contentArea.RegisterCallback<WheelEvent>(OnWheel);

            textArea.TextView.Services.AddService(this);
            textArea.TextView.ScrollOffsetChanged += OnTextViewScrollOffsetChanged;
            textArea.TextView.VisualLinesChanged += OnVisualLinesChanged;

            Options = _textArea.Options;
            Document = document;

            RegisterCallback<FocusInEvent>(OnFocusIn);
        }

        internal void Dispose()
        {
            _contentArea.UnregisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            _contentArea.UnregisterCallback<WheelEvent>(OnWheel);
            UnregisterCallback<FocusInEvent>(OnFocusIn);

            _textArea.TextView.ScrollOffsetChanged -= OnTextViewScrollOffsetChanged;
            _textArea.TextView.VisualLinesChanged -= OnVisualLinesChanged;

            _textArea.Dispose();
        }

        #endregion

        #region Scroll synchronization

        private bool _updatingScroll;

        private void OnContentGeometryChanged(GeometryChangedEvent e)
        {
            _textArea.TextView.UpdateVisualLines();
            UpdateScrollerRanges();
        }

        private void OnWheel(WheelEvent e)
        {
            var offset = _textArea.TextView.ScrollOffset;
            var lineHeight = _textArea.TextView.DefaultLineHeight;
            var newY = offset.y + e.delta.y * lineHeight * 3;
            newY = Mathf.Clamp(newY, 0, Mathf.Max(0, _textArea.TextView.ScrollExtent.y - _textArea.TextView.ScrollViewport.y));
            _textArea.TextView.ScrollOffset = new Vector2(offset.x, newY);
            _textArea.TextView.UpdateVisualLines();
            UpdateScrollerRanges();
            e.StopPropagation();
        }

        private void UpdateScrollerRanges()
        {
            var extent = _textArea.TextView.ScrollExtent;
            var viewport = _textArea.TextView.ScrollViewport;
            var offset = _textArea.TextView.ScrollOffset;

            float vMax = Mathf.Max(0, extent.y - viewport.y);
            _verticalScroller.lowValue = 0;
            _verticalScroller.slider.pageSize = viewport.y;
            _verticalScroller.highValue = vMax;
            if (!_updatingScroll)
            {
                _updatingScroll = true;
                _verticalScroller.value = offset.y;
                _updatingScroll = false;
            }
            float vFactor = extent.y > 0 ? Mathf.Clamp01(viewport.y / extent.y) : 1f;
            _verticalScroller.Adjust(vFactor);
            _verticalScroller.style.display = (vMax > 0 && _verticalScrollBarVisibility != ScrollBarVisibility.Hidden)
                ? DisplayStyle.Flex : DisplayStyle.None;

            float hMax = Mathf.Max(0, extent.x - viewport.x);
            _horizontalScroller.lowValue = 0;
            _horizontalScroller.slider.pageSize = viewport.x;
            _horizontalScroller.highValue = hMax;
            if (!_updatingScroll)
            {
                _updatingScroll = true;
                _horizontalScroller.value = offset.x;
                _updatingScroll = false;
            }
            float hFactor = extent.x > 0 ? Mathf.Clamp01(viewport.x / extent.x) : 1f;
            _horizontalScroller.Adjust(hFactor);
            _horizontalScroller.style.display = (hMax > 0 && _horizontalScrollBarVisibility != ScrollBarVisibility.Hidden)
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnVerticalScrollChanged(float value)
        {
            if (_updatingScroll) return;
            _updatingScroll = true;
            try
            {
                var offset = _textArea.TextView.ScrollOffset;
                _textArea.TextView.ScrollOffset = new Vector2(offset.x, value);
                _textArea.TextView.UpdateVisualLines();
                UpdateScrollerRanges();
            }
            finally
            {
                _updatingScroll = false;
            }
        }

        private void OnHorizontalScrollChanged(float value)
        {
            if (_updatingScroll) return;
            _updatingScroll = true;
            try
            {
                var offset = _textArea.TextView.ScrollOffset;
                _textArea.TextView.ScrollOffset = new Vector2(value, offset.y);
                _textArea.TextView.UpdateVisualLines();
                UpdateScrollerRanges();
            }
            finally
            {
                _updatingScroll = false;
            }
        }

        private void OnVisualLinesChanged(object sender, EventArgs e)
        {
            UpdateScrollerRanges();
        }

        private void OnTextViewScrollOffsetChanged(object sender, EventArgs e)
        {
            if (_updatingScroll) return;
            _updatingScroll = true;
            try
            {
                UpdateScrollerRanges();
            }
            finally
            {
                _updatingScroll = false;
            }
        }

        #endregion

        #region FocusHandling

        private void OnFocusIn(FocusInEvent e)
        {
            _textArea.Focus();
        }

        #endregion

        #region Document property
        /// <summary>
        /// Gets/Sets the document displayed by the text editor.
        /// This is a dependency property.
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

        /// <summary>
        /// Occurs when the document property has changed.
        /// </summary>
        internal event EventHandler<DocumentChangedEventArgs> DocumentChanged;

        /// <summary>
        /// Raises the <see cref="DocumentChanged"/> event.
        /// </summary>
        protected virtual void OnDocumentChanged(DocumentChangedEventArgs e)
        {
            DocumentChanged?.Invoke(this, e);
        }

        private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
        {
            if (oldValue != null)
            {
                TextDocumentWeakEventManager.TextChanged.RemoveHandler(oldValue, OnTextChanged);
                PropertyChangedWeakEventManager.RemoveHandler(oldValue.UndoStack, OnUndoStackPropertyChangedHandler);
            }
            _textArea.Document = newValue;
            if (newValue != null)
            {
                TextDocumentWeakEventManager.TextChanged.AddHandler(newValue, OnTextChanged);
                PropertyChangedWeakEventManager.AddHandler(newValue.UndoStack, OnUndoStackPropertyChangedHandler);
            }

            ResetScrollAndUpdate();

            OnDocumentChanged(new DocumentChangedEventArgs(oldValue, newValue));
            OnTextChanged(EventArgs.Empty);
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
                OnOptionsPropertyChanged(oldValue, value);
            }
        }

        /// <summary>
        /// Occurs when a text editor option has changed.
        /// </summary>
        internal event PropertyChangedEventHandler OptionChanged;

        /// <summary>
        /// Raises the <see cref="OptionChanged"/> event.
        /// </summary>
        protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
        {
            OptionChanged?.Invoke(this, e);
        }

        private void OnOptionsPropertyChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
        {
            if (oldValue != null)
            {
                PropertyChangedWeakEventManager.RemoveHandler(oldValue, OnPropertyChangedHandler);
            }
            _textArea.Options = newValue;
            if (newValue != null)
            {
                PropertyChangedWeakEventManager.AddHandler(newValue, OnPropertyChangedHandler);
            }
            OnOptionChanged(new PropertyChangedEventArgs(null));
        }

        private void OnPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            OnOptionChanged(e);
        }

        private void OnUndoStackPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsOriginalFile")
            {
                HandleIsOriginalChanged(e);
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {
            OnTextChanged(e);
        }

        #endregion

        #region Text property
        /// <summary>
        /// Gets/Sets the text of the current document.
        /// </summary>
        internal string Text
        {
            get
            {
                var document = Document;
                return document != null ? document.Text : string.Empty;
            }
            set
            {
                var document = GetDocument();
                document.Text = value ?? string.Empty;
                CaretOffset = 0;
                document.UndoStack.ClearAll();
            }
        }

        private TextDocument GetDocument()
        {
            var document = Document;
            if (document == null)
                throw ThrowUtil.NoDocumentAssigned();
            return document;
        }

        /// <summary>
        /// Occurs when the Text property changes.
        /// </summary>
        internal event EventHandler TextChanged;

        /// <summary>
        /// Raises the <see cref="TextChanged"/> event.
        /// </summary>
        protected virtual void OnTextChanged(EventArgs e)
        {
            TextChanged?.Invoke(this, e);
        }
        #endregion

        #region TextArea / ScrollViewer properties

        /// <summary>
        /// Gets the text area.
        /// </summary>
        internal TextArea TextArea => _textArea;

        internal int FontSize
        {
            get { return _textArea.TextView.FontSize; }
            set
            {
                _textArea.TextView.FontSize = value;
            }
        }

        internal Font Font
        {
            get { return _textArea.TextView.Font; }
            set
            {
                _textArea.TextView.Font = value;
            }
        }

        #endregion

        #region WordWrap
        /// <summary>
        /// Specifies whether the text editor uses word wrapping.
        /// </summary>
        /// <remarks>
        /// Setting WordWrap=true has the same effect as setting HorizontalScrollBarVisibility=Disabled and will override the
        /// HorizontalScrollBarVisibility setting.
        /// </remarks>
        internal bool WordWrap
        {
            get { return _wordWrap;}
            set
            {
                if (_wordWrap != value)
                {
                    _wordWrap = value;
                    OnWordWrapChanged();
                }
            }
        }

        private void OnWordWrapChanged()
        {
            if (WordWrap)
            {
                _horizontalScrollBarVisibilityBck = HorizontalScrollBarVisibility;
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
            else
            {
                HorizontalScrollBarVisibility = _horizontalScrollBarVisibilityBck;
            }

            _textArea.TextView.CanHorizontallyScroll = !WordWrap;
        }

        #endregion

        #region IsReadOnly

        /// <summary>
        /// Specifies whether the user can change the text editor content.
        /// Setting this property will replace the
        /// <see cref="Editing.TextArea.ReadOnlySectionProvider">TextArea.ReadOnlySectionProvider</see>.
        /// </summary>
        internal bool IsReadOnly
        {
            get { return _isReadonly; }
            set
            {
                if (_isReadonly != value)
                {
                    _isReadonly = value;
                    OnIsReadOnlyChanged(value);
                }
            }
        }

        private void OnIsReadOnlyChanged(bool isReadonly)
        {
                _textArea.ReadOnlySectionProvider = isReadonly ?
                    ReadOnlySectionDocument.Instance :
                    NoReadOnlySections.Instance;
        }
        #endregion

        #region IsModified

        /// <summary>
        /// Gets/Sets the 'modified' flag.
        /// </summary>
        internal bool IsModified
        {
            get { return _isModified; }
            set
            {
                if (_isModified != value)
                {
                    _isModified = value;
                    OnIsModifiedChanged(value);
                }
            }
        }

        private void OnIsModifiedChanged(bool newValue)
        {
            var document = Document;
            if (document != null)
            {
                var undoStack = document.UndoStack;
                if (newValue)
                {
                    if (undoStack.IsOriginalFile)
                        undoStack.DiscardOriginalFileMarker();
                }
                else
                {
                    undoStack.MarkAsOriginalFile();
                }
            }
        }

        private void HandleIsOriginalChanged(PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsOriginalFile")
            {
                var document = Document;
                if (document != null)
                {
                    IsModified = !document.UndoStack.IsOriginalFile;
                }
            }
        }
        #endregion

        #region ShowLineNumbers
        /// <summary>
        /// Specifies whether line numbers are shown on the left to the text view.
        /// </summary>
        internal bool ShowLineNumbers
        {
            get { return _showLineNumbers; }
            set
            {
                if (_showLineNumbers != value)
                {
                    _showLineNumbers = value;
                    OnShowLineNumbersChanged();
                }
            }
        }

        private void OnShowLineNumbersChanged()
        {
            var leftMargins = _textArea.LeftMargins;
            if (_showLineNumbers)
            {
                var lineNumbers = new LineNumberMargin();
                leftMargins.Insert(0, lineNumbers);
            }
            else
            {
                for (var i = 0; i < leftMargins.Count; i++)
                {
                    if (leftMargins[i] is LineNumberMargin)
                    {
                        leftMargins.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        #endregion

        #region TextBoxBase-like methods
        /// <summary>
        /// Appends text to the end of the document.
        /// </summary>
        internal void AppendText(string textData)
        {
            var document = GetDocument();
            document.Insert(document.TextLength, textData);
        }

        /// <summary>
        /// Begins a group of document changes.
        /// </summary>
        internal void BeginChange()
        {
            GetDocument().BeginUpdate();
        }

        /// <summary>
        /// Begins a group of document changes and returns an object that ends the group of document
        /// changes when it is disposed.
        /// </summary>
        internal IDisposable DeclareChangeBlock()
        {
            return GetDocument().RunUpdate();
        }

        /// <summary>
        /// Ends the current group of document changes.
        /// </summary>
        internal void EndChange()
        {
            GetDocument().EndUpdate();
        }

        /// <summary>
        /// Gets the vertical size of the document.
        /// </summary>
        internal double ExtentHeight => _textArea.TextView.ScrollExtent.y;

        /// <summary>
        /// Gets the horizontal size of the current document region.
        /// </summary>
        internal double ExtentWidth => _textArea.TextView.ScrollExtent.x;

        /// <summary>
        /// Gets the vertical size of the viewport.
        /// </summary>
        internal double ViewportHeight => _textArea.TextView.ScrollViewport.y;

        /// <summary>
        /// Gets the horizontal size of the viewport.
        /// </summary>
        internal double ViewportWidth => _textArea.TextView.ScrollViewport.x;

        /// <summary>
        /// Gets the vertical scroll position.
        /// </summary>
        internal float VerticalOffset => _textArea.TextView.ScrollOffset.y;

        /// <summary>
        /// Gets the horizontal scroll position.
        /// </summary>
        internal float HorizontalOffset => _textArea.TextView.ScrollOffset.x;

        #endregion

        #region TextBox methods
        /// <summary>
        /// Gets/Sets the selected text.
        /// </summary>
        internal string SelectedText
        {
            get
            {
                if (_textArea.Document != null && !_textArea.Selection.IsEmpty)
                    return _textArea.Document.GetText(_textArea.Selection.SurroundingSegment);
                return string.Empty;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                var textArea = TextArea;
                if (textArea.Document != null)
                {
                    var offset = SelectionStart;
                    var length = SelectionLength;
                    textArea.Document.Replace(offset, length, value);
                    textArea.Selection = Selection.Create(textArea, offset, offset + value.Length);
                }
            }
        }

        /// <summary>
        /// Gets/sets the caret position.
        /// </summary>
        internal int CaretOffset
        {
            get
            {
                return _textArea.Caret.Offset;
            }
            set
            {
                _textArea.Caret.Offset = value;
            }
        }

        /// <summary>
        /// Gets/sets the start position of the selection.
        /// </summary>
        internal int SelectionStart
        {
            get
            {
                if (_textArea.Selection.IsEmpty)
                    return _textArea.Caret.Offset;
                else
                    return _textArea.Selection.SurroundingSegment.Offset;
            }
            set => Select(value, SelectionLength);
        }

        /// <summary>
        /// Gets/sets the length of the selection.
        /// </summary>
        internal int SelectionLength
        {
            get
            {
                if (!_textArea.Selection.IsEmpty)
                    return _textArea.Selection.SurroundingSegment.Length;
                else
                    return 0;
            }
            set => Select(SelectionStart, value);
        }

        /// <summary>
        /// Selects the specified text section.
        /// </summary>
        internal void Select(int start, int length)
        {
            var documentLength = Document?.TextLength ?? 0;
            if (start < 0 || start > documentLength)
                throw new ArgumentOutOfRangeException(nameof(start), start, "Value must be between 0 and " + documentLength);
            if (length < 0 || start + length > documentLength)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Value must be between 0 and " + (documentLength - start));
            TextArea.Selection = Selection.Create(TextArea, start, start + length);
            TextArea.Caret.Offset = start + length;
        }

        /// <summary>
        /// Gets the number of lines in the document.
        /// </summary>
        internal int LineCount
        {
            get
            {
                var document = Document;
                if (document != null)
                    return document.LineCount;
                return 1;
            }
        }

        /// <summary>
        /// Clears the text.
        /// </summary>
        internal void ClearText()
        {
            Text = string.Empty;
        }
        #endregion

        #region Loading from stream
        /// <summary>
        /// Loads the text from the stream, auto-detecting the encoding.
        /// </summary>
        /// <remarks>
        /// This method sets <see cref="IsModified"/> to false.
        /// </remarks>
        internal void Load(Stream stream)
        {
            using (var reader = FileReader.OpenStream(stream, Encoding ?? Encoding.UTF8))
            {
                Text = reader.ReadToEnd();
                Encoding = reader.CurrentEncoding;
            }

            ResetScrollAndUpdate();

            IsModified = false;
        }

        private void ResetScrollAndUpdate()
        {
            _textArea.TextView.ScrollOffset = Vector2.zero;
            _textArea.TextView.UpdateVisualLines();
            UpdateScrollerRanges();
        }

        /// <summary>
        /// Loads the text from the stream, auto-detecting the encoding.
        /// </summary>
        internal void Load(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Load(fs);
            }
        }

        /// <summary>
        /// Gets/sets the encoding used when the file is saved.
        /// </summary>
        internal Encoding Encoding { get; set; }

        /// <summary>
        /// Saves the text to the stream.
        /// </summary>
        /// <remarks>
        /// This method sets <see cref="IsModified"/> to false.
        /// </remarks>
        internal void Save(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            var encoding = Encoding;
            var document = Document;
            var writer = encoding != null ? new StreamWriter(stream, encoding) : new StreamWriter(stream);
            document?.WriteTextTo(writer);
            writer.Flush();
            IsModified = false;
        }

        /// <summary>
        /// Saves the text to the file.
        /// </summary>
        internal void Save(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Save(fs);
            }
        }
        #endregion

        #region ScrollBarVisibility
        internal ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return _verticalScrollBarVisibility; }
            set
            {
                _verticalScrollBarVisibility = value;
                UpdateScrollerRanges();
            }
        }

        internal ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return _horizontalScrollBarVisibility; }
            set
            {
                _horizontalScrollBarVisibility = value;
                UpdateScrollerRanges();
            }
        }
        #endregion
    }
}
