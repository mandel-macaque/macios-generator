namespace Xamarin.Macios.Generator;

// TODO: this should come from the xamarin-macios project
public enum ArgumentSemantic : int {
    None = -1,
    Assign = 0,
    Copy = 1,
    Retain = 2,
    Weak = 3,
    Strong = Retain,
    UnsafeUnretained = Assign,
}