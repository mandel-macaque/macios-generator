using System;
using Microsoft.CodeAnalysis;

namespace Xamarin.Macios.Generator.Extensions;

public static class SpecialTypeExtensions {

	public static string GetDlfcnGetMethod (this SpecialType self) => self switch {
		SpecialType.System_Byte => "GetByte",
		SpecialType.System_SByte => "GetSByte",
		SpecialType.System_Int16 => "GetInt16",
		SpecialType.System_UInt16 => "GetUInt16",
		SpecialType.System_Int32 => "GetInt32",
		SpecialType.System_UInt32 => "GetUInt32",
		SpecialType.System_Double => "GetDouble",
		SpecialType.System_IntPtr => "GetIntPtr",
		SpecialType.System_UIntPtr => "GetUIntPtr",
		SpecialType.System_Int64 => "GetInt64",
		SpecialType.System_UInt64 => "GetUInt64",
		_ => null
	} ?? throw new InvalidOperationException();

	public static string GetDlfcnSetMethod (this SpecialType self) => self switch {
		SpecialType.System_Byte => "SetByte",
		SpecialType.System_SByte => "SetSByte",
		SpecialType.System_Int16 => "SetInt16",
		SpecialType.System_UInt16 => "SetUInt16",
		SpecialType.System_Int32 => "SetInt32",
		SpecialType.System_UInt32 => "SetUInt32",
		SpecialType.System_Double => "SetDouble",
		SpecialType.System_IntPtr => "SetIntPtr",
		SpecialType.System_UIntPtr => "SetUIntPtr",
		SpecialType.System_Int64 => "SetInt64",
		SpecialType.System_UInt64 => "SetUInt64",
		_ => null
	} ?? throw new InvalidOperationException();
}
