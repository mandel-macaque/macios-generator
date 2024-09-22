using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Xamarin.Macios.Generator.Tests;

/// <summary>
/// Base class that allows to test the generator.
/// </summary>
public class BaseTestClass
{
    protected BindingSourceGenerator _generator;
    protected CSharpGeneratorDriver _driver;
    protected PortableExecutableReference[] _references;
    // HACK: this is a hack to get the runtime dll for the attributes
    private const string RuntimeDll = "/Users/mandel/Xamarin/xamarin-macios/xamarin-macios/src/build/dotnet/ios/64/Microsoft.iOS.dll";

    public BaseTestClass()
    {
        _generator = new BindingSourceGenerator();
        _driver = CSharpGeneratorDriver.Create(_generator);

        var dotNetAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        _references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "mscorlib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Core.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Drawing.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Drawing.Primitives.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Runtime.InteropServices.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Private.CoreLib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(dotNetAssemblyPath, "System.Runtime.dll")),

            // needed for the attrs Export etc
            MetadataReference.CreateFromFile(RuntimeDll),
        ];
    }
}
