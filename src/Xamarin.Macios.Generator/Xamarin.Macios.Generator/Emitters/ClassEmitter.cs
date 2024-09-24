using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Extensions;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Emitters;

public class ClassEmitter : ICodeEmitter<ClassDeclarationSyntax> {
	readonly ClassBindingContext _context;
	readonly TabbedStringBuilder _builder;

	public string SymbolName => _context.SymbolName;

	public ClassEmitter (ClassBindingContext? context, TabbedStringBuilder builder)
	{
		_context = context ?? throw new ArgumentNullException (nameof(context));
		_builder = builder;
	}

	static string GetNotificationName (IPropertySymbol symbol)
	{
		// TODO: fetch the NotificationAttribute, see if there is an override there.
		var name = symbol.Name;
		if (name.EndsWith ("Notification", StringComparison.Ordinal))
			return name.Substring (0, name.Length - "Notification".Length);
		return name;
	}

	public static void EmitDefaultConstructor (TabbedStringBuilder classBlock, string className)
	{
		classBlock.AppendGeneratedCodeAttribute ();
		classBlock.AppendEditorBrowsableAttribute ();
		classBlock.AppendLine ("[Export (\"init\")]");
		classBlock.AppendLine ($"public {className} () : base (NSObjectFlag.Empty)");
		using (var body = classBlock.CreateBlock (isBlock: true)) {
			using (var ifBlock = body.CreateBlock ("if (IsDirectBinding)", isBlock: true)) {
				ifBlock.AppendLine (
					"InitializeHandle (global::ObjCRuntime.Messaging.IntPtr_objc_msgSend (this.Handle, global::ObjCRuntime.Selector.GetHandle (\"init\")), \"init\");");
			}

			using (var elseBlock = body.CreateBlock ("else", isBlock: true)) {
				elseBlock.AppendLine (
					"InitializeHandle (global::ObjCRuntime.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, global::ObjCRuntime.Selector.GetHandle (\"init\")), \"init\");");
			}
		}
	}

	public static void EmitSkipInit (TabbedStringBuilder classBlock, string className)
	{
		classBlock.AppendGeneratedCodeAttribute ();
		classBlock.AppendEditorBrowsableAttribute ();
		classBlock.AppendLine ($"protected {className} (NSObjectFlag t) : base (t)");
		using (var body = classBlock.CreateBlock (isBlock: true)) {
			// empty body
		}
	}

