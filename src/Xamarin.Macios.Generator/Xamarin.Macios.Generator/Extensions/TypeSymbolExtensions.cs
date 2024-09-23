using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Xamarin.Macios.Generator;

public static class TypeSymbolExtensions {
	public static bool IsSmartEnum (this ITypeSymbol symbol)
	{
		return false;
	}

	public static Dictionary<string, AttributeData> GetAttributeData (this ISymbol symbol)
	{
		var boundAttributes = symbol.GetAttributes ();
		if (boundAttributes.Length == 0) {
			// return an empty dictionary if there are no attributes
			return new();
		}

		var attributes = new Dictionary<string, AttributeData> ();
		foreach (var attributeData in boundAttributes) {
			var attrName = attributeData.AttributeClass?.ToDisplayString ();
			if (string.IsNullOrEmpty (attrName))
				continue;
			if (!attributes.TryAdd (attrName, attributeData)) {
				// TODO: diagnostics
			}
		}

		return attributes;
	}
}
