using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Parsers;

namespace Xamarin.Macios.Generator.Emitters;

public class MethodEmitter (SymbolBindingContext context, TabbedStringBuilder classBlock) {
	public void Emit (IMethodSymbol methodSymbol)
	{
		//throw new System.NotImplementedException();
	}

	public bool TryEmit (INamedTypeSymbol enumSymbol,
		ImmutableArray<(IFieldSymbol Symbol, FieldData FieldData)>? members)
	{
		if (members is null)
			return false;

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
		return true;
	}
}
