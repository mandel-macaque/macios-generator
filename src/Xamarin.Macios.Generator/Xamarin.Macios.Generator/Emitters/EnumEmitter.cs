using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Extensions;

namespace Xamarin.Macios.Generator.Emitters;

public class EnumEmitter : ICodeEmitter<EnumDeclarationSyntax> {

    EnumBindingContext _context;
    TabbedStringBuilder _builder;
    public string SymbolName => $"{_context.SymbolName}Extensions";

    public EnumEmitter (EnumBindingContext? context, TabbedStringBuilder builder)
    {
        _context = context ?? throw new ArgumentNullException (nameof (context));
        _builder = builder;
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
		    var propertyEmitter = new PropertyEmitter (_context, classBlock);
		    if (!propertyEmitter.TryEmit (members.Value, out var propertyDiagnostics)) {
			    // TODO Diagnostic
		    }
		    classBlock.AppendLine ();
		    // emit the extension methods that will be used to get the values from the enum
		    var methodEmitter = new MethodEmitter (_context, classBlock);
		    if (!methodEmitter.TryEmit (_context.Symbol, members)) {
			    // TODO: diagnostics
		    }
	    }
    }

    public bool TryValidate ([NotNullWhen (false)] out ImmutableArray<Diagnostic>? diagnostics)
    {
        // TODO: Implement validation
        diagnostics = null;
        return true;
    }
}
