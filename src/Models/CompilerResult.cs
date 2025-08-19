using System.Reflection;

namespace MauiFlow.Models
{
    public class CompilerResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public ContentView ContentView { get; set; }
        public Assembly Assembly { get; set; }
        public Dictionary<string, object> NamedElements { get; set; } = new();
    }
}
