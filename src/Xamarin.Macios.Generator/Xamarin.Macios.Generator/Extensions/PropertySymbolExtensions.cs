using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Xamarin.Macios.Generator.Extensions;

public static class PropertySymbolExtensions {
	public static string FormatType (this IPropertySymbol symbol)
	{
		// we cannot rely on the symbol.Name property since the type will be returning the underlying type
		// to work around this we are going to check the syntax of the symbol and return the correct type
		var location = symbol.Locations.FirstOrDefault ();
		var node = location.SourceTree?.GetRoot ()?.FindNode (location.SourceSpan) as PropertyDeclarationSyntax;
		var syntaxReturnType = node.Type.ToString ();
		var typeDisplayString = symbol.Type.ToDisplayString ();

		if (symbol.Type.SpecialType != SpecialType.None && syntaxReturnType == typeDisplayString)
			return syntaxReturnType;

		return symbol.Type.SpecialType switch {
			SpecialType.System_Int16 => symbol.Type.Name,
			SpecialType.System_UInt16 => symbol.Type.Name,
			SpecialType.System_Int32 => symbol.Type.Name,
			SpecialType.System_UInt32 => symbol.Type.Name,
			SpecialType.System_Int64 => symbol.Type.Name,
			SpecialType.System_UInt64 => symbol.Type.Name,
			SpecialType.System_IntPtr => symbol.Type.Name,
			SpecialType.System_UIntPtr => symbol.Type.Name,
			_ => typeDisplayString
		};
	}
}
