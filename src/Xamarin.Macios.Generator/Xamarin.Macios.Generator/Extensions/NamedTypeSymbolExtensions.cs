using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xamarin.Macios.Generator.Attributes;

namespace Xamarin.Macios.Generator.Extensions;

public static class NamedTypeSymbolExtensions {
	public static bool TryGetProperties (this INamedTypeSymbol classSymbol,
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
			var attributes = symbol.GetAttributeData ();
			if (attributes.Count == 0)
				continue;

			// based on the attributes we can decide what to do with the property, in the old generator we needed
			// to check if the properties come from a protocol that we are implementing since everything is an interface
			// in the current case, that is not an issue.
			if (attributes.TryGetValue (AttributesNames.FieldAttribute, out var fieldAttrData)) {
				var fieldSyntax = fieldAttrData.ApplicationSyntaxReference?.GetSyntax ();
				if (fieldSyntax is null)
					continue;

				if (FieldData.TryParse (fieldSyntax, fieldAttrData, out var fieldData)) {
					fieldBucket.Add ((Symbol: symbol, FieldData: fieldData,
						IsNotification: attributes.ContainsKey (AttributesNames.NotificationAttribute)));
				} else {
					// TODO: diagnostics
				}
			}

			if (attributes.TryGetValue (AttributesNames.ExportAttribute, out var exportAttrData)) {
				var syntax = exportAttrData.ApplicationSyntaxReference?.GetSyntax ();
				if (syntax is not null) {
					if (ExportData.TryParse (syntax, exportAttrData, out var exportData))
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

	public static bool TryGetEnumFields (this INamedTypeSymbol enumSymbol,
		[NotNullWhen (true)]
		out ImmutableArray<(IFieldSymbol Symbol, FieldData FieldData)>? fields,
		[NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		fields = null;
		diagnostics = null;
		
		// because we are dealing with an enum, we need to get all the fields from the symbol but we need to
		// keep the order in which they are defined in the source code.

		var fieldBucket =
			ImmutableArray.CreateBuilder<(IFieldSymbol Symbol, FieldData FieldData)> ();

		var members = enumSymbol.GetMembers ().OfType<IFieldSymbol> ().ToArray ();
		foreach (var fieldSymbol in members) {
			var attributes = fieldSymbol.GetAttributeData ();
			if (attributes.Count == 0)
				continue;

			// Get all the FieldAttribute, parse it and add the data to the result
			if (attributes.TryGetValue (AttributesNames.FieldAttribute, out var fieldAttrData)) {
				var fieldSyntax = fieldAttrData.ApplicationSyntaxReference?.GetSyntax ();
				if (fieldSyntax is null)
					continue;

				if (FieldData.TryParse (fieldSyntax, fieldAttrData, out var fieldData)) {
					fieldBucket.Add ((Symbol: fieldSymbol, FieldData: fieldData));
				} else {
					// TODO: diagnostics
				}
			}
		}

		fields = fieldBucket.ToImmutable ();
		return true;
	}
}
