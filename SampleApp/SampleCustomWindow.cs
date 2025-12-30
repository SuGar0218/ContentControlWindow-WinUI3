using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using SuGarToolkit.WinUI3.Controls.Windows;

namespace SampleApp;

public sealed partial class SampleCustomWindow : ContentWindow
{
    public SampleCustomWindow()
    {
        DefaultStyleKey = typeof(SampleCustomWindow);
    }
}
