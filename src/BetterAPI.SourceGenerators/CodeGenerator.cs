using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BetterAPI.SourceGenerators
{
    [Generator]
    public class CodeGenerator : ISourceGenerator
    {
        private StringBuilder _log;

        public void Initialize(GeneratorInitializationContext context)
        {
            _log = new StringBuilder();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            _log.AppendLine("Starting code generation");

            var sw = Stopwatch.StartNew();

            try
            {
                GenerateStringTable(context);
            }
            catch (Exception e)
            {
                _log.AppendLine(e.ToString());
            }

            _log.AppendLine($"Completed code generation, took {sw.Elapsed}.");

            GenerateFile(context, "RunLog.cs", sb =>
            {
                sb.AppendLine("internal static class RunLog");
                sb.AppendLine("{");
                sb.AppendLine("    /*");
                sb.AppendLine();
                sb.AppendLine(_log.ToString());
                sb.AppendLine();
                sb.AppendLine("    */");
                sb.AppendLine("}");
            });
        }

        private static void GenerateStringTable(GeneratorExecutionContext context)
        {
            GenerateFile(context, "Strings.cs", sb =>
            {
                sb.AppendLine("namespace BetterAPI.Logging");
                sb.AppendLine("{");
                sb.AppendLine("    public static class Strings");
                sb.AppendLine("    {");

                var messages = GetLocalizedStrings(context).OrderBy(x => x).ToList();

                sb.AppendLine($"        public const int Count = {messages.Count};");
                sb.AppendLine();

                foreach (var message in messages)
                {
                    sb.AppendLine($"        public const string {ConvertMessageToMethodName(message)} = {message};");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                GetLocalizedStrings(context);
            });
        }

        private static string ConvertMessageToMethodName(string message)
        {
            //
            // Normalize the log message into a valid method name
            //
            
            var sb = new StringBuilder();
            bool capitalize = true; // capitalize first character
            foreach (var c in message)
            {
                if (c == '"' || c == '\'' || c == '(' || c == ')' || c == '[' || c == ']' || c == ',' || c == '/' ||
                    c == '\\' || c == '=')
                    continue; // ignore insignificant punctuation

                if (c == '.')
                    break; // discard additional sentences

                if (c == ' ' || c == '-' || c == '|')
                {
                    capitalize = true;
                    continue; // capitalize the next character
                }

                if (capitalize)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    capitalize = false;
                }
                else
                {
                    sb.Append(c);
                }
            }

            var methodName = sb.ToString();

            // Special cases:
            //
            methodName = methodName
                    .Replace("Took{Elapsed}", string.Empty)
                    .Replace("HTTP", "Http")
                    .Replace("SQLite", "Sqlite")
                    .Replace("ID", "Id")
                ;

            // Mask out structured tokens
            //
            methodName = Regex.Replace(methodName, "{\\w*}", string.Empty, RegexOptions.Compiled);
            return methodName;
        }

        private static ISet<string> GetLocalizedStrings(GeneratorExecutionContext context)
        {
            var messages = new HashSet<string>();

            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var model = context.Compilation.GetSemanticModel(syntaxTree);
                var nodes = syntaxTree.GetRoot().DescendantNodes().ToList();

                for (var i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];

                    if (node is InvocationExpressionSyntax invocation)
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax)
                        {
                            var symbol = model.GetSymbolInfo(invocation);

                            var ns = symbol.Symbol?.ContainingNamespace.ToString();
                            var methodName = symbol.Symbol?.Name;

                            if (ns != null &&
                                ns.Equals("Microsoft.Extensions.Localization") &&
                                methodName.Equals("GetString", StringComparison.OrdinalIgnoreCase))
                            {
                                var syntaxNode = nodes[i + 6];
                                if (syntaxNode is LiteralExpressionSyntax literal)
                                {
                                    var message = literal.ToFullString();
                                    messages.Add(message);
                                }
                            }
                        }
                    }
                }
            }

            return messages;
        }

        public static void GenerateFile(GeneratorExecutionContext context, string fileName, Action<IStringBuilder> generateAction)
        {
            var sb = new IndentAwareStringBuilder();
            generateAction(sb);
            sb.InsertAutoGeneratedHeader();
            var source = sb.ToString();
            var sourceText = SourceText.From(source, Encoding.UTF8);
            context.AddSource(fileName, sourceText);
        }
    }
}

