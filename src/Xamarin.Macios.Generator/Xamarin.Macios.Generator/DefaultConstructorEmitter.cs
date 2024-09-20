using System.Text;

namespace Xamarin.Macios.Generator;

public static class DefaultConstructorEmitter
{
    public static void RenderDefaultConstructor(TabbedStringBuilder classBlock, string className)
    {
        classBlock.AppendGeneraedCodeAttribute();
        classBlock.AppendEditorBrowsableAttribute();
        classBlock.AppendLine("[Export (\"init\")]");
        classBlock.AppendLine($"public {className} () : base (NSObjectFlag.Empty)");
        using (var body = classBlock.CreateBlock(isBlock: true))
        {
            using (var ifBlock = body.CreateBlock("if (IsDirectBinding)", isBlock: true))
            {
                ifBlock.AppendLine("InitializeHandle (global::ObjCRuntime.Messaging.IntPtr_objc_msgSend (this.Handle, global::ObjCRuntime.Selector.GetHandle (\"init\")), \"init\");");
            }
            using (var elseBlock = body.CreateBlock("else", isBlock: true))
            {
                elseBlock.AppendLine("InitializeHandle (global::ObjCRuntime.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, global::ObjCRuntime.Selector.GetHandle (\"init\")), \"init\");");
            }
        }
    }

    public static void RenderSkipInit (TabbedStringBuilder classBlock, string className)
    {
        classBlock.AppendGeneraedCodeAttribute();
        classBlock.AppendEditorBrowsableAttribute();
        classBlock.AppendLine($"protected {className} (NSObjectFlag t) : base (t)");
        using (var body = classBlock.CreateBlock(isBlock: true))
        {
            // empty body
        }
    }

    public static void RenderNativeHandlerConstructor (TabbedStringBuilder classBlock, string className)
    {
        classBlock.AppendGeneraedCodeAttribute();
        classBlock.AppendEditorBrowsableAttribute();
        classBlock.AppendLine($"protected internal {className} (NativeHandle handle) : base (handle)");
        using (var body = classBlock.CreateBlock(isBlock: true))
        {
            // empty body
        }
    }
}