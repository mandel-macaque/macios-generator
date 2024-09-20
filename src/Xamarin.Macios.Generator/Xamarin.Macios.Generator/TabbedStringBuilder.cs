using System;
using System.Text;

namespace Xamarin.Macios.Generator;

public class TabbedStringBuilder : IDisposable
{
    private StringBuilder _sb;
    private int _tabCount = 0;
    private string? _tabs = string.Empty;
    private bool _isBlock = false;
    private bool _disposed = false;
    public TabbedStringBuilder (StringBuilder sb, int tabCount = 0, bool isBlock = false)
    {
        _sb = sb;
        _tabs = new string('\t', tabCount);
        _tabCount = tabCount;
        _isBlock = isBlock;
        if (_isBlock)
        {
            var braceTab = new string('\t', _tabCount - 1);
            _sb.AppendLine($"{braceTab}{{");
        }
    }

    public TabbedStringBuilder AppendLine()
    {
         _sb.AppendLine();
         return this;
    }
    public TabbedStringBuilder AppendLine(string line)
    {
        _sb.AppendLine($"{_tabs}{line}");
        return this;
    }

    public TabbedStringBuilder AppendFormatLine(string format, params object[] args)
    {
        _sb.AppendFormat(format, args);
        _sb.AppendLine();
        return this;
    }

    public TabbedStringBuilder AppendGeneraedCodeAttribute()
    {
        const string attr = "[BindingImpl (BindingImplOptions.GeneratedCode | BindingImplOptions.Optimizable)]";
        AppendLine(attr);
        return this;
    }

    public TabbedStringBuilder AppendEditorBrowsableAttribute()
    {
        const string attr = "[EditorBrowsable (EditorBrowsableState.Never)]";
        AppendLine(attr);
        return this;
    }

    public TabbedStringBuilder CreateBlock(bool isBlock) => CreateBlock(string.Empty, isBlock);
    public TabbedStringBuilder CreateBlock(string line, bool isBlock)
    {
        if (!string.IsNullOrEmpty(line))
        {
            _sb.AppendLine($"{_tabs}{line}");
        }
        return new TabbedStringBuilder(_sb, _tabCount + 1, isBlock);
    }

    public override string ToString()
    {
        Dispose();
        return _sb.ToString();
    }

    public void Dispose()
    {
        if (_disposed || !_isBlock) return;

        var braceTab = new string('\t', _tabCount - 1);
        _disposed = true;
        _sb.AppendLine($"{braceTab}}}");
    }
}