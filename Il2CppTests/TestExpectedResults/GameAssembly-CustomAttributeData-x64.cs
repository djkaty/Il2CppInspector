// Image 0: mscorlib.dll - 0
// Image 1: CustomAttributeData.dll - 1810

// Namespace: Il2CppTests.TestSources
public enum ExampleKind // TypeDefIndex: 1811
{
	FirstKind = 0,
	SecondKind = 1,
	ThirdKind = 2,
	FourthKind = 3
}

// Namespace: Il2CppTests.TestSources
[AttributeUsage] // 0x000000018000C5B0
public class ExampleAttribute : Attribute // TypeDefIndex: 1812
{
	// Fields
	private ExampleKind kindValue; // 0x10
	private string noteValue; // 0x20
	private string[] arrayStrings; // 0x29
	private int[] arrayNumbers; // 0x8047D628

	// Properties
	public ExampleKind Kind { get; } // 0x00000001800ECD10 
	public string[] Strings { get; } // 0x00000001800EAEB0 
	public string Note { get; set; } // 0x00000001803E0EE0 0x00000001800EAEE0
	public int[] Numbers { get; set; } // 0x00000001803E0EF0 0x000000018015B760

	// Constructors
	public ExampleAttribute(ExampleKind initKind, string[] initStrings); // 0x00000001803E0E70
	public ExampleAttribute(ExampleKind initKind); // 0x00000001803E0EB0
	public ExampleAttribute(); // 0x00000001803E0E50

}

// Namespace: Il2CppTests.TestSources
[Example] // 0x000000018000C5D0
public class Test // TypeDefIndex: 1813
{
	// Constructors
	public Test(); // 0x00000001800E2000

	// Methods
	[Example] // 0x000000018000C710
	public void TestMethod([Example] /* 0x000000018000C750 */ object arg); // 0x00000001800EA7B0
}

