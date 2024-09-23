using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.Macios.Generator.Context;

public class SymbolBindingContext {

	public RootBindingContext RootBindingContext { get; init; }
	public SemanticModel SemanticModel { get; init; }
	public INamedTypeSymbol Symbol { get; init; }

	public SymbolBindingContext(RootBindingContext rootBindingContext,
		SemanticModel semanticModel, INamedTypeSymbol symbol)
	{
		RootBindingContext = rootBindingContext;
		SemanticModel = semanticModel;
		Symbol = symbol;
	}

}

public class SymbolBindingContext<T> : SymbolBindingContext where T : BaseTypeDeclarationSyntax {

	public T DeclarationSyntax { get; }
	public string Namespace => Symbol.ContainingNamespace.ToDisplayString();

	public SymbolBindingContext(RootBindingContext rootBindingContext,
		SemanticModel semanticModel, INamedTypeSymbol symbol, T declarationSyntax)
		: base(rootBindingContext, semanticModel, symbol)
	{
		DeclarationSyntax = declarationSyntax;
	}
}

