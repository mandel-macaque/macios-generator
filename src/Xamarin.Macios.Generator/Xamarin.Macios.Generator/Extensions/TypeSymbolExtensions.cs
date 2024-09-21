using Microsoft.CodeAnalysis;

namespace Xamarin.Macios.Generator;

public static class TypeSymbolExtensions
{
    public static bool IsSmartEnum(this ITypeSymbol symbol)
    {
        return false;
    }
}