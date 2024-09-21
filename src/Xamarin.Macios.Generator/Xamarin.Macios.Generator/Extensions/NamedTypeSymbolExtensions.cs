using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Extensions;

public static class NamedTypeSymbolExtensions {
	public static bool TryGetProperties (this INamedTypeSymbol classSymbol, ClassBindingContext classBindingContext,
		[NotNullWhen (true)]
		out ImmutableArray<(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification)>? fields,
		[NotNullWhen (true)] out ImmutableArray<(IPropertySymbol Symbol, ExportData ExpotData)>? boundProperties,
		[NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		diagnostics = null;
		// create buckets for the result
		var fieldBucket =
			ImmutableArray.CreateBuilder<(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification)> ();
		var boundPropertiesBucket = ImmutableArray.CreateBuilder<(IPropertySymbol, ExportData)> ();

		// we need to get all the properties from the classSymbol, parse their attributes and add them to a bucket.
		var propertySymbols = classSymbol.GetMembers ()
			.OfType<IPropertySymbol> ();

		foreach (var symbol in propertySymbols) {
			var boundAttributes = symbol.GetAttributes ();
			if (boundAttributes.Length == 0) {
				// if the property has no attributes we can't do anything with it
				continue;
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

			// based on the attributes we can decide what to do with the property, in the old generator we needed
			// to check if the properties come from a protocol that we are implementing since everything is an interface
			// in the current case, that is not an issue.
			if (attributes.TryGetValue (AttributesNames.FieldAttribute, out var fieldAttrData)) {
				var fieldSyntax = fieldAttrData.ApplicationSyntaxReference?.GetSyntax ();
				if (fieldSyntax is null)
					continue;

				if (FieldParser.TryParse (fieldSyntax, fieldAttrData, out var fieldData)) {
					fieldBucket.Add ((Symbol: symbol, FieldData: fieldData,
						IsNotification: attributes.ContainsKey (AttributesNames.NotificationAttribute)));
				} else {
					// TODO: diagnostics
				}
			}

			if (attributes.TryGetValue (AttributesNames.ExportAttribute, out var exportAttrData)) {
				var syntax = exportAttrData.ApplicationSyntaxReference?.GetSyntax ();
				if (syntax is not null) {
					if (ExportParser.TryParse (syntax, exportAttrData, out var exportData))
						boundPropertiesBucket.Add ((symbol, exportData));
				} else {
					// TODO: diagnostics
				}
			}
		}

		fields = fieldBucket.ToImmutable ();
		boundProperties = boundPropertiesBucket.ToImmutable ();
		return true;
	}
}
