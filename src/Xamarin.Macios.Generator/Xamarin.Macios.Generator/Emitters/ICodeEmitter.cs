using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.Macios.Generator.Emitters;
public interface ICodeEmitter<T> where T : BaseTypeDeclarationSyntax {
    public string SymbolName {get;}
    bool TryValidate ([NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics);
    void Emit ();
}
