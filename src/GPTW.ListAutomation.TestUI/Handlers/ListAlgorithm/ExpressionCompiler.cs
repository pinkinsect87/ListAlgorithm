using GPTW.ListAutomation.Core.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class ExpressionCompiler
{
    public bool HasError { get; set; }
    private object instance;
    private MethodInfo method;
    private List<string> parameters = new List<string>();

    public ExpressionCompiler(ILogger _logger, List<AlgorithmVariableItem> variableItems)
    {
        #region Parese and prepare expression

        var expression = "";

        foreach (var variableItem in variableItems)
        {
            this._parseParameters(variableItem.Expression);

            if (variableItem.ModuleIfCondition.IsMissing())
            {
                expression += $@"

                    return {variableItem.ModuleParameters};
                    ";
            }
            else
            {
                expression += $@"
                        
                    if ({variableItem.Expression}) 
                    {{
                        return {variableItem.ModuleParameters};
                    }}";
            }
        }

        parameters = parameters.Distinct().ToList();

        expression = expression
            .Replace("$", "")
            .Replace(" = ", " == ")
            .Replace(" and ", " && ")
            .Replace(" or ", " || ")
            .Replace(" AND ", " && ")
            .Replace(" OR ", " || ");

        #endregion

        #region Create expression instance 

        var className = "Expression";
        var methodName = "Compute";
        var parameter = parameters.Any() ? "double " + string.Join(", double ", parameters) : "";

        var source = @"
            using System;
            using System.Linq;

            namespace RoslynCompile
            {{
                public sealed class {0}
                {{
                    public object {1}({2})
                    {{
                        {3}
                    }}

                    private double min(double val1, double val2)
                    {{
                        return Math.Min(val1, val2);
                    }}

                    private double min(double val1, double val2, double val3)
                    {{
                        return min(min(val1, val2), val3);
                    }}

                    private double log(double val1, double val2)
                    {{
                        return Math.Log(val1, val2);
                    }}
                    
                    private double mean(double val1, double val2, double val3, double val4)
                    {{
                        var lst = new double[4] {{ val1, val2, val3, val4 }};
                        var mean = lst.Average();
                        return mean;
                    }}
                }}
            }}";

        var codeToCompile = string.Format(source, className, methodName, parameter, expression);

        var syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

        var assemblyName = Path.GetRandomFileName();
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "AlgorithmFiles", "ddls");

        IEnumerable<MetadataReference> DefaultReferences =
            new[]
            {
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.Uri.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll"))
            };

        CSharpCompilation compilation = CSharpCompilation.Create(
           assemblyName,
           syntaxTrees: new[] { syntaxTree },
           references: DefaultReferences,
           options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            EmitResult result = compilation.Emit(ms);

            if (!result.Success)
            {
                this.HasError = true;

                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    _logger.LogError("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
            }
            else
            {
                ms.Seek(0, SeekOrigin.Begin);

                var assembly = Assembly.Load(ms.ToArray());
                this.instance = assembly.CreateInstance($"RoslynCompile.{className}");
                this.method = instance.GetType().GetMethod(methodName);
            }
        }

        #endregion 
    }

    /// <summary>
    /// Compute
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public object? Compute(object[]? p)
    {
        return this.method.Invoke(this.instance, p);
    }

    /// <summary>
    /// GetParameters
    /// </summary>
    /// <returns></returns>
    public List<string> GetParameters()
    {
        return this.parameters;
    }

    /// <summary>
    /// _parseParameters
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    private void _parseParameters(string expression)
    {
        var parameter = new List<char>();
        var hasParameter = false;

        foreach (var c in expression.ToCharArray())
        {
            if (c == '$')
            {
                hasParameter = true;
                parameter = new List<char>();
            }
            else if (hasParameter)
            {
                if (Char.IsLetter(c) || Char.IsDigit(c) || c == '_')
                {
                    parameter.Add(c);
                }
                else
                {
                    hasParameter = false;

                    if (parameter.Any())
                    {
                        this.parameters.Add(string.Join("", parameter));
                    }
                }
            }
        }

        if (hasParameter && parameter.Any())
        {
            this.parameters.Add(string.Join("", parameter));
        }
    }
}
