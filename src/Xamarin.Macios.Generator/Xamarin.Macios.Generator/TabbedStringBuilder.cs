using System;
using System.Text;

namespace Xamarin.Macios.Generator;

public class TabbedStringBuilder : IDisposable {
	StringBuilder _sb;
	readonly int _tabCount = 0;
	readonly string _tabs;
	readonly bool _isBlock = false;
	bool _disposed = false;

	public TabbedStringBuilder (StringBuilder sb, int tabCount = 0, bool isBlock = false)
	{
		_sb = sb;
		_tabs = new string ('\t', tabCount);
		_tabCount = tabCount;
		_isBlock = isBlock;
		if (_isBlock) {
			var braceTab = new string ('\t', _tabCount - 1);
			_sb.AppendLine ($"{braceTab}{{");
		}
	}

	public TabbedStringBuilder AppendLine ()
	{
		_sb.AppendLine ();
		return this;
	}

	public TabbedStringBuilder AppendLine (string line)
	{
		_sb.AppendLine ($"{_tabs}{line}");
		return this;
	}

	public TabbedStringBuilder AppendFormatLine (string format, params object [] args)
	{
		_sb.AppendFormat ($"{_tabs}{format}", args);
		_sb.AppendLine ();
		return this;
	}

	public TabbedStringBuilder AppendGeneraedCodeAttribute (bool optimizable = true)
	{
		if (optimizable) {
			const string attr = "[BindingImpl (BindingImplOptions.GeneratedCode | BindingImplOptions.Optimizable)]";
			AppendLine (attr);
		} else {
			const string attr = "[BindingImpl (BindingImplOptions.GeneratedCode)]";
			AppendLine (attr);
		}

		return this;
	}

	public TabbedStringBuilder AppendEditorBrowsableAttribute ()
	{
		const string attr = "[EditorBrowsable (EditorBrowsableState.Never)]";
		AppendLine (attr);
		return this;
	}

	public TabbedStringBuilder CreateBlock (bool isBlock) => CreateBlock (string.Empty, isBlock);

	public TabbedStringBuilder CreateBlock (string line, bool isBlock)
	{
		if (!string.IsNullOrEmpty (line)) {
			_sb.AppendLine ($"{_tabs}{line}");
		}

		return new TabbedStringBuilder (_sb, _tabCount + 1, isBlock);
	}

	public override string ToString ()
	{
		Dispose ();
		return _sb.ToString ();
	}

	public void Dispose ()
	{
		if (_disposed || !_isBlock) return;

		var braceTab = new string ('\t', _tabCount - 1);
		_disposed = true;
		_sb.AppendLine ($"{braceTab}}}");
	}
}
