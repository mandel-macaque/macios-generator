using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Xamarin.Macios.Generator.Parsers;

public static class FieldParser
{

    public static bool TryParse(SyntaxNode attributeSyntax, AttributeData attributeData,
        [NotNullWhen(true)] out FieldData? data)
    {
        data = default;

        var count = attributeData.ConstructorArguments.Length;
        switch (count)
        {
            case 1:
                data = new() { SymbolName = (string)attributeData.ConstructorArguments[0].Value! };
                return true;
            case 2:
                data = new()
                {
                    SymbolName = (string)attributeData.ConstructorArguments[0].Value!,
                    LibraryName = (string)attributeData.ConstructorArguments[1].Value!,
                };
                return true;
            default:
                // 0 should not be an option..
                return false;
        }
    }

}
