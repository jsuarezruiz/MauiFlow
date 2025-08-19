using System.Text.RegularExpressions;

namespace MauiFlow.Helpers
{
    internal class FileHelper
    {
        public static Dictionary<string, string> ExtractFiles(string content)
        {
            var fileContents = new Dictionary<string, string>();

            // Common file extensions for .NET projects
            var extensions = new[] { "xaml", "cs", "txt", "json", "xml", "config", "csproj" };

            // Pattern to match file headers and capture content (updated to match ### and clean file names)
            var headerPattern = @"###\s+([^\s/\\:]+?\.(?:" + string.Join("|", extensions) + @"))\s*\n(.*?)(?=\n###|\n---|\n####|$)";

            var matches = Regex.Matches(content, headerPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success && match.Groups[2].Success)
                {
                    var fileName = match.Groups[1].Value.Trim();
                    var fileContent = match.Groups[2].Value.Trim();

                    // Extract content from code blocks if present
                    fileContent = ExtractCodeContent(fileContent);

                    if (IsValidFileName(fileName) && !string.IsNullOrWhiteSpace(fileContent))
                    {
                        fileContents[fileName] = fileContent;
                    }
                }
            }

            // Fallback: try to extract from numbered lists with content
            if (fileContents.Count == 0)
            {
                ExtractFromProjectStructure(content, fileContents, extensions);
            }

            // If only a single code block exists, extract it as MainPage.cs
            if (fileContents.Count == 0)
            {
                var singleCodeContent = ExtractSingleCodeAsMainPage(content);
                if (!string.IsNullOrWhiteSpace(singleCodeContent))
                {
                    fileContents["MainPage.cs"] = singleCodeContent;
                }
            }

            return fileContents;
        }

        static string ExtractSingleCodeAsMainPage(string content)
        {
            // Try to extract code from markdown code block first
            var codeContent = ExtractCodeContent(content);

            // If no code block found, check if the entire content looks like C# code
            if (codeContent == content.Trim())
            {
                // Check if content appears to be C# code
                if (IsCSharpCode(content))
                {
                    return content.Trim();
                }
            }
            else
            {
                // Code was extracted from markdown, validate it's C# code
                if (IsCSharpCode(codeContent))
                {
                    return codeContent;
                }
            }

            return null;
        }

        static bool IsCSharpCode(string content)
        {
            var trimmed = content.Trim();

            // Check for common C# patterns
            var csharpPatterns = new[]
            {
                @"using\s+[\w\.]+\s*;",                    // using statements
                @"namespace\s+[\w\.]+",                    // namespace declarations
                @"public\s+(partial\s+)?class\s+\w+",      // class declarations
                @"public\s+(static\s+)?void\s+\w+\s*\(",   // method declarations
                @"private\s+[\w<>\[\]]+\s+\w+\s*[;=]",     // field declarations
                @"{\s*get\s*;\s*set\s*;\s*}",              // auto properties
                @"if\s*\([^)]+\)\s*{",                     // if statements
                @"for\s*\([^)]*\)\s*{",                    // for loops
                @"while\s*\([^)]+\)\s*{",                  // while loops
                @"new\s+\w+\s*\(",                         // object instantiation
                @"=>\s*"                                   // lambda expressions
            };

            // Content should match at least one C# pattern
            bool hasCodePattern = csharpPatterns.Any(pattern =>
                Regex.IsMatch(trimmed, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline));

            // Additional checks: should not be XAML and should have some code structure
            bool isNotXaml = !trimmed.TrimStart().StartsWith("<");
            bool hasCodeStructure = trimmed.Contains("{") && trimmed.Contains("}");

            return hasCodePattern && isNotXaml && hasCodeStructure;
        }

        static string ExtractCodeContent(string content)
        {
            // Remove markdown code block markers
            var codeBlockPattern = @"```(?:xml|csharp|cs|xaml)?\s*\n?(.*?)\n?```";
            var match = Regex.Match(content, codeBlockPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (match.Success && match.Groups[1].Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // If no code block found, return original content cleaned up
            return content.Trim();
        }

        static void ExtractFromProjectStructure(string content, Dictionary<string, string> fileContents, string[] extensions)
        {
            // Split content into sections by headers
            var sections = Regex.Split(content, @"\n(?=###)", RegexOptions.Multiline);

            foreach (var section in sections)
            {
                // Pattern 1: Look for file references in bold followed by content
                var boldFilePattern = @"\*\*([^\*]+?\.(?:" + string.Join("|", extensions) + @"))\*\*[^\n]*\n(.*?)(?=\n\*\*|\n###|\n---|\n####|$)";
                var boldMatches = Regex.Matches(section, boldFilePattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match match in boldMatches)
                {
                    if (match.Groups[1].Success && match.Groups[2].Success)
                    {
                        var fileName = match.Groups[1].Value.Trim();
                        var fileContent = match.Groups[2].Value.Trim();

                        fileContent = ExtractCodeContent(fileContent);

                        if (IsValidFileName(fileName) && !string.IsNullOrWhiteSpace(fileContent))
                        {
                            fileContents[fileName] = fileContent;
                        }
                    }
                }

                // Pattern 2: Look for plain file names followed by content
                var plainFilePattern = @"([^\s/\\:]+?\.(?:" + string.Join("|", extensions) + @"))[^\n]*\n(.*?)(?=\n[^\s/\\:]+?\.(?:" + string.Join("|", extensions) + @")|\n###|\n---|\n####|$)";
                var plainMatches = Regex.Matches(section, plainFilePattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

                foreach (Match match in plainMatches)
                {
                    if (match.Groups[1].Success && match.Groups[2].Success)
                    {
                        var fileName = match.Groups[1].Value.Trim();
                        var fileContent = match.Groups[2].Value.Trim();

                        fileContent = ExtractCodeContent(fileContent);

                        if (IsValidFileName(fileName) && !string.IsNullOrWhiteSpace(fileContent))
                        {
                            fileContents[fileName] = fileContent;
                        }
                    }
                }
            }
        }

        static bool IsValidFileName(string fileName)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
                return false;

            // Must contain at least one dot for extension
            if (!fileName.Contains('.'))
                return false;

            // Should not contain invalid characters
            var invalidChars = new char[] { '<', '>', '|', '"', '?', '*' };
            if (fileName.Any(c => invalidChars.Contains(c)))
                return false;

            // Should have a reasonable structure
            var parts = fileName.Split('.');
            return parts.Length >= 2 && parts[0].Length > 0 && parts.Last().Length > 0;
        }
    }
}