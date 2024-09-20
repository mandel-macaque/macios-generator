namespace Xamarin.Macios.Generator.Parsers;

public sealed class BindingTypeData
{
    public string? Name { get; set; }
    public string[]? Events { get; set; }
    public string[]? Delegates { get; set; }
    public bool Singleton { get; set; }
    public string? KeepRefUntil { get; set; }
}