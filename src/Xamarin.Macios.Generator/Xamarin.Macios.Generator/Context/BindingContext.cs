using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Xamarin.Macios.Generator.External;

namespace Xamarin.Macios.Generator.Context;

public class BindingContext {
	readonly Dictionary<string, string> _libraries = new();

	public PlatformName CurrentPlatform { get; set; }
	public Compilation Compilation { get; set; }
	public bool BindThirdPartyLibrary { get; set; }

	public BindingContext (Compilation compilation)
	{
		Compilation = compilation;
		CurrentPlatform = PlatformName.None;
		// use the reference assembly to determine what platform we are binding
		foreach (var referencedAssemblyName in compilation.ReferencedAssemblyNames) {
			switch (referencedAssemblyName.Name) {
			case "Microsoft.iOS":
				CurrentPlatform = PlatformName.iOS;
				break;
			}
		}
	}

	// TODO: clean code coming from the old generator
	public bool TryComputeLibraryName (string attributeLibraryName, string typeNamespace,
		[NotNullWhen (true)] out string? libraryName,
		out string? libraryPath)
	{
		libraryPath = null;

		if (!string.IsNullOrEmpty (attributeLibraryName)) {
			// Remapped
			libraryName = attributeLibraryName;
			if (libraryName [0] == '+') {
				switch (libraryName) {
				case "+CoreImage":
					libraryName = CurrentPlatform.GetCoreImageMap ();
					break;
				case "+CoreServices":
					libraryName = CurrentPlatform.GetCoreServicesMap ();
					break;
				case "+PDFKit":
					libraryName = "PdfKit";
					libraryPath = CurrentPlatform.GetPDFKitMap ();
					break;
				}
			} else {
				// we get something in LibraryName from FieldAttribute so we asume
				// it is a path to a library, so we save the path and change library name
				// to a valid identifier if needed
				libraryPath = libraryName;
				// without extension makes more sense, but we can't change it since it breaks compat
				if (BindThirdPartyLibrary) {
					libraryName = Path.GetFileName (libraryName);
				} else {
					libraryName = Path.GetFileNameWithoutExtension (libraryName);
				}

				if (libraryName.Contains ('.'))
					libraryName = libraryName.Replace (".", string.Empty);
			}
		} else if (BindThirdPartyLibrary) {
			// User should provide a LibraryName
			libraryName = null;
			return false;
		} else {
			libraryName = typeNamespace;
		}

		if (!_libraries.ContainsKey (libraryName))
			_libraries.Add (libraryName, libraryPath);

		return true;
	}
}