	public static void EmitNativeHandlerConstructor (TabbedStringBuilder classBlock, string className)
	{
		classBlock.AppendGeneratedCodeAttribute ();
		classBlock.AppendEditorBrowsableAttribute ();
		classBlock.AppendLine ($"protected internal {className} (NativeHandle handle) : base (handle)");
		using (var body = classBlock.CreateBlock (isBlock: true)) {
			// empty body
		}
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

	static void EmitEnum (TabbedStringBuilder block, MethodKind methodKind,
		(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification) property,
		INamedTypeSymbol enumSymbol, string symbolName, string libraryName, bool isSmartEnum)
	{
		if (isSmartEnum)
			EmitSmartEnum (block, methodKind, property, symbolName, libraryName);
		else
			EmitNativeEnum (block, methodKind, property, enumSymbol, symbolName, libraryName);
	}

	static void EmitNativeEnum (TabbedStringBuilder block, MethodKind methodKind,
		(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification) property,
		INamedTypeSymbol enumSymbol, string symbolName, string libraryName, bool isSmartEnum = false)
	{
		// based on the enum symbol backend we will generate the code for the accessor.
		var cast = enumSymbol.EnumUnderlyingType?.SpecialType switch {
			SpecialType.System_UInt64 => "ulong",
			SpecialType.System_Int64 => "long",
			SpecialType.System_UInt32 => "uint",
			SpecialType.System_Int32 => "int",
			SpecialType.System_Int16 => "short",
			SpecialType.System_Byte => "byte",
			_ => "",
		};

		switch (methodKind) {
		case MethodKind.PropertyGet:
			EmitEnumGetter ();
			break;
		case MethodKind.PropertySet:
			EmitEnumSetter ();
			break;
		}

		void EmitEnumGetter ()
			=> block.AppendFormatLine ("return ({0}) ({1}) Dlfcn.{2} (Libraries.{3}.Handle, \"{4}\");",
				property.Symbol.Type.Name, cast, enumSymbol.EnumUnderlyingType?.SpecialType.GetDlfcnGetMethod ()!,
				libraryName, symbolName);

		void EmitEnumSetter ()
			=> block.AppendFormatLine ("Dlfcn.{0} (Libraries.{2}.Handle, \"{1}\", value);",
				enumSymbol.EnumUnderlyingType?.SpecialType.GetDlfcnSetMethod ()!, libraryName,
				property.FieldData.SymbolName);
	}

	static void EmitSmartEnum (TabbedStringBuilder block, MethodKind methodKind,
		(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification) property,
		string symbolName, string libraryName)
	{
		switch (methodKind) {
		case MethodKind.PropertyGet:
			EmitSmartEnumGetter ();
			break;
		case MethodKind.PropertySet:
			EmitStarterEnumSetter ();
			break;
		}

		void EmitSmartEnumGetter ()
		{
			block.AppendFormatLine ("if (_{0} is null)", property.Symbol.Name);
			block.AppendFormatLine ("\t_{0} = Dlfcn.GetStringConstant (Libraries.{1}.Handle, \"{2}\")!;",
				property.Symbol.Name,
				libraryName, symbolName);
			block.AppendFormatLine ("return {0}Extensions.GetValue (_{1});", property.Symbol.Type.Name,
				property.Symbol.Name);
		}

		void EmitStarterEnumSetter ()
		{
			block.AppendFormatLine ("Dlfcn.SetString (Libraries.{0}.Handle, \"{1}\", value.GetConstant ());",
				libraryName, symbolName);
		}
	}

	bool Emit (TabbedStringBuilder classBlock,
		(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification) property)
	{
		var typeNamespace = property.Symbol.ContainingType.ContainingNamespace.Name;
		if (!_context.RootBindingContext.TryComputeLibraryName (property.FieldData.LibraryName, typeNamespace,
			    out string? libraryName, out string? libraryPath)) {
			return false;
		}
		// deal with the fact that the returning typeof the field can be a smart enumerator

		var fieldTypeName = string.Empty;
		string? smartEnumTypeName = null;
		var isSmartEnum = property.Symbol.Type.IsSmartEnum ();
		if (isSmartEnum) {
			fieldTypeName = property.Symbol.FormatType ();
			smartEnumTypeName = property.Symbol.Type.GetSmartEnumType ();
		} else {
			//var type = node.
			fieldTypeName = property.Symbol.FormatType ();
		}

		// Value types we dont cache for now, to avoid Nullable<T>
		if (!property.Symbol.Type.IsValueType || isSmartEnum) {
			classBlock.AppendGeneratedCodeAttribute ();
			classBlock.AppendFormatLine ("static {0}? _{1};", smartEnumTypeName ?? fieldTypeName, property.Symbol.Name);
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
				if (property.Symbol.Type is not INamedTypeSymbol namedTypeSymbol)
					continue;
				var blockKind = method.MethodKind == MethodKind.PropertyGet ? "get" : "set";
				using (var block = body.CreateBlock (blockKind, isBlock: true)) {
					var typeName = property.Symbol.Type.Name;
					if (property.Symbol.Type.SpecialType != SpecialType.None) {
						EmitNativeField (block, method.MethodKind, property, fieldTypeName, libraryName);
					} else if (property.Symbol.Type.TypeKind == TypeKind.Enum) {
						EmitEnum (block, method.MethodKind, property, namedTypeSymbol, property.FieldData.SymbolName,
							libraryName, isSmartEnum);
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

	void Emit (TabbedStringBuilder classBlock,
		ImmutableArray<(IPropertySymbol Symbol, FieldData FieldData, bool IsNotification)> fields)
	{
		foreach (var field
		         in fields.OrderBy (p => p.Symbol.Name, StringComparer.Ordinal)) {
			classBlock.AppendLine ();
			Emit (classBlock, field);
		}
	}

	void Emit (TabbedStringBuilder classBloc, (IPropertySymbol Symbol, ExportData ExportData) boundProperty)
	{
		//throw new System.NotImplementedException();
	}

	void Emit (TabbedStringBuilder classBlock,
		ImmutableArray<(IPropertySymbol Symbol, ExportData ExportData)> boundProperties)
	{
		foreach (var boundProperty
		         in boundProperties.OrderBy (p => p.Symbol.Name, StringComparer.Ordinal)) {
			_builder.AppendLine ();
			Emit (classBlock, boundProperty);
		}
	}

	public void Emit (IMethodSymbol methodSymbol)
	{
		//throw new System.NotImplementedException();
	}

	public void Emit ()
	{
		_builder.AppendLine ("// <auto-generated/>");
		_builder.AppendLine ("using System;");
		_builder.AppendLine ("using System.Drawing;");
		_builder.AppendLine ("using System.Diagnostics;");
		_builder.AppendLine ("using System.ComponentModel;");
		_builder.AppendLine ("using System.Threading.Tasks;");
		_builder.AppendLine ("using System.Runtime.Versioning;");
		_builder.AppendLine ("using System.Runtime.InteropServices;");
		_builder.AppendLine ("using System.Diagnostics.CodeAnalysis;");
		_builder.AppendLine ("using ObjCRuntime;");
		_builder.AppendLine ();
		_builder.AppendFormatLine ("namespace {0};", _context.Namespace);
		_builder.AppendLine ();
		// only register the class if it is not static
		if (!_context.IsStatic) {
			_builder.AppendFormatLine ("[Register(\"{0}\", true)]", _context.SymbolName);
		}

		_builder.AppendFormatLine ("public unsafe {0}partial class {1}",
			_context.IsStatic ? "static " : string.Empty,
			_context.SymbolName);

		using (var classBlock = _builder.CreateBlock (isBlock: true)) {
			// generate the class handle only for not static classes
			if (!_context.IsStatic) {
				classBlock.AppendGeneratedCodeAttribute ();
				classBlock.AppendFormatLine (
					"static readonly NativeHandle class_ptr = Class.GetHandle (\"{0}\");",
					_context.RegisterName);

				classBlock.AppendLine ();
			}

			// generate the default constructors only if they are not disabled or the class is not static
			if (!_context.IsStatic) {
				EmitDefaultConstructor (classBlock, _context.SymbolName);
				classBlock.AppendLine ();
				EmitSkipInit (classBlock, _context.SymbolName);
				classBlock.AppendLine ();
				EmitNativeHandlerConstructor (classBlock,
					_context.SymbolName);
			}

			// generate the methods
			var methods = _context.Symbol.GetMembers ().OfType<IMethodSymbol> ();
			foreach (var methodSymbol in methods) {
				Emit (methodSymbol);
			}

			// generate the properties, order them by name to make the output deterministic
			if (_context.Symbol.TryGetProperties (out var fields,
				    out var boundProperties, out var diagnostics)) {
				Emit (classBlock, fields.Value);
				Emit (classBlock, boundProperties.Value);
			} else {
				// TODO: diagnostics
			}
		}
	}

	public bool TryValidate ([NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
	{
		//TODO: check is props have right data, etc
		//get class w attributes, ensure attrs are being used correctly
		diagnostics = null;
		return true;
	}
}
