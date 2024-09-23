using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Extensions;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Emitters;

class PropertyEmitter (SymbolBindingContext context, TabbedStringBuilder classBlock) {
	static string GetNotificationName (IPropertySymbol symbol)
	{
		// TODO: fetch the NotificationAttribute, see if there is an override there.
		var name = symbol.Name;
		if (name.EndsWith ("Notification", StringComparison.Ordinal))
			return name.Substring (0, name.Length - "Notification".Length);
		return name;
	}

	static void EmitBackedField (TabbedStringBuilder block, string propertyName, string nativeCallFormat,
		params object [] args)
	{
		using (var ifBlock = block.CreateBlock ($"if (_{propertyName} is null)", isBlock: true)) {
			ifBlock.AppendFormatLine ($"_{propertyName} = {nativeCallFormat}", args);
		}

		block.AppendLine ($"return _{propertyName};");
	}

	static void EmitNSStringField (TabbedStringBuilder block, MethodKind methodKind, string propertyName,
		string symbolName, string libraryName)
	{
		switch (methodKind) {
		case MethodKind.PropertyGet:
			EmitNSStringGetter ();
			break;
		case MethodKind.PropertySet:
			EmitNSStringSetter ();
			break;
		}

		void EmitNSStringGetter ()
			=> EmitBackedField (block, propertyName,
				"Dlfcn.GetStringConstant (Libraries.{0}.Handle, \"{1}\")!;", libraryName, symbolName);

		void EmitNSStringSetter ()
			=> block.AppendFormatLine ("Dlfcn.SetString (Libraries.{0}.Handle, \"{1}\", value);", libraryName,
				symbolName);
	}

	static void EmitNSObjectField (TabbedStringBuilder block, MethodKind methodKind,
		string propertyName, string propertyType, string symbolName, string libraryName)
	{
		switch (methodKind) {
		case MethodKind.PropertyGet:
			EmitNSObjectGetter ();
			break;
		case MethodKind.PropertySet:
			EmitNSObjectSetter ();
			break;
		}

		void EmitNSObjectGetter () => EmitBackedField (block, propertyName,
			"Runtime.GetNSObject<{0}> (Dlfcn.GetIndirect (Libraries.{1}.Handle, \"{2}\"))!;",
			propertyType, libraryName, symbolName);

		void EmitNSObjectSetter ()
		{
			// we need to make a diff between an NSArray and all other NSObjects
			block.AppendFormatLine (
				propertyType == "NSArray"
					? "Dlfcn.SetArray (Libraries.{0}.Handle, \"{1}\", value);"
					: "Dlfcn.SetObject (Libraries.{0}.Handle, \"{1}\", value);",
				libraryName, symbolName);
		}
	}

	static void EmitUTTypeField (TabbedStringBuilder block, MethodKind methodKind, string propertyName,
		string symbolName, string libraryName)
	{
		switch (methodKind) {
		case MethodKind.PropertyGet:
			EmitUTTypeGetter ();
			break;
		case MethodKind.PropertySet:
			EmitUTTypeSetter ();
			break;
		}

		void EmitUTTypeGetter ()
			=> EmitBackedField (block, propertyName,
				"Runtime.GetNSObject<UTType> (Dlfcn.GetIntPtr (Libraries.{0}.Handle, \"{1}\"))!;",
				libraryName, symbolName);

		void EmitUTTypeSetter ()
			=> block.AppendFormatLine ("Dlfcn.SetObject (Libraries.{0}.Handle, \"{1}\", value.Handle);", libraryName,
				symbolName);
	}

	static void EmitNativeField (TabbedStringBuilder block, MethodKind methodKind,
		(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification) property,
		string displayName, string libraryName)
	{
		switch (methodKind) {
		case MethodKind.PropertyGet:
			EmitNativeTypeGetter ();
			break;
		case MethodKind.PropertySet:
			EmitNativeTypeSetter ();
			break;
		}

		void EmitNativeTypeSetter ()
		{
			var method = displayName switch {
				"nint" => "SetNInt",
				"nuint" => "SetNUInt",
				"float" => "SetFloat",
				"nfloat" => "SetNFloat",
				"CGSize" => "SetCGSize",
				"SizeF" => "SetSizeF",
				_ => property.Symbol.Type.SpecialType.GetDlfcnSetMethod ()
			};
			block.AppendFormatLine ("Dlfcn.{0} (Libraries.{2}.Handle, \"{1}\", value);",
				method, libraryName, property.FieldData.SymbolName);
		}

		void EmitNativeTypeGetter ()
		{
			var method = displayName switch {
				"nint" => "GetNInt",
				"nuint" => "GetNUInt",
				"float" => "GetFloat",
				"nfloat" => "GetNFloat",
				"CGSize" => "GetCGSize",
				"SizeF" => "GetSizeF",
				_ => property.Symbol.Type.SpecialType.GetDlfcnGetMethod ()
			};
			block.AppendFormatLine ("return Dlfcn.{0} (Libraries.{1}.Handle, \"{2}\");",
				method, libraryName, property.FieldData.SymbolName);
		}
	}

