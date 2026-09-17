using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.CodeEditor.Document;
using Unity.CodeEditor.Text;
using Unity.CodeEditor.Utils;
using Unity.PlasticSCM.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.CodeEditor.Rendering
{
    internal class TextView : VisualElement
    {
        bool _isMeasureValid;
        private EventHandler _scrollInvalidated;

        private readonly CurrentLineHighlightRenderer _currentLineHighlightRenderer;
        private readonly ColumnRulerRenderer _columnRulerRenderer;

        private TextDocument _document;
        private HeightTree _heightTree;
        private TextEditorOptions _options;
        private bool _defaultTextMetricsValid;
        private float _wideSpaceWidth; // Width of an 'x'. Used as basis for the tab width, and for scrolling.
        private float _defaultLineHeight; // Height of a line containing 'x'. Used for scrolling.
        private float _defaultBaseline; // Baseline of a line containing 'x'. Used for TextTop/TextBottom calculation.
        private List<VisualLine> _allVisualLines = new List<VisualLine>();
        private ReadOnlyCollection<VisualLine> _visibleVisualLines;
        private float _clippedPixelsOnTop;
        private List<VisualLine> _newVisualLines;
        private Vector2 _lastAvailableSize;
        private bool _inMeasure;
        private bool _canVerticallyScroll = true;
        private bool _canHorizontallyScroll = true;
        /// <summary>
        /// Offset of the scroll position.
        /// </summary>
        private Vector2 _scrollOffset;

        /// <summary>
        /// Size of the viewport.
        /// </summary>
        private Vector2 _scrollViewport;

        private Vector2 _scrollExtent;

        private Font _font;
        private int _fontSize = 13;
        private Color _foregroundColor = TextEditorColors.DefaultText;
        private Color _lineNumbersForegroundColor = TextEditorColors.LineNumbersForeground;
        private Color _lineNumbersSeparatorColor = TextEditorColors.LineNumbersSeparator;
        private Color _columnRulerColor = TextEditorColors.ColumnRuler;
        /// <summary>
        /// Gets the currently visible visual lines.
        /// </summary>
        /// <exception cref="VisualLinesInvalidException">
        /// Gets thrown if there are invalid visual lines when this property is accessed.
        /// You can use the <see cref="VisualLinesValid"/> property to check for this case,
        /// or use the <see cref="EnsureVisualLines()"/> method to force creating the visual lines
        /// when they are invalid.
        /// </exception>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        internal ReadOnlyCollection<VisualLine> VisualLines
        {
            get
            {
                if (_visibleVisualLines == null)
                    throw new VisualLinesInvalidException();
                return _visibleVisualLines;
            }
        }

        internal Rect Bounds => new Rect(0, 0, resolvedStyle.width, resolvedStyle.height);

        internal Vector2 ScrollViewport
        {
            get
            {
                return _scrollViewport;
            }
        }

        internal Vector2 ScrollExtent
        {
            get
            {
                return _scrollExtent;
            }
        }

        /// <summary>
        /// Gets the horizontal scroll offset.
        /// </summary>
        internal float HorizontalOffset => _scrollOffset.x;

        /// <summary>
        /// Gets the vertical scroll offset.
        /// </summary>
        internal float VerticalOffset => _scrollOffset.y;

        /// <summary>
        /// Gets the scroll offset;
        /// </summary>
        internal Vector2 ScrollOffset
        {
            get { return _scrollOffset; }
            set
            {
                value = new Vector2(ValidateVisualOffset(value.x), ValidateVisualOffset(value.y));
                var isX = !_scrollOffset.x.IsClose(value.x);
                var isY = !_scrollOffset.y.IsClose(value.y);
                if (isX || isY)
                {
                    SetScrollOffset(value);

                    if (isX)
                    {
                        InvalidateVisual();
                    }

                    InvalidateMeasure();
                }
            }
        }

        #region colors

        internal Color LineNumbersForegroundColor
        {
            get { return _lineNumbersForegroundColor; }
            set
            {
                if (_lineNumbersForegroundColor != value)
                {
                    _lineNumbersForegroundColor = value;
                    Redraw();
                }
            }
        }

        internal Color LineNumbersSeparatorColor
        {
            get { return _lineNumbersSeparatorColor; }
            set
            {
                if (_lineNumbersSeparatorColor != value)
                {
                    _lineNumbersSeparatorColor = value;
                    Redraw();
                }
            }
        }

        /// <summary>
        /// Gets/sets the Color used for displaying non-printable characters.
        /// </summary>
        internal Color NonPrintableCharacterColor { get; set; } = TextEditorColors.NonPrintableCharacter;

        /// <summary>
        /// Gets/Sets the pen used to draw the column ruler.
        /// <seealso cref="TextEditorOptions.ShowColumnRulers"/>
        /// </summary>
        internal Color ColumnRulerColor
        {
            get { return _columnRulerColor;}
            set
            {
                if (_columnRulerColor != value)
                {
                    _columnRulerColor = value;
                    _columnRulerRenderer.SetRuler(Options.ColumnRulerPositions, value);
                    InvalidateVisual();
                }
            }
        }

        #endregion


        internal void InvalidateVisual()
        {
            MarkDirtyRepaint();
        }

        internal void InvalidateMeasure()
        {
            _isMeasureValid = false;
            ScheduleVisualLineUpdate();
        }

        private bool _updateScheduled;

        private void ScheduleVisualLineUpdate()
        {
            if (_updateScheduled)
                return;
            _updateScheduled = true;
            schedule.Execute(() =>
            {
                _updateScheduled = false;
                UpdateVisualLines();
            });
        }

        /// <summary>
        /// Gets/Sets highlighted line number.
        /// </summary>
        internal int HighlightedLine
        {
            get => _currentLineHighlightRenderer.Line;
            set => _currentLineHighlightRenderer.Line = value;
        }

        internal bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll;
            set
            {
                if (_canHorizontallyScroll != value)
                {
                    _canHorizontallyScroll = value;
                    ClearVisualLines();
                    MarkDirtyRepaint();
                }
            }
        }

        internal Font Font
        {
            get { return _font; }
            set
            {
                if (_font != value)
                {
                    _font = value;
                    // changing font properties requires recreating cached elements
                    // and we need to re-measure the font metrics:
                    InvalidateDefaultTextMetrics();
                    Redraw();
                }
            }
        }

        internal int FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    // changing font properties requires recreating cached elements
                    // and we need to re-measure the font metrics:
                    InvalidateDefaultTextMetrics();
                    Redraw();
                }
            }
        }

        internal Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    // changing brushes requires recreating the cached elements
                    Redraw();
                }
            }
        }

        /// <summary>
        /// Additonal amount that allows horizontal scrolling past the end of the longest line.
        /// This is necessary to ensure the caret always is visible, even when it is at the end of the longest line.
        /// </summary>
        private const float AdditionalHorizontalScrollAmount = 25;

        private readonly ObserveAddRemoveCollection<IBackgroundRenderer> _backgroundRenderers;

        /// <summary>
        /// Gets the list of background renderers.
        /// </summary>
        internal IList<IBackgroundRenderer> BackgroundRenderers => _backgroundRenderers;
        private TextFormatter _formatter;

        private TextParagraphProperties CreateParagraphProperties()
        {
            if (_font == null)
                _font = Fonts.GetCascadiaFont();

            return new TextParagraphProperties(
                _canHorizontallyScroll == false,
                Options.IndentationSize * WideSpaceWidth,
                Font,
                FontSize,
                ForegroundColor);

        }

        internal TextView()
        {
            style.flexGrow = 1;
            style.overflow = Overflow.Hidden;
            style.position = Position.Relative;
            style.backgroundColor = TextEditorColors.Background;

            Services.AddService(this);

            TextLayer = new TextLayer(this);
            _elementGenerators = new ObserveAddRemoveCollection<VisualLineElementGenerator>(ElementGenerator_Added, ElementGenerator_Removed);
            _lineTransformers = new ObserveAddRemoveCollection<IVisualLineTransformer>(LineTransformer_Added, LineTransformer_Removed);
            _backgroundRenderers = new ObserveAddRemoveCollection<IBackgroundRenderer>(BackgroundRenderer_Added, BackgroundRenderer_Removed);
            _currentLineHighlightRenderer = new CurrentLineHighlightRenderer(this);
            _columnRulerRenderer = new ColumnRulerRenderer(this);
            Options = new TextEditorOptions();

            Debug.Assert(_singleCharacterElementGenerator != null); // assert that the option change created the builtin element generators

            Layers = new LayerCollection(this);
            InsertLayer(TextLayer, KnownLayer.Text, LayerInsertionPosition.Replace);

            this.SetMouseCursor(MouseCursor.Text);
        }

        #region Layers
        internal readonly TextLayer TextLayer;

        /// <summary>
        /// Gets the list of layers displayed in the text view.
        /// </summary>
        internal LayerCollection Layers { get; }

        internal sealed class LayerCollection : Collection<Layer>
        {
            private readonly TextView _textView;

            internal LayerCollection(TextView textView)
            {
                _textView = textView;
            }

            protected override void ClearItems()
            {
                foreach (var control in Items)
                {
                    control.RemoveFromHierarchy();
                }
                base.ClearItems();
                _textView.LayersChanged();
            }

            protected override void InsertItem(int index, Layer item)
            {
                base.InsertItem(index, item);
                _textView.Insert(index, item);
                _textView.LayersChanged();
            }

            protected override void RemoveItem(int index)
            {
                Items[index].RemoveFromHierarchy();
                base.RemoveItem(index);
                _textView.LayersChanged();
            }

            protected override void SetItem(int index, Layer item)
            {
                Items[index].RemoveFromHierarchy();
                base.SetItem(index, item);
                _textView.Insert(index, item);
                _textView.LayersChanged();
            }
        }

        private void LayersChanged()
        {
            TextLayer.Index = Layers.IndexOf(TextLayer);
        }

        /// <summary>
        /// Inserts a new layer at a position specified relative to an existing layer.
        /// </summary>
        /// <param name="layer">The new layer to insert.</param>
        /// <param name="referencedLayer">The existing layer</param>
        /// <param name="position">Specifies whether the layer is inserted above,below, or replaces the referenced layer</param>
        internal void InsertLayer(Layer layer, KnownLayer referencedLayer, LayerInsertionPosition position)
        {
            if (layer == null)
                throw new ArgumentNullException(nameof(layer));
            if (!Enum.IsDefined(typeof(KnownLayer), referencedLayer))
                throw new ArgumentOutOfRangeException(nameof(referencedLayer), (int)referencedLayer, nameof(KnownLayer));
            if (!Enum.IsDefined(typeof(LayerInsertionPosition), position))
                throw new ArgumentOutOfRangeException(nameof(position), (int)position, nameof(LayerInsertionPosition));
            if (referencedLayer == KnownLayer.Background && position != LayerInsertionPosition.Above)
                throw new InvalidOperationException("Cannot replace or insert below the background layer.");

            var newPosition = new LayerPosition(referencedLayer, position);

            if (Layers == null)
                return;

            layer.LayerPosition = newPosition;
            for (var i = 0; i < Layers.Count; i++)
            {
                var p = Layers[i].LayerPosition;
                if (p != null)
                {
                    if (p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Replace)
                    {
                        // found the referenced layer
                        switch (position)
                        {
                            case LayerInsertionPosition.Below:
                                Layers.Insert(i, layer);
                                return;
                            case LayerInsertionPosition.Above:
                                Layers.Insert(i + 1, layer);
                                return;
                            case LayerInsertionPosition.Replace:
                                Layers[i] = layer;
                                return;
                        }
                    }
                    else if (p.KnownLayer == referencedLayer && p.Position == LayerInsertionPosition.Above
                             || p.KnownLayer > referencedLayer)
                    {
                        // we skipped the insertion position (referenced layer does not exist?)
                        Layers.Insert(i, layer);
                        return;
                    }
                }
            }
            // inserting after all existing layers:
            Layers.Add(layer);
        }

        #endregion

        void Measure(Vector2 availableSize)
        {
            if (!_canHorizontallyScroll && !availableSize.x.IsClose(_lastAvailableSize.x))
            {
                ClearVisualLines();
            }

            _lastAvailableSize = availableSize;

            float maxWidth;
            if (_document == null)
            {
                // no document -> create empty list of lines
                _allVisualLines = new List<VisualLine>();
                _visibleVisualLines = new ReadOnlyCollection<VisualLine>(_allVisualLines.ToArray());
                maxWidth = 0;
            }
            else
            {
                _inMeasure = true;
                try
                {
                    maxWidth = CreateAndMeasureVisualLines(availableSize);
                }
                finally
                {
                    _inMeasure = false;
                }
            }

            //RemoveInlineObjectsNow();

            maxWidth += AdditionalHorizontalScrollAmount;
            var heightTreeHeight = DocumentHeight;
            var options = Options;
            float desiredHeight = Math.Min(availableSize.y, heightTreeHeight);
            float extraHeightToAllowScrollBelowDocument = 0;

            if (options.AllowScrollBelowDocument)
            {
                if (!double.IsInfinity(_scrollViewport.y))
                {
                    // HACK: we need to keep at least Caret.MinimumDistanceToViewBorder visible so that we don't scroll back up when the user types after
                    // scrolling to the very bottom.
                    var minVisibleDocumentHeight = DefaultLineHeight;
                    // increase the extend height to allow scrolling below the document
                    extraHeightToAllowScrollBelowDocument = desiredHeight - minVisibleDocumentHeight;
                }
            }

            TextLayer.SetVisualLines(_visibleVisualLines);

            SetScrollData(availableSize,
                new Vector2(maxWidth, heightTreeHeight + extraHeightToAllowScrollBelowDocument),
                _scrollOffset);

            VisualLinesChanged?.Invoke(this, EventArgs.Empty);
        }

        void Arrange()
        {
        }

        /// <summary>
        /// Build all VisualLines in the visible range.
        /// </summary>
        /// <returns>Width the longest line</returns>
        private float CreateAndMeasureVisualLines(Vector2 availableSize)
        {
            //Debug.Log("Measure availableSize=" + availableSize + ", scrollOffset=" + _scrollOffset);
            var firstLineInView = _heightTree.GetLineByVisualPosition(_scrollOffset.y);

            // number of pixels clipped from the first visual line(s)
            _clippedPixelsOnTop = _scrollOffset.y - _heightTree.GetVisualPosition(firstLineInView);
            // clippedPixelsOnTop should be >= 0, except for floating point inaccurracy.
            Debug.Assert(_clippedPixelsOnTop >= -ExtensionMethods.Epsilon);

            _newVisualLines = new List<VisualLine>();

            VisualLineConstructionStarting?.Invoke(this, new VisualLineConstructionStartEventArgs(firstLineInView));

            var elementGeneratorsArray = _elementGenerators.ToArray();
            var lineTransformersArray = _lineTransformers.ToArray();
            var nextLine = firstLineInView;
            float maxWidth = 0;
            var yPos = -_clippedPixelsOnTop;
            while (yPos < availableSize.y && nextLine != null)
            {
                var visualLine = GetVisualLine(nextLine.LineNumber) ??
                                        BuildVisualLine(nextLine,
                                            CreateParagraphProperties(),
                                            elementGeneratorsArray,
                                            lineTransformersArray,
                                            availableSize);

                visualLine.VisualTop = _scrollOffset.y + yPos;

                nextLine = visualLine.LastDocumentLine.NextLine;

                yPos += visualLine.Height;

                foreach (var textLine in visualLine.TextLines)
                {
                    if (textLine.WidthIncludingTrailingWhitespace > maxWidth)
                        maxWidth = textLine.WidthIncludingTrailingWhitespace;
                }

                _newVisualLines.Add(visualLine);
            }

            foreach (var line in _allVisualLines)
            {
                Debug.Assert(line.IsDisposed == false);
                if (!_newVisualLines.Contains(line))
                    DisposeVisualLine(line);
            }

            _allVisualLines = _newVisualLines;
            // visibleVisualLines = readonly copy of visual lines
            _visibleVisualLines = new ReadOnlyCollection<VisualLine>(_newVisualLines.ToArray());
            _newVisualLines = null;

            if (_allVisualLines.Any(line => line.IsDisposed))
            {
                throw new InvalidOperationException("A visual line was disposed even though it is still in use.\n" +
                                                    "This can happen when Redraw() is called during measure for lines " +
                                                    "that are already constructed.");
            }
            return maxWidth;
        }

        #region BuildVisualLine
        private VisualLine BuildVisualLine(DocumentLine documentLine,
                                   TextParagraphProperties paragraphProperties,
                                   IReadOnlyList<VisualLineElementGenerator> elementGeneratorsArray,
                                   IReadOnlyList<IVisualLineTransformer> lineTransformersArray,
                                   Vector2 availableSize)
        {
            if (_heightTree.GetIsCollapsed(documentLine.LineNumber))
                throw new InvalidOperationException("Trying to build visual line from collapsed line");

            //Debug.Log("Building line " + documentLine.LineNumber);

            VisualLine visualLine = new VisualLine(this, documentLine);
            VisualLineTextSource textSource = new VisualLineTextSource(visualLine)
            {
                Document = _document,
                TextParagraphProperties = paragraphProperties,
                TextView = this
            };

            visualLine.ConstructVisualElements(textSource, elementGeneratorsArray);

            if (visualLine.FirstDocumentLine != visualLine.LastDocumentLine)
            {
                // Check whether the lines are collapsed correctly:
                double firstLinePos = _heightTree.GetVisualPosition(visualLine.FirstDocumentLine.NextLine);
                double lastLinePos = _heightTree.GetVisualPosition(visualLine.LastDocumentLine.NextLine ?? visualLine.LastDocumentLine);
                if (!firstLinePos.IsClose(lastLinePos))
                {
                    for (int i = visualLine.FirstDocumentLine.LineNumber + 1; i <= visualLine.LastDocumentLine.LineNumber; i++)
                    {
                        if (!_heightTree.GetIsCollapsed(i))
                            throw new InvalidOperationException("Line " + i + " was skipped by a VisualLineElementGenerator, but it is not collapsed.");
                    }
                    throw new InvalidOperationException("All lines collapsed but visual pos different - height tree inconsistency?");
                }
            }

            visualLine.RunTransformers(textSource, lineTransformersArray);

            // now construct textLines:
            var textOffset = 0;
            var textLines = new List<TextLine>();

            while (textOffset <= visualLine.VisualLengthWithEndOfLineMarker)
            {
                var textLine = _formatter.FormatLine(
                    textSource,
                    textOffset,
                    availableSize.x,
                    paragraphProperties);

                textLines.Add(textLine);
                textOffset += textLine.Length;

                // exit loop so that we don't do the indentation calculation if there's only a single line
                if (textOffset >= visualLine.VisualLengthWithEndOfLineMarker)
                    break;

                if (paragraphProperties.FirstLineInParagraph)
                {
                    paragraphProperties.FirstLineInParagraph = false;

                    TextEditorOptions options = this.Options;
                    double indentation = 0;
                    if (options.InheritWordWrapIndentation)
                    {
                        // determine indentation for next line:
                        int indentVisualColumn = GetIndentationVisualColumn(visualLine);
                        if (indentVisualColumn > 0 && indentVisualColumn < textOffset)
                        {
                            indentation = textLine.GetDistanceFromCharacterHit(new CharacterHit(indentVisualColumn, 0));
                        }
                    }
                    indentation += options.WordWrapIndentation;
                    // apply the calculated indentation unless it's more than half of the text editor size:
                    if (indentation > 0 && indentation * 2 < availableSize.x)
                        paragraphProperties.Indent = indentation;
                }
            }
            visualLine.SetTextLines(textLines);
            _heightTree.SetHeight(visualLine.FirstDocumentLine, visualLine.Height);
            return visualLine;
        }

        private static int GetIndentationVisualColumn(VisualLine visualLine)
        {
            if (visualLine.Elements.Count == 0)
                return 0;
            var column = 0;
            var elementIndex = 0;
            var element = visualLine.Elements[elementIndex];
            while (element.IsWhitespace(column))
            {
                column++;
                if (column == element.VisualColumn + element.VisualLength)
                {
                    elementIndex++;
                    if (elementIndex == visualLine.Elements.Count)
                        break;
                    element = visualLine.Elements[elementIndex];
                }
            }
            return column;
        }
        #endregion

        private void BackgroundRenderer_Added(IBackgroundRenderer renderer)
        {
            ConnectToTextView(renderer);
            InvalidateLayer(renderer.Layer);
        }

        private void BackgroundRenderer_Removed(IBackgroundRenderer renderer)
        {
            DisconnectFromTextView(renderer);
            InvalidateLayer(renderer.Layer);
        }

        /// <summary>
        /// Updates visual lines for the current viewport. Called when scroll offset
        /// changes or when the element size changes.
        /// </summary>
        internal void UpdateVisualLines()
        {
            var availableSize = new Vector2(resolvedStyle.width, resolvedStyle.height);

            if (float.IsNaN(availableSize.x) || float.IsNaN(availableSize.y))
                return;

            if (availableSize.x <= 0 || availableSize.y <= 0)
                return;

            if (_isMeasureValid && _lastAvailableSize.IsClose(availableSize))
                return;

            Measure(availableSize);
            Arrange();

            _isMeasureValid = true;
        }

        internal void RenderBackground(KnownLayer layer)
        {
            foreach (var bg in _backgroundRenderers)
            {
                if (bg.Layer == layer)
                {
                    bg.Draw(this);
                }
            }
        }

        internal void ArrangeTextLayer(IList<VisualLineDrawingVisual> visuals)
        {
            var pos = new Vector2(-_scrollOffset.x, -_clippedPixelsOnTop);
            foreach (var visual in visuals)
            {
                var t = visual.RenderTransform;
                if (t == null || t.x != pos.x || t.y != pos.y)
                {
                    visual.RenderTransform = new Vector2(pos.x, pos.y);
                }
                pos = new Vector2(pos.x, pos.y + visual.LineHeight);
            }
        }

        #region Document Property
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

        /// <summary>
        /// Occurs when the document property has changed.
        /// </summary>
        internal event EventHandler<DocumentChangedEventArgs> DocumentChanged;

        private void OnDocumentChanged(TextDocument oldValue, TextDocument newValue)
        {
            if (oldValue != null)
            {
                _heightTree.Dispose();
                _heightTree = null;
                _formatter = null;
                TextDocumentWeakEventManager.Changing.RemoveHandler(oldValue, OnChanging);
            }
            _document = newValue;
            ClearScrollData();
            ClearVisualLines();
            if (newValue != null)
            {
                TextDocumentWeakEventManager.Changing.AddHandler(newValue, OnChanging);
                _formatter = new TextFormatter();
                InvalidateDefaultTextMetrics(); // measuring DefaultLineHeight depends on formatter
                _heightTree = new HeightTree(newValue, DefaultLineHeight);
            }

            MarkDirtyRepaint();
            DocumentChanged?.Invoke(this, new DocumentChangedEventArgs(oldValue, newValue));
        }

        private void OnChanging(object sender, DocumentChangeEventArgs e)
        {
            Redraw(e.Offset, e.RemovalLength);
        }

        #endregion

        #region Options property

        /// <summary>
        /// Gets/Sets the options used by the text editor.
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

        /// <summary>
        /// Raises the <see cref="OptionChanged"/> event.
        /// </summary>
        protected virtual void OnOptionChanged(PropertyChangedEventArgs e)
        {
            OptionChanged?.Invoke(this, e);

            if (Options.ShowColumnRulers)
                _columnRulerRenderer.SetRuler(Options.ColumnRulerPositions, ColumnRulerColor);
            else
                _columnRulerRenderer.SetRuler(null, ColumnRulerColor);

            UpdateBuiltinElementGeneratorsFromOptions();
            Redraw();
        }

        private void OnOptionsChanged(TextEditorOptions oldValue, TextEditorOptions newValue)
        {
            if (oldValue != null)
            {
                PropertyChangedWeakEventManager.RemoveHandler(oldValue, OnPropertyChanged);
            }
            if (newValue != null)
            {
                PropertyChangedWeakEventManager.AddHandler(newValue, OnPropertyChanged);
            }
            OnOptionChanged(new PropertyChangedEventArgs(null));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnOptionChanged(e);
        }

        #endregion

        #region Service Provider

        /// <summary>
        /// Gets a service container used to associate services with the text view.
        /// </summary>
        internal IServiceContainer Services { get; } = new ServiceContainer();

        #endregion

        #region ElementGenerators+LineTransformers Properties

        private readonly ObserveAddRemoveCollection<VisualLineElementGenerator> _elementGenerators;

        /// <summary>
        /// Gets a collection where element generators can be registered.
        /// </summary>
        internal IList<VisualLineElementGenerator> ElementGenerators => _elementGenerators;

        private void ElementGenerator_Added(VisualLineElementGenerator generator)
        {
            ConnectToTextView(generator);
            Redraw();
        }

        private void ElementGenerator_Removed(VisualLineElementGenerator generator)
        {
            DisconnectFromTextView(generator);
            Redraw();
        }

        private readonly ObserveAddRemoveCollection<IVisualLineTransformer> _lineTransformers;

        /// <summary>
        /// Gets a collection where line transformers can be registered.
        /// </summary>
        internal IList<IVisualLineTransformer> LineTransformers => _lineTransformers;

        private void LineTransformer_Added(IVisualLineTransformer lineTransformer)
        {
            ConnectToTextView(lineTransformer);
            Redraw();
        }

        private void LineTransformer_Removed(IVisualLineTransformer lineTransformer)
        {
            DisconnectFromTextView(lineTransformer);
            Redraw();
        }
        #endregion

        #region Builtin ElementGenerators
        //		NewLineElementGenerator newLineElementGenerator;
        private SingleCharacterElementGenerator _singleCharacterElementGenerator;

        private LinkElementGenerator _linkElementGenerator;
        private MailLinkElementGenerator _mailLinkElementGenerator;
        TextFormatter mFormatter;

        private void UpdateBuiltinElementGeneratorsFromOptions()
        {
            var options = Options;

            //			AddRemoveDefaultElementGeneratorOnDemand(ref newLineElementGenerator, options.ShowEndOfLine);
            AddRemoveDefaultElementGeneratorOnDemand(ref _singleCharacterElementGenerator, true);
            AddRemoveDefaultElementGeneratorOnDemand(ref _linkElementGenerator, options.EnableHyperlinks);
            AddRemoveDefaultElementGeneratorOnDemand(ref _mailLinkElementGenerator, options.EnableEmailHyperlinks);
        }

        private void AddRemoveDefaultElementGeneratorOnDemand<T>(ref T generator, bool demand)
            where T : VisualLineElementGenerator, IBuiltinElementGenerator, new()
        {
            var hasGenerator = generator != null;
            if (hasGenerator != demand)
            {
                if (demand)
                {
                    generator = new T();
                    ElementGenerators.Add(generator);
                }
                else
                {
                    ElementGenerators.Remove(generator);
                    generator = null;
                }
            }
            generator?.FetchOptions(Options);
        }
        #endregion

        /// <summary>
        /// Gets the visual line that contains the document line with the specified number.
        /// If that line is outside the visible range, a new VisualLine for that document line is constructed.
        /// </summary>
        internal VisualLine GetOrConstructVisualLine(DocumentLine documentLine)
        {
            if (documentLine == null)
                throw new ArgumentNullException("documentLine");
            if (!this.Document.Lines.Contains(documentLine))
                throw new InvalidOperationException("Line belongs to wrong document");
            EditorDispatcher.VerifyMainThreadAccess();

            VisualLine l = GetVisualLine(documentLine.LineNumber);
            if (l == null)
            {
                TextParagraphProperties paragraphProperties = CreateParagraphProperties();

                while (_heightTree.GetIsCollapsed(documentLine.LineNumber))
                {
                    documentLine = documentLine.PreviousLine;
                }

                l = BuildVisualLine(documentLine,
                                    paragraphProperties,
                                    _elementGenerators.ToArray(),
                                    _lineTransformers.ToArray(),
                                    _lastAvailableSize);
                _allVisualLines.Add(l);
                // update all visual top values (building the line might have changed visual top of other lines due to word wrapping)
                foreach (var line in _allVisualLines)
                {
                    line.VisualTop = _heightTree.GetVisualPosition(line.FirstDocumentLine);
                }
            }
            return l;
        }

        #region Redraw methods / VisualLine invalidation
        /// <summary>
        /// Causes the text editor to regenerate all visual lines.
        /// </summary>
        internal void Redraw()
        {
            EditorDispatcher.VerifyMainThreadAccess();
            ClearVisualLines();
            InvalidateMeasure();
        }

        /// <summary>
        /// Causes the text editor to regenerate the specified visual line.
        /// </summary>
        internal void Redraw(VisualLine visualLine)
        {
            EditorDispatcher.VerifyMainThreadAccess();
            if (_allVisualLines.Remove(visualLine))
            {
                DisposeVisualLine(visualLine);
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Causes the text editor to redraw all lines overlapping with the specified segment.
        /// </summary>
        internal void Redraw(int offset, int length)
        {
            EditorDispatcher.VerifyMainThreadAccess();
            var changedSomethingBeforeOrInLine = false;
            for (var i = 0; i < _allVisualLines.Count; i++)
            {
                try
                {
                    var visualLine = _allVisualLines[i];
                    var lineStart = visualLine.FirstDocumentLine.Offset;
                    var lineEnd = visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength;
                    if (offset <= lineEnd)
                    {
                        changedSomethingBeforeOrInLine = true;

                        if (offset + length >= lineStart)
                        {
                            _allVisualLines.RemoveAt(i--);
                            DisposeVisualLine(visualLine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            if (changedSomethingBeforeOrInLine)
            {
                // Repaint not only when something in visible area was changed, but also when anything in front of it
                // was changed. We might have to redraw the line number margin. Or the highlighting changed.
                // However, we'll try to reuse the existing VisualLines.
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Causes a known layer to redraw.
        /// This method does not invalidate visual lines;
        /// use the <see cref="Redraw()"/> method to do that.
        /// </summary>
        internal void InvalidateLayer(KnownLayer knownLayer)
        {
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Causes the text editor to redraw all lines overlapping with the specified segment.
        /// Does nothing if segment is null.
        /// </summary>
        internal void Redraw(ISegment segment)
        {
            if (segment != null)
            {
                Redraw(segment.Offset, segment.Length);
            }
        }

        /// <summary>
        /// Invalidates all visual lines.
        /// The caller of ClearVisualLines() must also call InvalidateMeasure() to ensure
        /// that the visual lines will be recreated.
        /// </summary>
        private void ClearVisualLines()
        {
            if (_allVisualLines.Count != 0)
            {
                foreach (var visualLine in _allVisualLines)
                {
                    DisposeVisualLine(visualLine);
                }
                _allVisualLines.Clear();

                _visibleVisualLines = new ReadOnlyCollection<VisualLine>(_allVisualLines.ToArray());
            }
        }

        private void DisposeVisualLine(VisualLine visualLine)
        {
            if (_newVisualLines != null && _newVisualLines.Contains(visualLine))
            {
                throw new ArgumentException("Cannot dispose visual line because it is in construction!");
            }

            visualLine.Dispose();
            //RemoveInlineObjects(visualLine);
        }
        #endregion

        #region Get(OrConstruct)VisualLine
        /// <summary>
        /// Gets the visual line that contains the document line with the specified number.
        /// Returns null if the document line is outside the visible range.
        /// </summary>
        internal VisualLine GetVisualLine(int documentLineNumber)
        {
            // TODO: EnsureVisualLines() ?
            foreach (var visualLine in _allVisualLines)
            {
                Debug.Assert(visualLine.IsDisposed == false);
                var start = visualLine.FirstDocumentLine.LineNumber;
                var end = visualLine.LastDocumentLine.LineNumber;
                if (documentLineNumber >= start && documentLineNumber <= end)
                    return visualLine;
            }
            return null;
        }
        #endregion

        // Kept for backward compatibility with code that references visual line styling
        internal GUIStyle VisualLineStyle { get; set; } = new GUIStyle();

        /// <summary>
        /// Retrieves a service from the text view.
        /// If the service is not found in the <see cref="Services"/> container,
        /// this method will also look for it in the current document's service provider.
        /// </summary>
        internal virtual object GetService(Type serviceType)
        {
            var instance = Services.GetService(serviceType);
            if (instance == null && _document != null)
            {
                instance = _document.ServiceProvider.GetService(serviceType);
            }
            return instance;
        }

        private void ConnectToTextView(object obj)
        {
            var c = obj as ITextViewConnect;
            c?.AddToTextView(this);
        }

        private void DisconnectFromTextView(object obj)
        {
            var c = obj as ITextViewConnect;
            c?.RemoveFromTextView(this);
        }

        /// <summary>
        /// Gets the width of a 'wide space' (the space width used for calculating the tab size).
        /// </summary>
        /// <remarks>
        /// This is the width of an 'x' in the current font.
        /// We do not measure the width of an actual space as that would lead to tiny tabs in
        /// some proportional fonts.
        /// For monospaced fonts, this property will return the expected value, as 'x' and ' ' have the same width.
        /// </remarks>
        internal float WideSpaceWidth
        {
            get
            {
                CalculateDefaultTextMetrics();
                return _wideSpaceWidth;
            }
        }

        /// <summary>
        /// Gets the default line height. This is the height of an empty line or a line containing regular text.
        /// Lines that include formatted text or custom UI elements may have a different line height.
        /// </summary>
        internal float DefaultLineHeight
        {
            get
            {
                CalculateDefaultTextMetrics();
                return _defaultLineHeight;
            }
        }

        private void InvalidateDefaultTextMetrics()
        {
            _defaultTextMetricsValid = false;
            if (_heightTree != null)
            {
                // calculate immediately so that height tree gets updated
                CalculateDefaultTextMetrics();
            }
        }

        private void CalculateDefaultTextMetrics()
        {
            if (_defaultTextMetricsValid)
                return;
            _defaultTextMetricsValid = true;

            if (_formatter != null)
            {
                var paragraphProperties = CreateParagraphProperties();
                var size = _formatter.CalcSize(
                    "x",
                    paragraphProperties);

                _wideSpaceWidth = Math.Max(1, size.x);
                _defaultLineHeight = Math.Max(1, Mathf.Round(size.y));
            }

            // Update heightTree.DefaultLineHeight, if a document is loaded.
            if (_heightTree != null)
                _heightTree.DefaultLineHeight = _defaultLineHeight;
        }

        private static float ValidateVisualOffset(float offset)
        {
            if (float.IsNaN(offset))
                throw new ArgumentException("offset must not be NaN");
            if (offset < 0)
                return 0;
            return offset;
        }

        /// <summary>
        /// Scrolls the text view so that the specified rectangle gets visible.
        /// </summary>
        internal virtual void MakeVisible(Rect rectangle)
        {
            var visibleRectangle = new Rect(_scrollOffset.x, _scrollOffset.y,
                _scrollViewport.x, _scrollViewport.y);
            var newScrollOffsetX = _scrollOffset.x;
            var newScrollOffsetY = _scrollOffset.y;
            if (rectangle.x < visibleRectangle.x)
            {
                if (rectangle.xMax > visibleRectangle.xMax)
                {
                    newScrollOffsetX = rectangle.x + rectangle.width / 2;
                }
                else
                {
                    newScrollOffsetX = rectangle.x;
                }
            }
            else if (rectangle.xMax > visibleRectangle.xMax)
            {
                newScrollOffsetX = rectangle.xMax - _scrollViewport.x;
            }
            if (rectangle.y < visibleRectangle.y)
            {
                if (rectangle.yMax > visibleRectangle.yMax)
                {
                    newScrollOffsetY = rectangle.y + rectangle.height / 2;
                }
                else
                {
                    newScrollOffsetY = rectangle.y;
                }
            }
            else if (rectangle.yMax > visibleRectangle.yMax)
            {
                newScrollOffsetY = rectangle.yMax - _scrollViewport.y;
            }
            newScrollOffsetX = ValidateVisualOffset(newScrollOffsetX);
            newScrollOffsetY = ValidateVisualOffset(newScrollOffsetY);
            var newScrollOffset = new Vector2(newScrollOffsetX, newScrollOffsetY);
            if (!_scrollOffset.IsClose(newScrollOffset))
            {
                SetScrollOffset(newScrollOffset);
                OnScrollChange();
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets the height of the document.
        /// </summary>
        internal float DocumentHeight => _heightTree?.TotalHeight ?? 0;

        /// <summary>
        /// Occurs when the TextView was measured and changed its visual lines.
        /// </summary>
        internal event EventHandler VisualLinesChanged;

        /// <summary>
        /// Gets the default baseline position. This is the difference between <see cref="VisualYPosition.TextTop"/>
        /// and <see cref="VisualYPosition.Baseline"/> for a line containing regular text.
        /// Lines that include formatted text or custom UI elements may have a different baseline.
        /// </summary>
        internal float DefaultBaseline
        {
            get
            {
                CalculateDefaultTextMetrics();
                return _defaultBaseline;
            }
        }

        /// <summary>
        /// Gets whether the visual lines are valid.
        /// Will return false after a call to Redraw().
        /// Accessing the visual lines property will cause a <see cref="VisualLinesInvalidException"/>
        /// if this property is <c>false</c>.
        /// </summary>
        internal bool VisualLinesValid => _visibleVisualLines != null;

        /// <summary>
        /// Occurs when the TextView is about to be measured and will regenerate its visual lines.
        /// This event may be used to mark visual lines as invalid that would otherwise be reused.
        /// </summary>
        internal event EventHandler<VisualLineConstructionStartEventArgs> VisualLineConstructionStarting;

        /// <summary>
        /// Occurs when the scroll offset has changed.
        /// </summary>
        internal event EventHandler ScrollOffsetChanged;

        /// <summary>
        /// If the visual lines are invalid, creates new visual lines for the visible part
        /// of the document.
        /// If all visual lines are valid, this method does nothing.
        /// </summary>
        /// <exception cref="InvalidOperationException">The visual line build process is already running.
        /// It is not allowed to call this method during the construction of a visual line.</exception>
        internal void EnsureVisualLines()
        {
            EditorDispatcher.VerifyMainThreadAccess();
            if (_inMeasure)
                throw new InvalidOperationException("The visual line build process is already running! Cannot EnsureVisualLines() during Measure!");
            if (!VisualLinesValid)
            {
                // force immediate re-measure
                UpdateVisualLines();
            }
            // Sometimes we still have invalid lines after UpdateLayout - work around the problem
            // by calling MeasureOverride directly.
            if (!VisualLinesValid)
            {
                Debug.Log("UpdateLayout() failed in EnsureVisualLines");
                Measure(_lastAvailableSize);
            }
            if (!VisualLinesValid)
                throw new VisualLinesInvalidException("Internal error: visual lines invalid after EnsureVisualLines call");
        }

        #region Visual element pointer handling

        internal void OnPointerPressed(Vector2 localPosition)
        {
            EnsureVisualLines();
            var element = GetVisualLineElementFromPosition(localPosition + _scrollOffset);
            element?.OnPointerPressed(localPosition);
        }

        internal void OnPointerMoved()
        {
            if (_lastAvailableSize.x > 0 && _lastAvailableSize.y > 0)
                Measure(_lastAvailableSize);
        }

        internal void OnPointerReleased(Vector2 localPosition)
        {
            EnsureVisualLines();
            var element = GetVisualLineElementFromPosition(localPosition + _scrollOffset);
            element?.OnPointerReleased(localPosition);
        }

        #endregion

        #region Getting elements from Visual Position
        /// <summary>
        /// Gets the visual line at the specified document position (relative to start of document).
        /// Returns null if there is no visual line for the position (e.g. the position is outside the visible
        /// text area).
        /// </summary>
        internal VisualLine GetVisualLineFromVisualTop(double visualTop)
        {
            // TODO: change this method to also work outside the visible range -
            // required to make GetPosition work as expected!
            EnsureVisualLines();
            foreach (var vl in VisualLines)
            {
                if (visualTop < vl.VisualTop)
                    continue;
                if (visualTop < vl.VisualTop + vl.Height)
                    return vl;
            }
            return null;
        }

        /// <summary>
        /// Gets the visual top position (relative to start of document) from a document line number.
        /// </summary>
        internal double GetVisualTopByDocumentLine(int line)
        {
            EditorDispatcher.VerifyMainThreadAccess();
            if (_heightTree == null)
                throw ThrowUtil.NoDocumentAssigned();
            return _heightTree.GetVisualPosition(_heightTree.GetLineByNumber(line));
        }

        private VisualLineElement GetVisualLineElementFromPosition(Vector2 visualPosition)
        {
            var vl = GetVisualLineFromVisualTop(visualPosition.y);
            if (vl != null)
            {
                var column = vl.GetVisualColumnFloor(visualPosition);

                foreach (var element in vl.Elements)
                {
                    if (element.VisualColumn + element.VisualLength <= column)
                        continue;
                    return element;
                }
            }
            return null;
        }
        #endregion

        /// <summary>
        /// Empty line selection width.
        /// </summary>
        internal virtual float EmptyLineSelectionWidth => 1;

        internal TextFormatter Formatter => _formatter;

        #region ScrollInfo implementation

        private void ClearScrollData()
        {
            SetScrollData(new Vector2(), new Vector2(), new Vector2());
        }

        private bool SetScrollData(Vector2 viewport, Vector2 extent, Vector2 offset)
        {
            if (!(viewport.IsClose(_scrollViewport)
                  && extent.IsClose(_scrollExtent)
                  && offset.IsClose(_scrollOffset)))
            {
                _scrollViewport = viewport;
                _scrollExtent = extent;
                SetScrollOffset(offset);
                OnScrollChange();
                return true;
            }

            return false;
        }

        private void SetScrollOffset(Vector2 vector)
        {
            if (!_canHorizontallyScroll)
            {
                vector = new Vector2(0, vector.y);
            }

            if (!_canVerticallyScroll)
            {
                vector = new Vector2(vector.x, 0);
            }

            if (!_scrollOffset.IsClose(vector))
            {
                _scrollOffset = vector;
                ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnScrollChange()
        {
            //_repaintAction();
        }

        internal DocumentLine GetDocumentLineByVisualTop(double visualTop)
        {
            EditorDispatcher.VerifyMainThreadAccess();
            if (_heightTree == null)
                throw ThrowUtil.NoDocumentAssigned();
            return _heightTree.GetLineByVisualPosition(visualTop);
        }

        #endregion
    }
}
