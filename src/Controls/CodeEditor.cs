namespace MauiFlow.Controls
{
    public enum EditorTheme
    {
        Dark,
        Light,
        HighContrast
    }

    public class CodeEditor : WebView
    {
        private string _text = string.Empty;
        private string _language = "csharp";
        private EditorTheme _theme = EditorTheme.Dark;
        private int _fontSize = 14;

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(
                nameof(Text),
                typeof(string),
                typeof(CodeEditor),
                string.Empty,
                BindingMode.TwoWay,
                propertyChanged: OnTextChanged);

        public static readonly BindableProperty LanguageProperty =
            BindableProperty.Create(
                nameof(Language),
                typeof(string),
                typeof(CodeEditor),
                "csharp",
                propertyChanged: OnLanguageChanged);

        public static readonly BindableProperty ThemeProperty =
            BindableProperty.Create(
                nameof(Theme),
                typeof(EditorTheme),
                typeof(CodeEditor),
                EditorTheme.Dark,
                propertyChanged: OnThemeChanged);

        public static readonly BindableProperty FontSizeProperty =
            BindableProperty.Create(
                nameof(FontSize),
                typeof(int),
                typeof(CodeEditor),
                14,
                propertyChanged: OnFontSizeChanged);

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? string.Empty;
                    UpdateEditorContent();
                }
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value ?? "csharp";
                    UpdateEditorContent();
                }
            }
        }

        public EditorTheme Theme
        {
            get => _theme;
            set
            {
                if (_theme != value)
                {
                    _theme = value;
                    UpdateEditorContent();
                }
            }
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    UpdateEditorContent();
                }
            }
        }

        public CodeEditor()
        {
            Source = new HtmlWebViewSource
            {
                Html = GenerateHtml(_text, _language, _theme, _fontSize)
            };
        }

        static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var editor = (CodeEditor)bindable;
            editor.Text = (string)newValue ?? string.Empty;
        }

        static void OnLanguageChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var editor = (CodeEditor)bindable;
            editor.Language = (string)newValue ?? "csharp";
        }

        static void OnThemeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var editor = (CodeEditor)bindable;
            editor.Theme = (EditorTheme)newValue;
        }

        static void OnFontSizeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var editor = (CodeEditor)bindable;
            editor.FontSize = (int)newValue;
        }

        void UpdateEditorContent()
        {
            if (Source is HtmlWebViewSource htmlSource)
            {
                htmlSource.Html = GenerateHtml(_text, _language, _theme, _fontSize);
                Source = htmlSource; // refresh WebView
            }
        }

        string GenerateHtml(string code, string language, EditorTheme theme, int fontSize)
        {
            string themeStr = theme switch
            {
                EditorTheme.Light => "vs-light",
                EditorTheme.HighContrast => "hc-black",
                _ => "vs-dark"
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <style>
    html, body {{
      margin: 0;
      padding: 0;
      height: 100%;
      overflow: hidden;
    }}
    #container {{
      width: 100%;
      height: 100%;
    }}
  </style>
  <script src=""https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs/loader.js""></script>
  <script>
    require.config({{ paths: {{ 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' }} }});
    require(['vs/editor/editor.main'], function() {{
      var editor = monaco.editor.create(document.getElementById('container'), {{
        value: {EscapeJsString(code)},
        language: '{language}',
        theme: '{themeStr}',
        fontSize: {fontSize},
        automaticLayout: true
      }});
    }});
  </script>
</head>
<body>
  <div id=""container""></div>
</body>
</html>";
        }

        string EscapeJsString(string value)
        {
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "") + "\"";
        }
    }
}