	bool TryEmit ((IPropertySymbol Symbol, FieldData FieldData, bool IsNotification) property)
	{
		var typeNamespace = property.Symbol.ContainingType.ContainingNamespace.Name;
		if (!context.RootBindingContext.TryComputeLibraryName (property.FieldData.LibraryName, typeNamespace,
			    out string? libraryName, out string? libraryPath)) {
			return false;
		}
		// deal with the fact that the returning typeof the field can be a smart enumerator

		var fieldTypeName = string.Empty;
		string? smartEnumTypeName = null;
		var isSmartEnum = property.Symbol.Type.IsSmartEnum ();
		if (isSmartEnum) {
			fieldTypeName = property.Symbol.FormatType ();
			smartEnumTypeName = property.Symbol.FormatType ();
		} else {
			//var type = node.
			fieldTypeName = property.Symbol.FormatType ();
		}

		// Value types we dont cache for now, to avoid Nullable<T>
		if (!property.Symbol.Type.IsValueType || isSmartEnum) {
			classBlock.AppendGeneraedCodeAttribute ();
			classBlock.AppendFormatLine ("static {0}? _{1};", fieldTypeName, property.Symbol.Name);
			classBlock.AppendLine ();
		}

		classBlock.AppendFormatLine ("[Field (\"{0}\", \"{1}\")]", property.FieldData.SymbolName,
			libraryPath ?? libraryName);
		if (property.IsNotification) {
			classBlock.AppendFormatLine (
				"[Advice (\"Use {0}.Notifications.Observe{1} helper method instead.\")]",
				property.Symbol.ContainingType.Name,
				GetNotificationName (property.Symbol));
		}

		classBlock.AppendFormatLine ("{0} partial static {1}{2} {3}",
			property.Symbol.DeclaredAccessibility == Accessibility.Internal ? "internal" : "public",
			fieldTypeName, property.Symbol.NullableAnnotation == NullableAnnotation.Annotated ? "?" : string.Empty,
			property.Symbol.Name);

		using (var body = classBlock.CreateBlock (isBlock: true)) {
			// loop over the getter and setter and generate the code for each of them
			foreach (var method in new [] { property.Symbol.GetMethod, property.Symbol.SetMethod }) {
				if (method is null)
					continue;
				var blockKind = method.MethodKind == MethodKind.PropertyGet ? "get" : "set";
				using (var block = body.CreateBlock (blockKind, isBlock: true)) {
					var typeName = property.Symbol.Type.Name;
					if (property.Symbol.Type.SpecialType != SpecialType.None) {
						if (property.Symbol.Type.SpecialType == SpecialType.System_Enum) {
							// TODO: emit enum
						} else {
							EmitNativeField (block, method.MethodKind, property, fieldTypeName, libraryName);
						}
					} else {
						switch (typeName) {
						case "NSString":
							EmitNSStringField (block, method.MethodKind, property.Symbol.Name,
								property.FieldData.SymbolName, libraryName);
							break;
						// NSObject types
						case "NSArray":
						case "NSNumber":
							EmitNSObjectField (block, method.MethodKind, property.Symbol.Name,
								property.Symbol.Type.Name,
								property.FieldData.SymbolName, libraryName);
							break;
						case "UTType":
							EmitUTTypeField (block, method.MethodKind, property.Symbol.Name,
								property.FieldData.SymbolName, libraryName);
							break;
						// native types that do not match roslyn special types
						case "nfloat":
						case "float":
						case "CGSize":
						case "SizeF":
							EmitNativeField (block, method.MethodKind, property, typeName, libraryName);
							break;
						default:
							// TODO: diagnostics
							break;
						}
					}
				}
			}
		}

		return true;
	}

	void TryEmit ((IPropertySymbol Symbol, ExportData ExportData) boundProperty)
	{
		//throw new System.NotImplementedException();
	}

	bool TryEmit ((IFieldSymbol Symbol, FieldData FieldData) enumField, int index)
	{
		var typeNamespace = enumField.Symbol.ContainingType.ContainingNamespace.Name;
		if (!context.RootBindingContext.TryComputeLibraryName (enumField.FieldData.LibraryName, typeNamespace,
			    out string? libraryName, out string? libraryPath)) {
			return false;
		}

		classBlock.AppendFormatLine ("[Field (\"{0}\", \"{1}\")]", enumField.FieldData.SymbolName,
			libraryPath ?? libraryName);
		classBlock.AppendFormatLine ("internal unsafe static IntPtr {0}", enumField.FieldData.SymbolName);
		using (var propertyBlock = classBlock.CreateBlock (isBlock: true))
		using (var getterBlock = propertyBlock.CreateBlock ("get", isBlock: true)) {
			getterBlock.AppendFormatLine ("fixed (IntPtr *storage = &values [{0}])", index);
			getterBlock.AppendFormatLine("\treturn Dlfcn.CachePointer (Libraries.{0}.Handle, \"{1}\", storage);",
				libraryPath ?? libraryName, enumField.FieldData.SymbolName);
		}
		return false;
	}

	public bool TryEmit (ImmutableArray<(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification)> fields,
		[NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		var diagnosticsBucket = ImmutableArray.CreateBuilder<(IPropertySymbol, FieldData)> ();
		foreach (var field
		         in fields.OrderBy (p => p.Symbol.Name, StringComparer.Ordinal)) {
			classBlock.AppendLine ();
			TryEmit (field);
		}

		diagnostics = null;
		return true;
	}

	public bool TryEmit (ImmutableArray<(IPropertySymbol Symbol, ExportData ExportData)> boundProperties,
		[NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		var diagnosticsBucket = ImmutableArray.CreateBuilder<(IPropertySymbol, FieldData)> ();
		foreach (var boundProperty
		         in boundProperties.OrderBy (p => p.Symbol.Name, StringComparer.Ordinal)) {
			classBlock.AppendLine ();
			TryEmit (boundProperty);
		}

		diagnostics = null;
		return true;
	}

	public bool TryEmit (ImmutableArray<(IFieldSymbol Symbol, FieldData FieldData)> fields,
		[NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		var diagnosticsBucket = ImmutableArray.CreateBuilder<(IPropertySymbol, FieldData)> ();
		for (var index = 0; index < fields.Length; index++) {
			var field = fields [index];
			classBlock.AppendLine ();
			TryEmit (field, index);
		}
		diagnostics = null;
		return true;
	}
}
