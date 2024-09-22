using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Extensions;

public static class ClassDeclarationSyntaxExtensions {
	public static bool TryParseAttributes (this ClassDeclarationSyntax self, ClassBindingContext classBindingContext,
		[NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		diagnostics = null;

		var declaredSymbol = classBindingContext.SemanticModel.GetDeclaredSymbol (self);
		if (declaredSymbol is null) {
			// TODO: diagnostics
			return false;
		}

		var boundAttributes = declaredSymbol.GetAttributes ();
		if (boundAttributes.Length == 0) {
			// TODO: diagnostics
			return false;
		}

		// build a dict that will map the AttributeData with the AttributeSyntax
		foreach (var attributeData in boundAttributes) {
			var attributeSyntax = attributeData.ApplicationSyntaxReference?.GetSyntax ();
			if (attributeSyntax is not null &&
			    classBindingContext.SemanticModel.GetSymbolInfo (attributeSyntax).Symbol is IMethodSymbol
				    attributeSymbol) {
				// based on the type use the correct parser to retrieve the data
				var attributeContainingTypeSymbol = attributeSymbol.ContainingType.ToDisplayString ();
				switch (attributeContainingTypeSymbol) {
				case "BaseTypeAttribute":
					if (BindingTypeParser.TryParse (attributeSyntax, attributeData, out var baseTypeData)) {
						// TODO, set the context data
					}

					break;
				case "ModelAttribute":
					if (ModelParser.TryParse (attributeSyntax, attributeData, out var modelData)) {
						// TODO, set the context data
					}

					break;
				case "ProtocolAttribute": //these don't have additional info to parse through..
					break;
				case "DisableDefaultCtorAttribute":
					break;
				case "CategoryAttribute":
					break;
				// should we have a default case?
				}
			}
		}

		return true;
	}
}
