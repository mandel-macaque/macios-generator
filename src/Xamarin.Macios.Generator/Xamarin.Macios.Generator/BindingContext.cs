using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator;

public class BindingContext
{
    public BindingTypeData? BindingType { get; set; }
    public ClassDeclarationSyntax ClassDeclarationSyntax { get; }

    public SemanticModel SemanticModel { get; init; }
    public INamedTypeSymbol ClassSymbol { get; init; }
    public string Namespace => string.Empty;
    public string ClassName => string.Empty;
    public string RegisterName => string.Empty;

    public BindingContext(ClassDeclarationSyntax classDeclarationSyntax,
        SemanticModel semanticModel, INamedTypeSymbol classSymbol)
    {
        ClassDeclarationSyntax = classDeclarationSyntax;
        SemanticModel = semanticModel;
        ClassSymbol = classSymbol;
    }
}