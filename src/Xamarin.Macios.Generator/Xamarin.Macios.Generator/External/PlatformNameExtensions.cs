using System;
using System.IO;

namespace Xamarin.Macios.Generator.External;

public static class PlatformNameExtensions {

	public static string? GetApplicationClassName (this PlatformName currentPlatform)
	{
		switch (currentPlatform) {
		case PlatformName.iOS:
		case PlatformName.WatchOS:
		case PlatformName.TvOS:
		case PlatformName.MacCatalyst:
			return "UIApplication";
		case PlatformName.MacOSX:
			return "NSApplication";
		default:
			return null;
		}
	}

	public static string? GetCoreImageMap (this PlatformName currentPlatform)
	{
		switch (currentPlatform) {
		case PlatformName.iOS:
		case PlatformName.WatchOS:
		case PlatformName.TvOS:
		case PlatformName.MacCatalyst:
			return "CoreImage";
		case PlatformName.MacOSX:
			return "Quartz";
		default:
			return null;
		}
	}

	public static string? GetCoreServicesMap (this PlatformName currentPlatform)
	{
		switch (currentPlatform) {
		case PlatformName.iOS:
		case PlatformName.WatchOS:
		case PlatformName.TvOS:
		case PlatformName.MacCatalyst:
			return "MobileCoreServices";
		case PlatformName.MacOSX:
			return "CoreServices";
		default:
			return null;
		}
	}

	public static string? GetPDFKitMap (this PlatformName currentPlatform)
	{
		switch (currentPlatform) {
		case PlatformName.iOS:
		case PlatformName.MacCatalyst:
			return "PDFKit";
		case PlatformName.MacOSX:
			return "Quartz";
		default:
			return null;
		}
	}

	static string? GetSdkRoot (this PlatformName currentPlatform)
	{
		switch (currentPlatform) {
		case PlatformName.iOS:
		case PlatformName.WatchOS:
		case PlatformName.TvOS:
		case PlatformName.MacCatalyst:
			return "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current";
		case PlatformName.MacOSX:
			return "/Library/Frameworks/Xamarin.Mac.framework/Versions/Current";
		default:
			return null;
		}
	}

	public static string? GetPath (this PlatformName currentPlatform, params string [] paths)
	{
		var fullPaths = new string [paths.Length + 1];
		fullPaths [0] = currentPlatform.GetSdkRoot ();
		Array.Copy (paths, 0, fullPaths, 1, paths.Length);
		return Path.Combine (fullPaths);
	}
}
