using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Xamarin.Macios.Generator.Tests;

/// <summary>
///  Test all the field generation code.
/// </summary>
public class FieldTests : BaseTestClass
{
	public class TestDataGenerator : BaseTestDataGenerator, IEnumerable<object[]>
	{
		readonly List<(string ClassName, string BindingFile, string OutputFile)> _data = new()
		{
			("AVAssetTrackTrackAssociation", "NSStringStaticFieldBinding.txt", "ExpectedNSStringStaticFieldBinding.txt" ),
			("UIPasteboard", "NSArrayStaticFieldBinding.txt", "ExpectedNSArrayStaticFieldBinding.txt" ),
			("AVAssetTrackTrackAssociation", "NSNumberStaticFieldBinding.txt", "ExpectedNSNumberStaticFieldBinding.txt" ),
			("UTTypes", "UTTypeStaticFieldBinding.txt", "ExpectedUTTypeStaticFieldBinding.txt" ),
			("UICollectionViewFlowLayout", "CGSizeStaticFieldBinding.txt", "ExpectedCGSizeStaticFieldBinding.txt" ),
			// TODO: is this a thing in dotnet ? ("UISplitViewController", "NFloatStaticFieldBinding.txt", "ExpectedNFloatStaticFieldBinding.txt" ),
			("UISplitViewController", "FloatStaticFieldBinding.txt", "ExpectedFloatStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "NIntStaticFieldBinding.txt", "ExpectedNIntStaticFieldBinding.txt" ),
			("ARSCNDebugOptions", "NUintStaticFieldBinding.txt", "ExpectedNUintStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "ByteStaticFieldBinding.txt", "ExpectedByteStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "SByteStaticFieldBinding.txt", "ExpectedSByteStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "Int16StaticFieldBinding.txt", "ExpectedInt16StaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "UInt16StaticFieldBinding.txt", "ExpectedUInt16StaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "Int32StaticFieldBinding.txt", "ExpectedInt32StaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "UInt32StaticFieldBinding.txt", "ExpectedUInt32StaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "DoubleStaticFieldBinding.txt", "ExpectedDoubleStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "IntPtrStaticFieldBinding.txt", "ExpectedIntPtrStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "UIntPtrStaticFieldBinding.txt", "ExpectedUIntPtrStaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "Int64StaticFieldBinding.txt", "ExpectedInt64StaticFieldBinding.txt" ),
			("GCExtendedGamepadSnapshot", "UInt64StaticFieldBinding.txt", "ExpectedUInt64StaticFieldBinding.txt" ),
			("UISplitViewController", "SizeFStaticFieldBinding.txt", "ExpectedSizeFStaticFieldBinding.txt" ),
		};

		public IEnumerator<object[]> GetEnumerator()
		{
			foreach (var testData in _data)
			{
				yield return [
					testData.ClassName,
					testData.BindingFile,
					ReadFileAsString(testData.BindingFile),
					testData.OutputFile,
					ReadFileAsString(testData.OutputFile)];
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

    [Theory]
    [ClassData(typeof(TestDataGenerator))]
    public void ConstantFieldTests (string className, string inputFileName, string inputText, string outputFileName, string expectedOutputText)
		=> CompareGeneratedCode(className, inputFileName, inputText, outputFileName, expectedOutputText);
}
