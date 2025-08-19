using MauiFlow.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MauiFlow.Helpers
{
    /// <summary>
    /// Provides helper methods for dynamically compiling XAML with code-behind or 
    /// only C# code for .NET MAUI applications.
    /// </summary>
    public static class CompilerHelper
    {
        static readonly Dictionary<string, object> _namedElements = new();

        public static async Task<CompilerResult> CompileAsync(string xamlContent, string codeBehindContent = null)
        {
            var result = new CompilerResult();

            try
            {
                // Determine compilation mode
                var mode = DetermineCompilationMode(xamlContent, codeBehindContent);

                switch (mode)
                {
                    case CompilationMode.XamlWithCodeBehind:
                        return await CompileXamlWithCodeBehindAsync(xamlContent, codeBehindContent);

                    case CompilationMode.CSharp:
                        return await CompileCSharpAsync(xamlContent); // xamlContent actually contains C# code

                    case CompilationMode.Invalid:
                    default:
                        result.ErrorMessage = "Invalid input: Either provide XAML with code-behind or only C# code";
                        return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error during compilation: {ex.Message}");
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
                return result;
            }
        }

        enum CompilationMode
        {
            Invalid,
            XamlWithCodeBehind,
            CSharp
        }

        static CompilationMode DetermineCompilationMode(string xamlContent, string codeBehindContent)
        {
            bool hasXamlContent = !string.IsNullOrWhiteSpace(xamlContent) &&
                                 (xamlContent.TrimStart().StartsWith("<") || xamlContent.Contains("ContentView") || xamlContent.Contains("ContentPage"));

            bool hasCodeBehind = !string.IsNullOrWhiteSpace(codeBehindContent);

            bool isCSharp = !string.IsNullOrWhiteSpace(xamlContent) &&
                               !xamlContent.TrimStart().StartsWith("<") &&
                               (xamlContent.Contains("class") || xamlContent.Contains("using") || xamlContent.Contains("namespace"));

            if (hasXamlContent && hasCodeBehind)
                return CompilationMode.XamlWithCodeBehind;

            if (isCSharp && string.IsNullOrWhiteSpace(codeBehindContent))
                return CompilationMode.CSharp;

            return CompilationMode.Invalid;
        }

        static async Task<CompilerResult> CompileXamlWithCodeBehindAsync(string xamlContent, string codeBehindContent)
        {
            var result = new CompilerResult();
            string compilationErrorMessage = null;

            if (string.IsNullOrWhiteSpace(xamlContent))
            {
                result.ErrorMessage = "XAML content cannot be null or empty";
                return result;
            }

            if (string.IsNullOrWhiteSpace(codeBehindContent))
            {
                result.ErrorMessage = "Code-behind content cannot be null or empty";
                return result;
            }

            try
            {
                // Replace ContentPage with ContentView and fix resources tag
                xamlContent = ReplaceContentPageWithContentView(xamlContent);

                // Force ContentView as base type
                string baseType = "ContentView";
                var namedElements = ExtractNamedElements(xamlContent);

                // Log input code for debugging
                System.Diagnostics.Debug.WriteLine("Input Code-Behind:\n" + codeBehindContent);
                codeBehindContent = AdjustCodeBehindContent(codeBehindContent, namedElements, xamlContent, baseType);

                // Log the modified code-behind for debugging
                System.Diagnostics.Debug.WriteLine("Modified Code-Behind:\n" + codeBehindContent);

                // Validate code structure
                if (!IsValidClassStructure(codeBehindContent))
                {
                    compilationErrorMessage = "Code-behind contains top-level statements or invalid structure";
                    throw new InvalidOperationException(compilationErrorMessage);
                }

                var compilationResult = await CompileCodeBehindAsync(codeBehindContent);
                if (!compilationResult.Success)
                {
                    compilationErrorMessage = $"Compilation failed: {compilationResult.ErrorMessage}";
                    throw new InvalidOperationException(compilationErrorMessage);
                }

                var visualElementResult = await CreateAndLoadVisualElementAsync(compilationResult.Assembly, xamlContent, namedElements, baseType);
                if (!visualElementResult.Success)
                {
                    compilationErrorMessage = visualElementResult.ErrorMessage;
                    throw new InvalidOperationException(compilationErrorMessage);
                }

                result.Success = true;
                result.ContentView = visualElementResult.VisualElement as ContentView;
                result.Assembly = compilationResult.Assembly;
                result.NamedElements = new Dictionary<string, object>(_namedElements);

                return result;
            }
            catch (Exception ex)
            {
                // Store the compilation error for logging
                var fullCompilationError = compilationErrorMessage ?? ex.Message;
                System.Diagnostics.Debug.WriteLine($"Code-behind compilation failed: {fullCompilationError}");
                System.Diagnostics.Debug.WriteLine($"Attempting XAML-only fallback...");

                // Attempt fallback to XAML-only rendering
                try
                {
                    var fallbackResult = await RenderXamlOnlyAsync(xamlContent);

                    // Even if fallback succeeds, we need to indicate the original compilation failed
                    if (fallbackResult.Success)
                    {
                        fallbackResult.ErrorMessage = $"Code-behind compilation failed: {fullCompilationError}. " +
                                                     "Rendered XAML-only (no interactivity). " +
                                                     "Check your code-behind syntax and dependencies.";

                        // Log the fallback success
                        System.Diagnostics.Debug.WriteLine("XAML-only fallback succeeded");
                    }

                    return fallbackResult;
                }
                catch (Exception fallbackEx)
                {
                    // Both compilation and fallback failed
                    System.Diagnostics.Debug.WriteLine($"XAML-only fallback also failed: {fallbackEx.Message}");

                    result.ErrorMessage = $"Code-behind compilation failed: {fullCompilationError}. " +
                                         $"XAML-only fallback also failed: {fallbackEx.Message}";
                    return result;
                }
            }
        }

        static async Task<CompilerResult> CompileCSharpAsync(string csharpCode)
        {
            var result = new CompilerResult();

            try
            {
                System.Diagnostics.Debug.WriteLine("Input only C# Code:\n" + csharpCode);

                // Process the only C# code
                csharpCode = ProcessCSharpCode(csharpCode);

                System.Diagnostics.Debug.WriteLine("Processed only C# Code:\n" + csharpCode);

                // Validate code structure
                if (!IsValidClassStructure(csharpCode))
                {
                    result.ErrorMessage = "C# code contains top-level statements or invalid structure";
                    return result;
                }

                var compilationResult = await CompileCodeBehindAsync(csharpCode);
                if (!compilationResult.Success)
                {
                    result.ErrorMessage = $"Compilation failed: {compilationResult.ErrorMessage}";
                    return result;
                }

                var visualElementResult = await CreateCSharpVisualElementAsync(compilationResult.Assembly);
                if (!visualElementResult.Success)
                {
                    result.ErrorMessage = visualElementResult.ErrorMessage;
                    return result;
                }

                result.Success = true;
                result.ContentView = visualElementResult.VisualElement as ContentView;
                result.Assembly = compilationResult.Assembly;
                result.NamedElements = new Dictionary<string, object>(); // No named elements in only C#

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Only C# compilation error: {ex.Message}";
                return result;
            }
        }

        static string ProcessCSharpCode(string csharpCode)
        {
            // Check if we need to wrap in namespace and class
            bool hasNamespace = Regex.IsMatch(csharpCode, @"namespace\s+[\w\.]+", RegexOptions.IgnoreCase);
            bool hasClass = Regex.IsMatch(csharpCode, @"public\s+(partial\s+)?class\s+\w+\s*:\s*ContentView", RegexOptions.IgnoreCase);

            if (!hasNamespace || !hasClass)
            {
                // Extract methods and fields from the code
                var methodsAndFields = ExtractMethodsAndFields(csharpCode);

                // Create the complete class structure
                csharpCode = GenerateCSharpClassStructure(methodsAndFields);
            }
            else
            {
                // Adjust existing class structure for C#
                csharpCode = AdjustExistingCSharpClass(csharpCode);
            }

            // Remove unsupported code
            csharpCode = SanitizeCodeBehind(csharpCode);

            return csharpCode;
        }

        static string GenerateCSharpClassStructure(string methodsAndFields)
        {
            var usingStatements = GetConsolidatedUsingStatements();

            return $@"{usingStatements}

namespace DynamicNamespace
{{
    public partial class DynamicContentView : ContentView
    {{
        public DynamicContentView()
        {{
            InitializeComponent();
        }}

        private void InitializeComponent()
        {{
            BuildContent();
        }}

        private void BuildContent()
        {{
            // Content will be built programmatically
            // This method should be overridden by user code
        }}

{methodsAndFields}
    }}
}}";
        }

        private static string AdjustExistingCSharpClass(string csharpCode)
        {
            // Remove existing using statements to avoid duplicates
            csharpCode = RemoveExistingUsingStatements(csharpCode);

            // Fix constructor name to match our expected class name
            var constructorRegex = new Regex(@"public\s+(\w+)\s*\(\s*\)\s*\{");
            csharpCode = constructorRegex.Replace(csharpCode, "public DynamicContentView()\n        {");

            // Replace class name and ensure it inherits from ContentView
            var classRegex = new Regex(@"public\s+(partial\s+)?class\s+(\w+)(\s*:\s*ContentView)?");
            csharpCode = classRegex.Replace(csharpCode, "public partial class DynamicContentView : ContentView");

            // Fix namespace
            var namespaceRegex = new Regex(@"namespace\s+[\w\.]+");
            csharpCode = namespaceRegex.Replace(csharpCode, "namespace DynamicNamespace");

            // Ensure constructor calls InitializeComponent and BuildContent
            csharpCode = EnsureCSharpConstructor(csharpCode);

            // Add consolidated using statements at the beginning
            var usingStatements = GetConsolidatedUsingStatements();
            csharpCode = usingStatements + "\n\n" + csharpCode;

            return csharpCode;
        }

        private static string EnsureCSharpConstructor(string csharpCode)
        {
            var constructorMatch = Regex.Match(csharpCode, @"public\s+DynamicContentView\s*\(\s*\)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}", RegexOptions.Singleline);

            if (constructorMatch.Success)
            {
                var constructorBody = constructorMatch.Groups[1].Value;

                // Check if InitializeComponent is already called
                if (!constructorBody.Contains("InitializeComponent"))
                {
                    var newConstructorBody = "InitializeComponent();" + constructorBody;
                    var newConstructor = $@"public DynamicContentView()
        {{
            {newConstructorBody.Trim()}
        }}";

                    csharpCode = csharpCode.Replace(constructorMatch.Value, newConstructor);
                }
            }
            else
            {
                // Add constructor if not found
                var classStartRegex = new Regex(@"(public\s+partial\s+class\s+DynamicContentView\s*:\s*ContentView\s*\{)");
                csharpCode = classStartRegex.Replace(csharpCode, @"$1
        public DynamicContentView()
        {
            InitializeComponent();
        }");
            }

            // Ensure InitializeComponent method exists
            if (!Regex.IsMatch(csharpCode, @"private\s+void\s+InitializeComponent\s*\(", RegexOptions.IgnoreCase))
            {
                var methodInsertRegex = new Regex(@"(\s*public\s+DynamicContentView\s*\(\s*\)\s*\{[^}]*\})");
                csharpCode = methodInsertRegex.Replace(csharpCode, @"$1

        private void InitializeComponent()
        {
            BuildContent();
        }");
            }

            return csharpCode;
        }

        static async Task<(bool Success, string ErrorMessage, VisualElement VisualElement)> CreateCSharpVisualElementAsync(Assembly assembly)
        {
            try
            {
                var visualElementType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(ContentView).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic);

                if (visualElementType == null)
                {
                    return (false, "No ContentView type found in compiled code", null);
                }

                VisualElement? visualElementInstance;

                try
                {
                    visualElementInstance = Activator.CreateInstance(visualElementType) as VisualElement;

                    if (visualElementInstance == null)
                    {
                        return (false, "Failed to create ContentView instance - Activator.CreateInstance returned null", null);
                    }
                }
                catch (MissingMethodException ex)
                {
                    return (false, $"Constructor error: No parameterless constructor found. {ex.Message}", null);
                }
                catch (TargetInvocationException ex)
                {
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    return (false, $"Constructor execution failed: {innerException}", null);
                }
                catch (Exception ex)
                {
                    return (false, $"Instance creation failed: {ex.GetType().Name} - {ex.Message}", null);
                }

                // For C#, no XAML loading is needed, the content should be built in the constructor

                return (true, null, visualElementInstance);
            }
            catch (Exception ex)
            {
                var fullError = $"C# ContentView creation error: {ex.GetType().Name} - {ex.Message}";
                if (ex.InnerException != null)
                {
                    fullError += $"\nInner Exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}";
                }

                System.Diagnostics.Debug.WriteLine($"Full error details: {fullError}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return (false, fullError, null);
            }
        }

        // Keep all existing methods for XAML processing
        static string ReplaceContentPageWithContentView(string xamlContent)
        {
            // Validate XAML structure
            if (!Regex.IsMatch(xamlContent, @"<Content(Page|View)\b[^>]*>", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException("XAML must contain a ContentPage or ContentView root element");
            }

            // Replace <ContentPage> with <ContentView> if it exists
            if (Regex.IsMatch(xamlContent, @"<ContentPage\b", RegexOptions.IgnoreCase))
            {
                xamlContent = Regex.Replace(xamlContent, @"<ContentPage\b([^>]*)>", "<ContentView$1>", RegexOptions.IgnoreCase);
                xamlContent = xamlContent.Replace("</ContentPage>", "</ContentView>");
            }

            // Replace <ContentPage.Resources> with <ContentView.Resources>
            xamlContent = xamlContent.Replace("<ContentPage.Resources>", "<ContentView.Resources>")
                                    .Replace("</ContentPage.Resources>", "</ContentView.Resources>");

            // Remove Title attribute as ContentView doesn't support it
            xamlContent = Regex.Replace(xamlContent, @"\s+Title\s*=\s*""[^""]*""", "", RegexOptions.IgnoreCase);

            // Replace or add x:Class="DynamicNamespace.DynamicContentView"
            var classRegex = new Regex(@"x:Class\s*=\s*""[^""]+""", RegexOptions.IgnoreCase);
            if (classRegex.IsMatch(xamlContent))
            {
                xamlContent = classRegex.Replace(xamlContent, @"x:Class=""DynamicNamespace.DynamicContentView""");
            }
            else
            {
                xamlContent = Regex.Replace(xamlContent, @"<ContentView\b", @"<ContentView x:Class=""DynamicNamespace.DynamicContentView""");
            }

            return xamlContent;
        }

        static List<string> ExtractNamedElements(string xaml)
        {
            var namedElements = new List<string>();
            var regex = new Regex(@"x:Name\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
            var matches = regex.Matches(xaml);

            foreach (Match match in matches)
            {
                if (!namedElements.Contains(match.Groups[1].Value))
                    namedElements.Add(match.Groups[1].Value);
            }

            return namedElements;
        }

        static bool IsValidClassStructure(string code)
        {
            var trimmed = code.TrimStart();
            return trimmed.StartsWith("namespace") || trimmed.StartsWith("using") || trimmed.StartsWith("public partial class");
        }

        static string AdjustCodeBehindContent(string codeBehind, List<string> namedElements, string xamlContent, string baseType)
        {
            // Check if we need to wrap in namespace and class
            bool needsWrapping = !Regex.IsMatch(codeBehind, @"namespace\s+[\w\.]+", RegexOptions.IgnoreCase) ||
                               !Regex.IsMatch(codeBehind, @"public\s+partial\s+class", RegexOptions.IgnoreCase);

            if (needsWrapping)
            {
                // Extract methods and fields from the code
                var methodsAndFields = ExtractMethodsAndFields(codeBehind);

                // Generate field declarations for named elements
                var fieldDeclarations = GenerateFieldDeclarations(namedElements, xamlContent);

                // Create the complete class structure with consolidated using statements
                codeBehind = GenerateCompleteClassStructure(fieldDeclarations, methodsAndFields, baseType);
            }
            else
            {
                // Adjust existing class structure
                codeBehind = AdjustExistingClassStructure(codeBehind, namedElements, xamlContent, baseType);
            }

            // Remove unsupported code
            codeBehind = SanitizeCodeBehind(codeBehind);

            return codeBehind;
        }

        static string GenerateCompleteClassStructure(string fieldDeclarations, string methodsAndFields, string baseType)
        {
            var usingStatements = GetConsolidatedUsingStatements();

            return $@"{usingStatements}

namespace DynamicNamespace
{{
    public partial class DynamicContentView : {baseType}
    {{
{fieldDeclarations}
        public DynamicContentView()
        {{
            InitializeComponent();
        }}

        public void InitializeComponent()
        {{
            // XAML loading is handled externally
        }}

{methodsAndFields}
    }}
}}";
        }

        static string AdjustExistingClassStructure(string codeBehind, List<string> namedElements, string xamlContent, string baseType)
        {
            // Remove existing using statements to avoid duplicates
            codeBehind = RemoveExistingUsingStatements(codeBehind);

            // Fix constructor name
            var constructorRegex = new Regex(@"public\s+(\w+)\s*\(\s*\)\s*\{");
            codeBehind = constructorRegex.Replace(codeBehind, "public DynamicContentView()\n        {");

            // Replace base type
            var baseTypeRegex = new Regex(@"(public\s+partial\s+class\s+)(\w+)(\s*:\s*)(\w+)");
            codeBehind = baseTypeRegex.Replace(codeBehind, $"$1DynamicContentView$3{baseType}");

            // Fix namespace
            var namespaceRegex = new Regex(@"namespace\s+[\w\.]+");
            codeBehind = namespaceRegex.Replace(codeBehind, "namespace DynamicNamespace");

            // Add missing field declarations
            var fieldDeclarations = GenerateFieldDeclarations(namedElements, xamlContent);
            if (!string.IsNullOrEmpty(fieldDeclarations))
            {
                var classStartRegex = new Regex(@"(public\s+partial\s+class\s+DynamicContentView\s*:\s*\w+\s*\{)");
                codeBehind = classStartRegex.Replace(codeBehind, $"$1\n{fieldDeclarations}");
            }

            // Ensure InitializeComponent exists and handle constructor safety
            codeBehind = EnsureInitializeComponentMethodAndSafeConstructor(codeBehind, namedElements);

            // Add consolidated using statements at the beginning
            var usingStatements = GetConsolidatedUsingStatements();
            codeBehind = usingStatements + "\n\n" + codeBehind;

            return codeBehind;
        }

        static string GetConsolidatedUsingStatements()
        {
            return @"using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.IO;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls.Shapes;";
        }

        static string RemoveExistingUsingStatements(string code)
        {
            // Remove all existing using statements
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var filteredLines = lines.Where(line => !line.Trim().StartsWith("using ") || line.Trim().StartsWith("using (")).ToList();
            return string.Join(Environment.NewLine, filteredLines);
        }

        static string EnsureInitializeComponentMethodAndSafeConstructor(string codeBehind, List<string> namedElements)
        {
            // Check if InitializeComponent already exists
            var initMethodExists = Regex.IsMatch(codeBehind, @"public\s+void\s+InitializeComponent\s*\([^)]*\)", RegexOptions.IgnoreCase);

            if (!initMethodExists)
            {
                // Add InitializeComponent method before the last closing braces
                var lastBraceRegex = new Regex(@"(\s*)\}\s*\}\s*$");
                codeBehind = lastBraceRegex.Replace(codeBehind, @"
        public void InitializeComponent()
        {
            // XAML loading is handled externally
        }

        private void PostInitializeComponent()
        {
            // UI-dependent initialization code will be moved here
        }
$1}
}");
            }

            // Handle constructor safely by separating UI-dependent and UI-independent code
            codeBehind = RefactorConstructorForSafety(codeBehind, namedElements);

            return codeBehind;
        }

        static string RefactorConstructorForSafety(string codeBehind, List<string> namedElements)
        {
            var constructorMatch = Regex.Match(codeBehind, @"public\s+DynamicContentView\s*\(\s*\)\s*\{([^}]*(?:\{[^}]*\}[^}]*)*)\}", RegexOptions.Singleline);

            if (constructorMatch.Success)
            {
                var constructorBody = constructorMatch.Groups[1].Value;

                // Separate safe and unsafe code
                var (safeCode, unsafeCode) = SeparateConstructorCode(constructorBody, namedElements);

                // Create new constructor with only safe code
                var newConstructor = $@"public DynamicContentView()
        {{
            InitializeComponent();{(string.IsNullOrWhiteSpace(safeCode) ? "" : $"\n{safeCode}")}
        }}";

                // Replace the old constructor
                codeBehind = codeBehind.Replace(constructorMatch.Value, newConstructor);

                // If there's unsafe code, add it to PostInitializeComponent method
                if (!string.IsNullOrWhiteSpace(unsafeCode))
                {
                    var postInitRegex = new Regex(@"(private\s+void\s+PostInitializeComponent\s*\(\s*\)\s*\{\s*)(// UI-dependent initialization code will be moved here)(\s*\})", RegexOptions.Singleline);

                    if (postInitRegex.IsMatch(codeBehind))
                    {
                        codeBehind = postInitRegex.Replace(codeBehind, $"$1// UI-dependent initialization code moved from constructor\n{unsafeCode}$3");
                    }
                }
            }
            else
            {
                // If no constructor found, ensure we have a safe one
                if (!Regex.IsMatch(codeBehind, @"public\s+DynamicContentView\s*\(\s*\)", RegexOptions.IgnoreCase))
                {
                    var classStartRegex = new Regex(@"(public\s+partial\s+class\s+DynamicContentView\s*:\s*\w+\s*\{)");
                    codeBehind = classStartRegex.Replace(codeBehind, @"$1
        public DynamicContentView()
        {
            InitializeComponent();
        }");
                }
            }

            return codeBehind;
        }

        static (string safeCode, string unsafeCode) SeparateConstructorCode(string constructorBody, List<string> namedElements)
        {
            var lines = constructorBody.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var safeLines = new List<string>();
            var unsafeLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Skip empty lines and InitializeComponent calls
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.Contains("InitializeComponent"))
                    continue;

                if (IsCodeSafeForConstructor(trimmedLine, namedElements))
                {
                    safeLines.Add(line);
                }
                else
                {
                    unsafeLines.Add(line);
                }
            }

            return (string.Join(Environment.NewLine, safeLines), string.Join(Environment.NewLine, unsafeLines));
        }

        static bool IsCodeSafeForConstructor(string codeLine, List<string> namedElements)
        {
            var trimmedLine = codeLine.Trim();

            // Empty lines and comments are safe
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//") || trimmedLine.StartsWith("/*"))
                return true;

            // Variable declarations and assignments to non-UI fields are generally safe
            if (Regex.IsMatch(trimmedLine, @"^\s*(var|int|string|bool|double|float|decimal|DateTime|List<|Dictionary<|ObservableCollection<)"))
                return true;

            // Check if the line references any named UI elements
            foreach (var elementName in namedElements)
            {
                if (Regex.IsMatch(trimmedLine, $@"\b{Regex.Escape(elementName)}\b", RegexOptions.IgnoreCase))
                    return false;
            }

            // Check for common UI-related method calls that should be deferred
            var unsafePatterns = new[]
            {
                @"\.Content\s*=",
                @"\.Text\s*=",
                @"\.IsVisible\s*=",
                @"\.BackgroundColor\s*=",
                @"\.Source\s*=",
                @"\.ItemsSource\s*=",
                @"\.Children\.",
                @"\.Add\s*\(",
                @"\.Remove\s*\(",
                @"\.Clear\s*\(",
                @"FindByName",
                @"LoadFromXaml",
                @"SetBinding",
                @"BindingContext\s*="
            };

            foreach (var pattern in unsafePatterns)
            {
                if (Regex.IsMatch(trimmedLine, pattern, RegexOptions.IgnoreCase))
                    return false;
            }

            // Method calls that might access UI elements are potentially unsafe
            // unless they're clearly safe operations
            if (trimmedLine.Contains("(") && trimmedLine.Contains(")"))
            {
                // Safe method patterns
                var safeMethodPatterns = new[]
                {
                    @"Math\.",
                    @"Console\.",
                    @"Debug\.",
                    @"String\.",
                    @"DateTime\.",
                    @"Convert\.",
                    @"new\s+\w+\s*\(",
                    @"\.ToString\s*\(",
                    @"\.Parse\s*\(",
                    @"\.TryParse\s*\(",
                    @"\.Equals\s*\(",
                    @"\.GetHashCode\s*\(",
                    @"\.CompareTo\s*\("
                };

                bool isSafeMethod = safeMethodPatterns.Any(pattern =>
                    Regex.IsMatch(trimmedLine, pattern, RegexOptions.IgnoreCase));

                if (!isSafeMethod)
                    return false;
            }

            return true;
        }

        static string SanitizeCodeBehind(string code)
        {
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Remove any line containing DisplayAlert or DisplayPrompt (case-insensitive)
            var filteredLines = lines.Where(line =>
                !Regex.IsMatch(line, @"\bDisplay(Alert|Prompt)(Async)?\b", RegexOptions.IgnoreCase)
            ).ToArray();

            var sanitizedCode = string.Join(Environment.NewLine, filteredLines);

            // Log the sanitized code for debugging
            System.Diagnostics.Debug.WriteLine("Sanitized Code:\n" + sanitizedCode);

            return sanitizedCode;
        }

        static string ExtractMethodsAndFields(string code)
        {
            var result = new StringBuilder();

            // Extract existing methods (excluding constructors and InitializeComponent)
            var methodRegex = new Regex(@"(?:public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:void|[\w<>\[\]]+)\s+(\w+)\s*\([^)]*\)\s*\{[^}]*(?:\{[^}]*\}[^}]*)*\}",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            var matches = methodRegex.Matches(code);
            foreach (Match match in matches)
            {
                var methodName = match.Groups[1].Value;
                if (methodName != "InitializeComponent" && methodName != "BuildContent" && !IsConstructorName(methodName))
                {
                    result.AppendLine("        " + match.Value.Trim());
                    result.AppendLine();
                }
            }

            return result.ToString();
        }

        static bool IsConstructorName(string methodName)
        {
            // Check if it's likely a constructor (starts with uppercase and matches common constructor patterns)
            return char.IsUpper(methodName[0]) && (methodName.EndsWith("ContentView") || methodName.Contains("Dynamic"));
        }

        static string GenerateFieldDeclarations(List<string> namedElements, string xamlContent)
        {
            var result = new StringBuilder();

            foreach (var elementName in namedElements)
            {
                var elementType = GetElementTypeFromXaml(xamlContent, elementName);
                result.AppendLine($"        private {elementType} {elementName};");
            }

            return result.ToString();
        }

        static string GetElementTypeFromXaml(string xaml, string elementName)
        {
            var pattern = $@"<([a-zA-Z][\w\.]*)\s+[^>]*x:Name\s*=\s*""{Regex.Escape(elementName)}""[^>]*>";
            var match = Regex.Match(xaml, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (match.Success)
            {
                var elementType = match.Groups[1].Value;
                var typeName = elementType.Contains(":") ? elementType.Split(':').Last() : elementType;
                return MapXamlTypeToClrType(typeName);
            }

            System.Diagnostics.Debug.WriteLine($"Warning: Could not determine type for x:Name='{elementName}', defaulting to View");
            return "View";
        }

        static string MapXamlTypeToClrType(string xamlTypeName)
        {
            return xamlTypeName switch
            {
                // Basic controls
                "Label" => "Label",
                "Button" => "Button",
                "Entry" => "Entry",
                "Editor" => "Editor",
                "Image" => "Image",

                // Layout controls
                "StackLayout" => "StackLayout",
                "VerticalStackLayout" => "VerticalStackLayout",
                "HorizontalStackLayout" => "HorizontalStackLayout",
                "Grid" => "Grid",
                "AbsoluteLayout" => "AbsoluteLayout",
                "FlexLayout" => "FlexLayout",

                // Container controls
                "ContentView" => "ContentView",
                "ContentPage" => "ContentView",
                "ScrollView" => "ScrollView",
                "Frame" => "Frame",
                "Border" => "Border",

                // Input controls
                "Slider" => "Slider",
                "Switch" => "Switch",
                "Picker" => "Picker",
                "DatePicker" => "DatePicker",
                "TimePicker" => "TimePicker",
                "CheckBox" => "CheckBox",
                "RadioButton" => "RadioButton",
                "Stepper" => "Stepper",
                "SearchBar" => "SearchBar",

                // Display controls
                "ProgressBar" => "ProgressBar",
                "ActivityIndicator" => "ActivityIndicator",
                "WebView" => "WebView",

                // Collection controls
                "CollectionView" => "CollectionView",
                "ListView" => "ListView",
                "TableView" => "TableView",
                "CarouselView" => "CarouselView",

                // Shape controls (these are in Microsoft.Maui.Controls.Shapes namespace)
                "Rectangle" => "Microsoft.Maui.Controls.Shapes.Rectangle",
                "Ellipse" => "Microsoft.Maui.Controls.Shapes.Ellipse",
                "Line" => "Microsoft.Maui.Controls.Shapes.Line",
                "Path" => "Microsoft.Maui.Controls.Shapes.Path",
                "Polygon" => "Microsoft.Maui.Controls.Shapes.Polygon",
                "Polyline" => "Microsoft.Maui.Controls.Shapes.Polyline",

                // Fallback to View for unknown types
                _ => "View"
            };
        }

        static async Task<(bool Success, string ErrorMessage, Assembly Assembly)> CompileCodeBehindAsync(string code)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var references = GetMetadataReferences();

                var compilation = CSharpCompilation.Create(
                    $"DynamicAssembly_{Guid.NewGuid():N}",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    return (true, null, assembly);
                }

                var errors = string.Join("\n", result.Diagnostics
                    .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                    .Select(d => $"{d.Id}: {d.GetMessage()} at {d.Location}"));

                return (false, errors, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        static List<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();

            try
            {
                // Get currently loaded assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Distinct();

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    }
                    catch
                    {
                        // Skip assemblies that can't be referenced
                    }
                }

                // Add essential .NET and MAUI assemblies
                var essentialAssemblies = new[]
                {
                    typeof(object).Assembly.Location, // System.Runtime
                    typeof(Console).Assembly.Location, // System.Console
                    typeof(IEnumerable<>).Assembly.Location, // System.Collections
                    typeof(ObservableCollection<>).Assembly.Location,
                    typeof(INotifyPropertyChanged).Assembly.Location, // System.ComponentModel
                    typeof(Thickness).Assembly.Location, // Microsoft.Maui
                    typeof(ContentView).Assembly.Location, // Microsoft.Maui.Controls
                    typeof(Color).Assembly.Location, // Microsoft.Maui.Graphics
                    typeof(Microsoft.Maui.Controls.Xaml.Diagnostics.BindingDiagnostics).Assembly.Location, // XAML
                    typeof(System.Timers.Timer).Assembly.Location, // System.Timers
                    typeof(System.Timers.ElapsedEventArgs).Assembly.Location, // System.Timers
                    typeof(HttpClient).Assembly.Location,                    // System.Net.Http
                    typeof(System.Text.Json.JsonSerializer).Assembly.Location, // System.Text.Json
                }.Where(loc => !string.IsNullOrEmpty(loc)).Distinct();

                foreach (var location in essentialAssemblies)
                {
                    if (!references.Any(r => string.Equals(r.Display, location, StringComparison.OrdinalIgnoreCase)))
                    {
                        try
                        {
                            references.Add(MetadataReference.CreateFromFile(location));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to add reference {location}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting references: {ex.Message}");
            }

            return references;
        }

        static async Task<(bool Success, string ErrorMessage, VisualElement VisualElement)> CreateAndLoadVisualElementAsync(
                 Assembly assembly, string xamlContent, List<string> namedElements, string baseType)
        {
            try
            {
                var visualElementType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(ContentView).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic);

                if (visualElementType == null)
                {
                    return (false, "No ContentView type found in compiled code", null);
                }

                VisualElement? visualElementInstance;

                try
                {
                    visualElementInstance = Activator.CreateInstance(visualElementType) as VisualElement;

                    if (visualElementInstance == null)
                    {
                        return (false, "Failed to create ContentView instance - Activator.CreateInstance returned null", null);
                    }
                }
                catch (MissingMethodException ex)
                {
                    return (false, $"Constructor error: No parameterless constructor found. {ex.Message}", null);
                }
                catch (TargetInvocationException ex)
                {
                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    return (false, $"Constructor execution failed: {innerException}", null);
                }
                catch (Exception ex)
                {
                    return (false, $"Instance creation failed: {ex.GetType().Name} - {ex.Message}", null);
                }

                // Load XAML
                try
                {
                    visualElementInstance.LoadFromXaml(xamlContent);
                }
                catch (Exception ex)
                {
                    return (false, $"XAML loading error: {ex.Message}\nInner: {ex.InnerException?.Message}", null);
                }

                // Wire up named elements
                try
                {
                    WireNamedElements(visualElementInstance, namedElements, visualElementType);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Error wiring named elements: {ex.Message}");
                    // Don't fail the entire process for wiring errors
                }

                // Call PostInitializeComponent if it exists to handle UI-dependent initialization
                try
                {
                    var postInitMethod = visualElementType.GetMethod("PostInitializeComponent", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (postInitMethod != null)
                    {
                        postInitMethod.Invoke(visualElementInstance, null);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Error calling PostInitializeComponent: {ex.Message}");
                    // Don't fail for post-init errors
                }

                return (true, null, visualElementInstance);
            }
            catch (Exception ex)
            {
                var fullError = $"ContentView creation error: {ex.GetType().Name} - {ex.Message}";
                if (ex.InnerException != null)
                {
                    fullError += $"\nInner Exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}";
                }

                System.Diagnostics.Debug.WriteLine($"Full error details: {fullError}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return (false, fullError, null);
            }
        }

        static void WireNamedElements(VisualElement visualElementInstance, List<string> namedElements, Type visualElementType)
        {
            try
            {
                _namedElements.Clear();

                var findByNameMethod = typeof(NameScopeExtensions).GetMethod("FindByName", BindingFlags.Public | BindingFlags.Static);

                foreach (var elementName in namedElements)
                {
                    var field = visualElementType.GetField(elementName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field not found for {elementName} in {visualElementType.Name}");
                        continue;
                    }

                    try
                    {
                        var genericMethod = findByNameMethod.MakeGenericMethod(field.FieldType);
                        object element = genericMethod.Invoke(null, new object[] { visualElementInstance, elementName });

                        // Try searching in Content if not found at root level
                        if (element == null && visualElementInstance is ContentView contentView && contentView.Content != null)
                        {
                            element = genericMethod.Invoke(null, new object[] { contentView.Content, elementName });
                        }

                        if (element != null)
                        {
                            field.SetValue(visualElementInstance, element);
                            _namedElements[elementName] = element;
                            System.Diagnostics.Debug.WriteLine($"Wired element: {elementName} = {element.GetType().Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Element not found in XAML: {elementName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error wiring element {elementName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error wiring named elements: {ex.Message}");
            }
        }

        static async Task<CompilerResult> RenderXamlOnlyAsync(string xamlContent)
        {
            var result = new CompilerResult();

            try
            {
                var contentView = new ContentView();

                // Remove event handlers to avoid XamlParseException
                var cleanXaml = StripEventHandlersFromXaml(xamlContent.Trim());

                // Try to load only the XAML
                contentView.LoadFromXaml(cleanXaml);

                result.Success = true;
                result.ContentView = contentView;
                result.Assembly = null;
                result.NamedElements = new Dictionary<string, object>();

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"XAML-only render failed: {ex.Message}";
                return result;
            }
        }

        static string StripEventHandlersFromXaml(string xamlContent)
        {
            // List of common event attributes to remove
            var eventAttributes = new[]
            {
                "Clicked", "Pressed", "Released",           // Button
                "TextChanged", "Completed", "Unfocused", "Focused", // Entry, Editor
                "Tapped", "DoubleTapped", "Holding",       // Gesture
                "CurrentItemChanged", "SelectionChanged",  // CollectionView
                "DateSelected", "TimeSelected",            // DatePicker, TimePicker
                "CheckedChanged",                          // CheckBox
                "ValueChanged",                            // Slider, DatePicker
                "ScrollChanged",                           // ScrollView
                "ItemTapped",                              // ListView
                "PositionChanged",                         // CarouselView
                "Navigated", "Navigating", "ReloadRequested", // WebView
                "SizeChanged",                             // Any element
                "BindingContextChanged"                    // Any element
            };

            string result = xamlContent;

            foreach (var eventAttr in eventAttributes)
            {
                // Match: Clicked="OnClicked" or Tapped  = "Handle_Tapped"
                var regex = new Regex(
                    $@"\s+{eventAttr}\s*=\s*""[^""]*""",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

                result = regex.Replace(result, "");
            }

            return result;
        }
    }
}