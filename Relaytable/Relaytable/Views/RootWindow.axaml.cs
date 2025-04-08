using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Windowing;
using Relaytable.Views;
using System;
using System.Linq;

namespace Relaytable;

public partial class RootWindow : AppWindow
{
	public RootWindow()
	{
		InitializeComponent();
		TitleBar.ExtendsContentIntoTitleBar = true;
		TitleBar.TitleBarHitTestType = TitleBarHitTestType.Complex;

#if DEBUG
		this.AttachDevTools();
#endif
	}

	protected override void OnClosed(EventArgs e)
	{
		if (Content is MainWindow mainWindow)
		{
			mainWindow.Close(e);
		}
		base.OnClosed(e);
	}
}