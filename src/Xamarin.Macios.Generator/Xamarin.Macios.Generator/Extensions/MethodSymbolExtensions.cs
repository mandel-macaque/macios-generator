using Microsoft.CodeAnalysis;
using Xamarin.Macios.Generator.Attributes;

namespace Xamarin.Macios.Generator.Extensions;

public static class MethodSymbolExtensions {
	public static bool TryParseAttributes (this IMethodSymbol self, GeneratorSyntaxContext context)
	{
		var boundAttributes = self.GetAttributes ();
		if (boundAttributes.Length == 0)
			return false;

		// build a dict that will map the AttributeData with the AttributeSyntax
		foreach (var attributeData in boundAttributes) {
			var attributeSyntax = attributeData.ApplicationSyntaxReference?.GetSyntax ();
			if (attributeSyntax is not null &&
			    context.SemanticModel.GetSymbolInfo (attributeSyntax).Symbol is IMethodSymbol attributeSymbol) {
				// based on the type use the correct parser to retrieve the data
				var attributeContainingTypeSymbol = attributeSymbol.ContainingType.ToDisplayString ();
				switch (attributeContainingTypeSymbol) {
				case "Foundation.ExportAttribute":
					if (ExportData.TryParse (attributeSyntax, attributeData, out var exportData)) {
						//BindingDeclarations.Add(interfaceDeclarationSyntax, baseTypeData);
					}

					;
					break;
				case "Foundation.FieldAttribute":
					if (FieldData.TryParse (attributeSyntax, attributeData, out var fieldData)) {
						//BindingDeclarations.Add(interfaceDeclarationSyntax, baseTypeData);
					}
					break;
				}
			}
		}

		return false;
	}
}
