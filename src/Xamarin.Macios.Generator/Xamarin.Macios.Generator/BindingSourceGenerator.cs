using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xamarin.Macios.Generator.Context;
using Xamarin.Macios.Generator.Emitters;
using Xamarin.Macios.Generator.Extensions;

namespace Xamarin.Macios.Generator;

/// <summary>
/// Source generator that writes the code needed for the export of a given selector in a class. This
/// generator does not provide the attributes but relies on those from the xamarin-macios project, this way
/// we can maintain API compatibility and remove the need of a two step compilation.
/// </summary>
[Generator]
public class BindingSourceGenerator : IIncrementalGenerator {
	// The following attribute is a copy of the BasetypeAttribute from the xamarin-macios project, this attribute
	// is part of bgen, but instead we would want to expose itby the generator.
	private const string BaseTypeAttributeSourceCode = @"// <auto-generated/>

namespace Foundation 
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Enum)]
    public class BindingTypeAttribute : System.Attribute
    {
        public string Name { get; set; }
        public Type [] Events { get; set; }
        public string [] Delegates { get; set; }
        public bool Singleton { get; set; }

        // If set, the code will keep a reference in the EnsureXXX method for
        // delegates and will clear the reference to the object in the method
        // referenced by KeepUntilRef.   Currently uses an ArrayList, so this
        // is not really designed as a workaround for systems that create
        // too many objects, but two cases in particular that users keep
        // trampling on: UIAlertView and UIActionSheet
        public string KeepRefUntil { get; set; }
    }
}";

	private const string NotificationAttributeSourceCode = @"// <auto-generated/>
namespace Foundation 
{
    [AttributeUsage (AttributeTargets.Property, AllowMultiple = true)]
    public class NotificationAttribute : Attribute {
        public NotificationAttribute (Type t) { Type = t; }
        public NotificationAttribute (Type t, string notificationCenter) { Type = t; NotificationCenter = notificationCenter; }
        public NotificationAttribute (string notificationCenter) { NotificationCenter = notificationCenter; }
        public NotificationAttribute () { }

