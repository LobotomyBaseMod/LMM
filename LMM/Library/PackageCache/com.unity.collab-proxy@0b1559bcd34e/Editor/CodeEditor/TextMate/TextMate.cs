using System;
using System.Linq;

using TextMateSharp.Grammars;
using TextMateSharp.Model;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Unity.CodeEditor.TextMate
{
    internal static class TextMate
    {
        internal static void RegisterExceptionHandler(Action<Exception> handler)
        {
            _exceptionHandler = handler;
        }

        internal static Installation InstallTextMate(
            this TextEditor editor,
            IRegistryOptions registryOptions,
            bool initCurrentDocument = true)
        {
            return new Installation(editor, registryOptions, initCurrentDocument);
        }

        internal class Installation
        {
            private IRegistryOptions _textMateRegistryOptions;
            private Registry _textMateRegistry;
            private TextEditor _editor;
            private TextEditorModel _editorModel;
            private IGrammar _grammar;
            private TMModel _tmModel;
            private TextMateColoringTransformer _transformer;

            internal IRegistryOptions RegistryOptions { get { return _textMateRegistryOptions; } }
            internal TextEditorModel EditorModel { get { return _editorModel; } }

            internal Installation(TextEditor editor, IRegistryOptions registryOptions, bool initCurrentDocument = true)
            {
                _textMateRegistryOptions = registryOptions;
                _textMateRegistry = new Registry(registryOptions);

                _editor = editor;

                SetTheme(registryOptions.GetDefaultTheme());

                editor.DocumentChanged += OnEditorOnDocumentChanged;

                if (initCurrentDocument)
                {
                    OnEditorOnDocumentChanged(editor, EventArgs.Empty);
                }
            }

            internal void SetGrammar(string scopeName)
            {
                _grammar = _textMateRegistry.LoadGrammar(scopeName);

                GetOrCreateTransformer().SetGrammar(_grammar);

                _editor.TextArea.TextView.Redraw();
            }

            internal void SetTheme(IRawTheme theme)
            {
                _textMateRegistry.SetTheme(theme);

                GetOrCreateTransformer().SetTheme(_textMateRegistry.GetTheme());

                _tmModel?.InvalidateLine(0);

                _editorModel?.InvalidateViewPortLines();
            }

            internal void Dispose()
            {
                _editor.DocumentChanged -= OnEditorOnDocumentChanged;

                DisposeEditorModel(_editorModel);
                DisposeTMModel(_tmModel, _transformer);
                DisposeTransformer(_transformer);
            }

            void OnEditorOnDocumentChanged(object sender, EventArgs args)
            {
                try
                {
                    DisposeEditorModel(_editorModel);
                    DisposeTMModel(_tmModel, _transformer);

                    _editorModel = new TextEditorModel(_editor.TextArea.TextView, _editor.Document, _exceptionHandler);
                    _tmModel = new TMModel(_editorModel);
                    _tmModel.SetGrammar(_grammar);
                    _transformer = GetOrCreateTransformer();
                    _transformer.SetModel(_editor.Document, _tmModel);
                    _tmModel.AddModelTokensChangedListener(_transformer);
                }
                catch (Exception ex)
                {
                    _exceptionHandler?.Invoke(ex);
                }
            }

            TextMateColoringTransformer GetOrCreateTransformer()
            {
                var transformer = _editor.TextArea.TextView.LineTransformers.OfType<TextMateColoringTransformer>().FirstOrDefault();

                if (transformer is null)
                {
                    transformer = new TextMateColoringTransformer(
                        _editor.TextArea.TextView, _exceptionHandler);

                    _editor.TextArea.TextView.LineTransformers.Add(transformer);
                }

                return transformer;
            }

            static void DisposeTransformer(TextMateColoringTransformer transformer)
            {
                if (transformer == null)
                    return;

                transformer.Dispose();
            }

            static void DisposeTMModel(TMModel tmModel, TextMateColoringTransformer transformer)
            {
                if (tmModel == null)
                    return;

                if (transformer != null)
                    tmModel.RemoveModelTokensChangedListener(transformer);

                tmModel.Dispose();
            }

            static void DisposeEditorModel(TextEditorModel editorModel)
            {
                if (editorModel == null)
                    return;

                editorModel.Dispose();
            }
        }

        static Action<Exception> _exceptionHandler;
    }
}
