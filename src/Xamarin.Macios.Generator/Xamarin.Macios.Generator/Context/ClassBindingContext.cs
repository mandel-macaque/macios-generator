using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Context;

public class ClassBindingContext {
	public BindingTypeData? BindingType { get; set; }
	public BindingContext BindingContext { get; init; }
	public ClassDeclarationSyntax ClassDeclarationSyntax { get; }

	public SemanticModel SemanticModel { get; init; }
	public INamedTypeSymbol ClassSymbol { get; init; }
	public string Namespace { get; init; }
	public string ClassName { get; init; }
	public string RegisterName { get; init; }

	public bool IsStatic => ClassSymbol.IsStatic;

	public ClassBindingContext (BindingContext context, ClassDeclarationSyntax classDeclarationSyntax,
		SemanticModel semanticModel, INamedTypeSymbol classSymbol)
	{
		BindingContext = context;
		ClassDeclarationSyntax = classDeclarationSyntax;
		SemanticModel = semanticModel;
		ClassSymbol = classSymbol;
		Namespace = classSymbol.ContainingNamespace.ToDisplayString ();
		ClassName = classSymbol.Name;
		RegisterName =
			classSymbol.Name; //TODO: placeholder -> should this be extracted from the base type.Name attribute?
	}
}
