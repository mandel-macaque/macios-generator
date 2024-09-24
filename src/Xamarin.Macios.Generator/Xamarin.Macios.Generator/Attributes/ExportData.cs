using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Xamarin.Macios.Generator.Attributes;

public class ExportData {
	public string Selector { get; set; } = string.Empty;
	public ArgumentSemantic Semantic { get; set; } = ArgumentSemantic.None;

	public static bool TryParse (SyntaxNode attributeSyntax, AttributeData attributeData,
		[NotNullWhen (true)] out ExportData? data)
	{
		data = default;

		var count = attributeData.ConstructorArguments.Length;
		switch (count) {
		case 1:
			data = new() { Selector = (string) attributeData.ConstructorArguments [0].Value! };
			return true;
		case 2:
			data = new() {
				Selector = (string) attributeData.ConstructorArguments [0].Value!,
				Semantic = (ArgumentSemantic) (int) attributeData.ConstructorArguments [1].Value!
			};
			return true;
		default:
			// do not know how to handle this constructor case
			return false;
		}
	}
}
