using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Xamarin.Macios.Generator.Extensions;

public static class InmutableArrayExtensions {
	public static string [] ToStringArray (this ImmutableArray<TypedConstant> self)
	{
		var array = new string[self.Length];
		for (var i = 0; i < self.Length; i++) {
			array [i] = (string) self [i].Value!;
		}

		return array;
	}
}
