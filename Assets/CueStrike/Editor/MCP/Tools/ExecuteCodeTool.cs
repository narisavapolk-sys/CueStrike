using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using UnityEditor;
using UnityEngine;

namespace CueStrike.MCP.Tools
{
    /// <summary>
    /// Tool for executing C# code in the Unity Editor context.
    /// This is the most powerful tool - allows full access to Unity Editor API.
    /// </summary>
    public class ExecuteCodeTool : IMcpTool
    {
        public string Name => "execute_code";
        public string Description => "Execute C# code in the Unity Editor context. Has full access to UnityEditor, UnityEngine, and project assemblies.";

        public JToken InputSchema => CreateSchema();

        private static JToken CreateSchema()
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    code = new { type = "string", description = "C# code to execute. Can contain multiple statements." },
                    usings = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "Additional using statements to include (e.g., ['UnityEditor', 'System.Linq'])"
                    },
                    references = new
                    {
                        type = "array",
                        items = new { type = "string" },
                        description = "Additional assembly references (e.g., ['UnityEditor.CoreModule'])"
                    }
                },
                required = new[] { "code" }
            };
            return JToken.FromObject(schema);
        }

        public McpProtocol.ToolCallResult Execute(JToken arguments)
        {
            try
            {
                string code = arguments["code"]?.Value<string>();
                var usings = new List<string>
                {
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text",
                    "UnityEngine",
                    "UnityEditor",
                    "UnityEditor.SceneManagement"
                };

                if (arguments["usings"] is JArray usingsProp)
                {
                    foreach (var u in usingsProp)
                    {
                        if (u.Type == JTokenType.String)
                            usings.Add(u.Value<string>());
                    }
                }

                var references = new List<string>();
                if (arguments["references"] is JArray refsProp)
                {
                    foreach (var r in refsProp)
                    {
                        if (r.Type == JTokenType.String)
                            references.Add(r.Value<string>());
                    }
                }

                var result = ExecuteCode(code, usings, references);
                return McpProtocol.CreateJsonResult(result);
            }
            catch (Exception ex)
            {
                return McpProtocol.CreateTextResult($"Execution error: {ex.Message}\n{ex.StackTrace}", true);
            }
        }

        private object ExecuteCode(string code, List<string> usings, List<string> extraReferences)
        {
            var logs = new List<string>();
            Application.LogCallback logHandler = (condition, stackTrace, type) =>
            {
                logs.Add($"[{type}] {condition}");
            };
            Application.logMessageReceived += logHandler;

            try
            {
                // Wrap code in a class with Execute method
                var wrappedCode = WrapCode(code, usings);

                // Compile
                var assembly = CompileAssembly(wrappedCode, extraReferences);
                if (assembly == null)
                {
                    return new { success = false, error = "Compilation failed", logs };
                }

                // Execute
                var type = assembly.GetType("McpScript.Script");
                var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    return new { success = false, error = "Execute method not found", logs };
                }

                var result = method.Invoke(null, null);
                return new { success = true, result, logs };
            }
            catch (Exception ex)
            {
                return new { success = false, error = ex.Message, stackTrace = ex.StackTrace, logs };
            }
            finally
            {
                Application.logMessageReceived -= logHandler;
            }
        }

        private string WrapCode(string code, List<string> usings)
        {
            var sb = new StringBuilder();
            foreach (var u in usings)
            {
                sb.AppendLine($"using {u};");
            }
            sb.AppendLine();
            sb.AppendLine("namespace McpScript {");
            sb.AppendLine("    public static class Script {");
            sb.AppendLine("        public static object Execute() {");
            sb.AppendLine(code.Indent(12));
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private Assembly CompileAssembly(string code, List<string> extraReferences)
        {
            using (var provider = new CSharpCodeProvider())
            {
                var parameters = new CompilerParameters
                {
                    GenerateInMemory = true,
                    GenerateExecutable = false
                };

                // Add standard references
                parameters.ReferencedAssemblies.Add(typeof(object).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(UnityEngine.Object).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(UnityEditor.Editor).Assembly.Location);
                parameters.ReferencedAssemblies.Add(typeof(UnityEditor.SceneManagement.EditorSceneManager).Assembly.Location);
                parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

                // Add Unity Editor assemblies
                var editorAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName.Contains("UnityEditor") || a.FullName.Contains("UnityEngine"))
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location));

                foreach (var asm in editorAssemblies)
                {
                    try { parameters.ReferencedAssemblies.Add(asm.Location); } catch { }
                }

                // Add extra references
                foreach (var refName in extraReferences)
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == refName || a.FullName.StartsWith(refName));
                    if (asm != null && !string.IsNullOrEmpty(asm.Location))
                    {
                        try { parameters.ReferencedAssemblies.Add(asm.Location); } catch { }
                    }
                }

                var result = provider.CompileAssemblyFromSource(parameters, code);
                if (result.Errors.HasErrors)
                {
                    foreach (CompilerError err in result.Errors)
                    {
                        UnityEngine.Debug.LogError($"[MCP Compile] {err.ErrorText}");
                    }
                    return null;
                }

                return result.CompiledAssembly;
            }
        }
    }

    internal static class StringExtensions
    {
        public static string Indent(this string str, int spaces)
        {
            var indent = new string(' ', spaces);
            return string.Join("\n", str.Split('\n').Select(line => indent + line));
        }
    }
}
