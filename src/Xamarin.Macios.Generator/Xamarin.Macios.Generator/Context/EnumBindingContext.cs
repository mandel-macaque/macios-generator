using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.Macios.Generator.Context;

public class EnumBindingContext : SymbolBindingContext<EnumDeclarationSyntax> {

	public string SymbolName => Symbol.Name;
	public EnumBindingContext (RootBindingContext context,
		SemanticModel semanticModel, INamedTypeSymbol enumSymbol, EnumDeclarationSyntax enumDeclarationSyntax) : base (
		context, semanticModel, enumSymbol, enumDeclarationSyntax)
	{
	}
}
