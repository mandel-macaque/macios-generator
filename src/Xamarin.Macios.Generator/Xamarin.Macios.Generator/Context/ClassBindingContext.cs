using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Context;

public class ClassBindingContext : SymbolBindingContext<ClassDeclarationSyntax> {
	public BindingTypeData? BindingType { get; set; }
	public string SymbolName { get; init; }
	public string RegisterName { get; init; }

	public bool IsStatic => Symbol.IsStatic;

	public ClassBindingContext (RootBindingContext context, SemanticModel semanticModel,
		INamedTypeSymbol symbol, ClassDeclarationSyntax declarationSyntax)
		: base (context, semanticModel, symbol, declarationSyntax)
	{
		SymbolName = symbol.Name;
		RegisterName =
			symbol.Name; //TODO: placeholder -> should this be extracted from the base type.Name attribute?
	}
}
