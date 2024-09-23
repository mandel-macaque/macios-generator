using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Context;

namespace Xamarin.Macios.Generator.Emitters;

public static class EmitterFactory {

    public static ICodeEmitter<T> Create<T> (SymbolBindingContext context, TabbedStringBuilder builder) where T : BaseTypeDeclarationSyntax
    {
        var declarationType = typeof (T);
        return declarationType switch {
            Type t when t == typeof (ClassDeclarationSyntax) => (ICodeEmitter<T>) new ClassEmitter (context as ClassBindingContext, builder),
            Type t when t == typeof (EnumDeclarationSyntax) => (ICodeEmitter<T>) new EnumEmitter (context as EnumBindingContext, builder),
            _ => throw new NotImplementedException ($"Unknown declaration type {declarationType}")
        };
        
    }
}