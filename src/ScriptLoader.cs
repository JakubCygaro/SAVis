using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAVis.API;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using Raylib_CsLo.InternalHelpers;


namespace SAVis;


[Serializable]
public class LoadingException : Exception
{
    public LoadingException() { }
    public LoadingException(string message) : base(message) { }
    public LoadingException(string message, Exception inner) : base(message, inner) { }
}

internal static class ScriptLoader
{
    static readonly MetadataReference[] _references = new[] {
                typeof(System.Object).GetTypeInfo().Assembly.Location,
                typeof(IEnumerator<bool>).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location)!, "System.Runtime.dll"),
                typeof(ISorter).GetTypeInfo().Assembly.Location
            }
            .Select(r => MetadataReference.CreateFromFile(r)).ToArray();
    public static ISorter[] LoadFromFile(string path)
    {
        var source = File.ReadAllText(path, Encoding.UTF8);

        var ast = CSharpSyntaxTree.ParseText(source);
        string assmName = Path.GetRandomFileName();
        CSharpCompilation compilation = CSharpCompilation.Create(
               assmName,
               syntaxTrees: new[] { ast },
               references: _references,
               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();

        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            //Console.WriteLine("ERROR: \tFailed to compile");
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            List<LoadingException> ls = [];
            foreach (Diagnostic diagnostic in failures)
            {
                ls.Add(new LoadingException($"{diagnostic.Location.ToString()} {diagnostic.Id}: {diagnostic.GetMessage()}"));
            }
            throw new AggregateException($"There were compilation errors for script `{path}`", ls);
        }
        ms.Seek(0, SeekOrigin.Begin);

        Assembly assm = AssemblyLoadContext.Default.LoadFromStream(ms);

        var sorters = assm.GetTypes()
            .Where(static t => 
                t.IsClass && 
                t.IsPublic && 
                t.GetInterface(typeof(SAVis.API.ISorter).Name!) is not null &&
                !t.IsGenericType &&
                !t.IsSpecialName &&
                t.GetCustomAttribute<CompilerGeneratedAttribute>() is null
                )
            .Select(sorterType => 
                {
                    return assm.CreateInstance(sorterType.Name) as ISorter
                        ?? throw new LoadingException($"Could not instantiate the sorter class `{sorterType.Name}` from `{path}`");
                } 
            )
            .Where(static v => v is not null)
            .Select(static s => s)
            .ToArray();

        if(sorters.Length <= 0)
            throw new LoadingException($"Could not find the proper sorter class in `{path}`");

        return sorters;
    }
}
