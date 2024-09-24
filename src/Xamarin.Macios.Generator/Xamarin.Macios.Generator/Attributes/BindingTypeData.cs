using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Xamarin.Macios.Generator.Extensions;

namespace Xamarin.Macios.Generator.Attributes;

public sealed class BindingTypeData {
	public string? Name { get; set; }
	public string []? Events { get; set; }
	public string []? Delegates { get; set; }
	public bool Singleton { get; set; }
	public string? KeepRefUntil { get; set; }

	public static bool TryParse (SyntaxNode attributeSyntax, AttributeData attributeData,
		[NotNullWhen (true)] out BindingTypeData? data)
	{
		data = new BindingTypeData ();
		// the BindingTypeAttribute only contains named arguments.
		foreach (var (name, value) in attributeData.NamedArguments) {
			switch (name) {
			case "Name":
				data.Name = (string) value.Value!;
				break;
			case "Events":
				// this is an array of typed constants
				break;
			case "Delegates":
				data.Delegates = value.Values.ToStringArray ();
				break;
			case "Singleton":
				data.Singleton = (bool) value.Value!;
				break;
			case "KeepRefUntil":
				data.KeepRefUntil = (string) value.Value!;
				break;
			default:
				data = null;
				return false;
			}
		}

		return true;
	}
}
