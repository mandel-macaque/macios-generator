using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Extensions;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Emitters;

public class EnumEmitter : ICodeEmitter<EnumDeclarationSyntax> {
	readonly EnumBindingContext _context;
	readonly TabbedStringBuilder _builder;
    public string SymbolName => $"{_context.SymbolName}Extensions";

    public EnumEmitter (EnumBindingContext? context, TabbedStringBuilder builder)
    {
        _context = context ?? throw new ArgumentNullException (nameof (context));
        _builder = builder;
    }

	void Emit (TabbedStringBuilder classBlock, (IFieldSymbol Symbol, FieldData FieldData) enumField, int index)
	{
		var typeNamespace = enumField.Symbol.ContainingType.ContainingNamespace.Name;
		if (!_context.RootBindingContext.TryComputeLibraryName (enumField.FieldData.LibraryName, typeNamespace,
			    out string? libraryName, out string? libraryPath)) {
			return;
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
	}

	void Emit (TabbedStringBuilder classBlock, ImmutableArray<(IFieldSymbol Symbol, FieldData FieldData)> fields)
	{
		for (var index = 0; index < fields.Length; index++) {
			var field = fields [index];
			classBlock.AppendLine ();
			Emit (classBlock, field, index);
		}
	}

	void Emit (TabbedStringBuilder classBlock, INamedTypeSymbol enumSymbol,
		ImmutableArray<(IFieldSymbol Symbol, FieldData FieldData)>? members)
	{
		if (members is null)
			return;

		// smart enum require 4 diff methods to be able to retrieve the values

		// Get constant
		classBlock.AppendFormatLine ("public static NSString? GetConstant (this {0} self)", enumSymbol.Name);
		using (var getConstantBlock = classBlock.CreateBlock (isBlock: true)) {
			getConstantBlock.AppendLine ("IntPtr ptr = IntPtr.Zero;");
			using (var switchBlock = getConstantBlock.CreateBlock ("switch ((int) self)", isBlock: true)) {
				for (var index = 0; index < members.Value.Length; index++) {
					var (symbol, fieldData) = members.Value [index];
					switchBlock.AppendFormatLine ("case {0}: // {1}", index, fieldData.SymbolName);
					switchBlock.AppendFormatLine ("\tptr = {0};", fieldData.SymbolName);
					switchBlock.AppendFormatLine ("\tbreak;");
				}
			}

			getConstantBlock.AppendLine ("return (NSString?) Runtime.GetNSObject (ptr);");
		}

		classBlock.AppendLine ();
		classBlock.AppendFormatLine ("public static {0} GetValue (NSString constant)", enumSymbol.Name);
		// Get value
		using (var getValueBlock = classBlock.CreateBlock (isBlock: true)) {
			getValueBlock.AppendLine ("if (constant is null)");
			getValueBlock.AppendLine ("\tthrow new ArgumentNullException (nameof (constant));");
			foreach ((IFieldSymbol? fieldSymbol, FieldData? fieldData) in members) {
				getValueBlock.AppendFormatLine ("if (constant.IsEqualTo ({0}))", fieldData.SymbolName);
				getValueBlock.AppendFormatLine ("\treturn {0}.{1};", enumSymbol.Name, fieldSymbol.Name);
			}

			getValueBlock.AppendLine (
				"throw new NotSupportedException ($\"{constant} has no associated enum value on this platform.\");");
		}

		classBlock.AppendLine ();
		// To ContantArray
		classBlock.AppendRaw (
@$"internal static NSString?[]? ToConstantArray (this {enumSymbol.Name}[]? values)
{{
	if (values is null)
		return null;
	var rv = new global::System.Collections.Generic.List<NSString?> ();
	for (var i = 0; i < values.Length; i++) {{
		var value = values [i];
		rv.Add (value.GetConstant ());
	}}
	return rv.ToArray ();
}}"
);
		classBlock.AppendLine ();
		// ToEnumArray
		classBlock.AppendRaw (
@$"internal static {enumSymbol.Name}[]? ToEnumArray (this NSString[]? values)
{{
	if (values is null)
		return null;
	var rv = new global::System.Collections.Generic.List<{enumSymbol.Name}> ();
	for (var i = 0; i < values.Length; i++) {{
		var value = values [i];
		rv.Add (GetValue (value));
	}}
	return rv.ToArray ();
}}"
);
	}

    public void Emit ()
    {
	    if (!_context.Symbol.TryGetEnumFields (out var members,
		        out var diagnostics) || members.Value.Length == 0) {
		    // TODO: Haritha diagnostics?
	    }
	    // in the old generator we had to copy over the enum, in this new approach the only code
	    // we need to create is the extension class for the enum that is backed by fields
	    _builder.AppendFormatLine ("namespace {0};", _context.Namespace);
	    _builder.AppendLine ();

	    _builder.AppendGeneratedCodeAttribute ();
	    _builder.AppendFormatLine ("static public partial class {0}Extensions", _context.Symbol.Name);
	    using (var classBlock = _builder.CreateBlock (isBlock: true)) {
		    classBlock.AppendLine ();
		    classBlock.AppendFormatLine ("static IntPtr[] values = new IntPtr [{0}];", members.Value.Length);
		    // foreach member in the enum we need to create a field that holds the value, the property emitter
		    // will take care of generating the property. Do not order by name to keep the order of the enum
		    Emit (classBlock, members.Value);

		    classBlock.AppendLine ();
		    // emit the extension methods that will be used to get the values from the enum
		    Emit (classBlock, _context.Symbol, members);
	    }
    }

    public bool TryValidate ([NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
    {
        // TODO: Implement validation
        diagnostics = null;
        return true;
    }
}
