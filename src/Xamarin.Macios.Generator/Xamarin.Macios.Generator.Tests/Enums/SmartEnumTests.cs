using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Xamarin.Macios.Generator.Tests;

/// <summary>
///  Test all the field generation code.
/// </summary>
public class SmartEnumTests : BaseTestClass
{
	public class TestDataGenerator : BaseTestDataGenerator, IEnumerable<object[]>
	{
		readonly List<(string ClassName, string BindingFile, string OutputFile)> _data = new()
		{
			("AVCaptureDeviceTypeExtensions", "AVCaptureDeviceTypeEnum.txt", "ExpectedAVCaptureDeviceTypeEnum.txt" ),
			("AVCaptureSystemPressureLevelExtensions", "AVCaptureSystemPressureLevel.txt", "ExpectedAVCaptureSystemPressureLevel.txt" ),
			("UIFocusSoundIdentifierExtensions", "UIFocusSoundIdentifier.txt", "ExpectedUIFocusSoundIdentifier.txt" ),
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
    public void ExtensionGenerationTests (string className, string inputFileName, string inputText, string outputFileName, string expectedOutputText)
		=> CompareGeneratedCode(className, inputFileName, inputText, outputFileName, expectedOutputText);
}
