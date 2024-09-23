using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.Macios.Generator.Context;

public static class ContextFactory
{
    public static SymbolBindingContext<T> Create<T> (RootBindingContext context, SemanticModel semanticModel,
		INamedTypeSymbol symbol, BaseTypeDeclarationSyntax declarationSyntax) where T : BaseTypeDeclarationSyntax 
    {
        var declarationType = typeof (T);
        var bindingContext = declarationType switch {
            Type t when t == typeof (ClassDeclarationSyntax) => new ClassBindingContext (context, semanticModel, symbol, (declarationSyntax as ClassDeclarationSyntax)!) as SymbolBindingContext<T>,
            Type t when t == typeof (EnumDeclarationSyntax) => new EnumBindingContext (context, semanticModel, symbol, (declarationSyntax as EnumDeclarationSyntax)!) as SymbolBindingContext<T>,
            _ => throw new NotImplementedException ($"Unknown declaration type {declarationType}")
        };
        return bindingContext!;
    }
}