        public Type Type { get; set; }
        public string NotificationCenter { get; set; }
    }
}
";

	public void Initialize (IncrementalGeneratorInitializationContext context)
	{
		// Add the binding generator attributes to the compilation. This are only available when the
		// generator is used, similar to how bgen works.
		context.RegisterPostInitializationOutput (ctx => ctx.AddSource (
			"BindingTypeAttribute.g.cs",
			SourceText.From (BaseTypeAttributeSourceCode, Encoding.UTF8)));

		context.RegisterPostInitializationOutput (ctx => ctx.AddSource (
			"NotificationAttribute.g.cs",
			SourceText.From (NotificationAttributeSourceCode, Encoding.UTF8)));

		// Register to the compilation and listen for the classes that have the [BindingType] attribute.
		var classProvider = context.SyntaxProvider
			.CreateSyntaxProvider (
				static (s, _) => s is ClassDeclarationSyntax,
				(ctx, _) => GetDeclarationForSourceGen<ClassDeclarationSyntax> (ctx))
			.Where (t => t.BindingAttributeFound)
			.Select ((t, _) => t.Declaration);

		context.RegisterSourceOutput (context.CompilationProvider.Combine (classProvider.Collect ()),
			((ctx, t) => GenerateClassesCode (ctx, t.Left, t.Right)));

		// same process for enums
		var enumProvider = context.SyntaxProvider
			.CreateSyntaxProvider (
				static (s, _) => s is EnumDeclarationSyntax,
				(ctx, _) => GetDeclarationForSourceGen<EnumDeclarationSyntax> (ctx))
			.Where (t => t.BindingAttributeFound)
			.Select ((t, _) => t.Declaration);

		context.RegisterSourceOutput (context.CompilationProvider.Combine (enumProvider.Collect ()),
			((ctx, t) => GenerateEnumCode (ctx, t.Left, t.Right)));
	}

	static (T Declaration, bool BindingAttributeFound) GetDeclarationForSourceGen<T> (GeneratorSyntaxContext context)
		where T : BaseTypeDeclarationSyntax
	{
		var classDeclarationSyntax = (T) context.Node;

		// Go through all attributes of the class.
		foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
		foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes) {
			if (context.SemanticModel.GetSymbolInfo (attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
				continue; // if we can't get the symbol, ignore it

			string attributeName = attributeSymbol.ContainingType.ToDisplayString ();

			// Check the full name of the [Binding] attribute.
			if (attributeName == "Foundation.BindingTypeAttribute")
				return (classDeclarationSyntax, true);
		}

		return (classDeclarationSyntax, false);
	}

	void GenerateClassesCode (SourceProductionContext context, Compilation compilation,
		ImmutableArray<ClassDeclarationSyntax> classDeclarations)
	{
		var bindingContext = new RootBindingContext (compilation);
		foreach (var classDeclarationSyntax in classDeclarations) {
			var semanticModel = compilation.GetSemanticModel (classDeclarationSyntax.SyntaxTree);
			if (semanticModel.GetDeclaredSymbol (classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
				continue;

			var classBindingContext = new ClassBindingContext (bindingContext, semanticModel,
				classSymbol, classDeclarationSyntax);
			// parse the class level attributes so that we have all the needed data to generate the class
			if (!classDeclarationSyntax.TryParseAttributes (classBindingContext, out var diagnostics)) {
				// TODO: Add a diagnostic here to inform the user that the attribute is missing
				continue;
			}

			var sb = new TabbedStringBuilder (new());
			// TODO: move this logic to the class data
			sb.AppendLine ("// <auto-generated/>");
			sb.AppendLine ("using System;");
			sb.AppendLine ("using System.Drawing;");
			sb.AppendLine ("using System.Diagnostics;");
			sb.AppendLine ("using System.ComponentModel;");
			sb.AppendLine ("using System.Threading.Tasks;");
			sb.AppendLine ("using System.Runtime.Versioning;");
			sb.AppendLine ("using System.Runtime.InteropServices;");
			sb.AppendLine ("using System.Diagnostics.CodeAnalysis;");
			sb.AppendLine ("using ObjCRuntime;");
			sb.AppendLine ();
			sb.AppendFormatLine ("namespace {0};", classBindingContext.Namespace);
			sb.AppendLine ();
			// only register the class if it is not static
			if (!classBindingContext.IsStatic) {
				sb.AppendFormatLine ("[Register(\"{0}\", true)]", classBindingContext.SymbolName);
			}

			sb.AppendFormatLine ("public unsafe {0}partial class {1}",
				classBindingContext.IsStatic ? "static " : string.Empty,
				classBindingContext.SymbolName);

			using (var classBlock = sb.CreateBlock (isBlock: true)) {
				// generate the class handle only for not static classes
				if (!classBindingContext.IsStatic) {
					classBlock.AppendGeneraedCodeAttribute ();
					classBlock.AppendFormatLine (
						"static readonly NativeHandle class_ptr = Class.GetHandle (\"{0}\");",
						classBindingContext.RegisterName);

					classBlock.AppendLine ();
				}

				// generate the default constructors only if they are not disabled or the class is not static
				if (!classBindingContext.IsStatic) {
					DefaultConstructorEmitter.RenderDefaultConstructor (classBlock, classBindingContext.SymbolName);
					classBlock.AppendLine ();
					DefaultConstructorEmitter.RenderSkipInit (classBlock, classBindingContext.SymbolName);
					classBlock.AppendLine ();
					DefaultConstructorEmitter.RenderNativeHandlerConstructor (classBlock,
						classBindingContext.SymbolName);
				}

				// generate the methods
				var methodEmitter = new MethodEmitter (classBindingContext, classBlock);
				var methods = classSymbol.GetMembers ().OfType<IMethodSymbol> ();
				foreach (var methodSymbol in methods) {
					methodEmitter.Emit (methodSymbol);
				}

				// generate the properties, order them by name to make the output deterministic
				var propertyEmitter = new PropertyEmitter (classBindingContext, classBlock);
				if (classSymbol.TryGetProperties (out var fields,
					    out var boundProperties, out diagnostics)) {
					if (!propertyEmitter.TryEmit (fields.Value, out var propertyDiagnostics)) {
					}

					if (!propertyEmitter.TryEmit (boundProperties.Value, out var boundPropertyDiagnostics)) {
					}
				} else {
					// TODO: diagnostics
				}
			}

			var code = sb.ToString ();
			context.AddSource ($"{classBindingContext.SymbolName}.g.cs", SourceText.From (code, Encoding.UTF8));
		}
	}

	void GenerateEnumCode (SourceProductionContext context, Compilation compilation,
		ImmutableArray<EnumDeclarationSyntax> enumDeclarations)
	{
		var bindingContext = new RootBindingContext (compilation);
		foreach (var enumDeclarationSyntax in enumDeclarations) {
			var semanticModel = compilation.GetSemanticModel (enumDeclarationSyntax.SyntaxTree);
			if (semanticModel.GetDeclaredSymbol (enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
				continue;

			var enumBindingContext = new EnumBindingContext (bindingContext, semanticModel,
				enumSymbol, enumDeclarationSyntax);

			if (!enumSymbol.TryGetEnumFields (out var members,
				    out var diagnostics) || members.Value.Length == 0) {
				// could not get the fields
				// TODO: add a diagnostic here
				continue;
			}

			// in the old generator we had to copy over the enum, in this new approach the only code
			// we need to create is the extension class for the enum that is backed by fields
			var sb = new TabbedStringBuilder (new());
			sb.AppendFormatLine ("namespace {0};", enumBindingContext.Namespace);
			sb.AppendLine ();

			sb.AppendGeneraedCodeAttribute ();
			sb.AppendFormatLine ("static public partial class {0}Extensions", enumSymbol.Name);
			using (var classBlock = sb.CreateBlock (isBlock: true)) {
				classBlock.AppendLine ();
				classBlock.AppendFormatLine ("static IntPtr[] values = new IntPtr [{0}];", members.Value.Length);
				// foreach member in the enum we need to create a field that holds the value, the property emitter
				// will take care of generating the property. Do not order by name to keep the order of the enum
				var propertyEmitter = new PropertyEmitter (enumBindingContext, classBlock);
				if (!propertyEmitter.TryEmit (members.Value, out var propertyDiagnostics)) {
					// TODO Diagnostic
				}
				classBlock.AppendLine ();
				// emit the extension methods that will be used to get the values from the enum
				var methodEmitter = new MethodEmitter (enumBindingContext, classBlock);
				if (!methodEmitter.TryEmit (enumSymbol, members)) {
					// TODO: diagnostics
				}

			}

			var code = sb.ToString ();
			context.AddSource ($"{enumBindingContext.SymbolName}Extensions.g.cs", SourceText.From (code, Encoding.UTF8));
		}
	}
}
