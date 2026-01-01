using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.ComponentModel;

using Windows.Foundation;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SuGarToolkit.WinUI3.Controls.Windows;

public partial class ContentWindow : ContentControl
{
    public ContentWindow() : this(new Window()) { }

    public ContentWindow(Window window)
    {
        DefaultStyleKey = typeof(ContentWindow);
        _window = window;
        _hwnd = new HWND(Win32Interop.GetWindowFromWindowId(_window.AppWindow.Id));
        _styleHelper = new WindowStyleHelper(_hwnd);
        _subclassProcHelper = new WindowSubclassProcHelper(_hwnd);
        _baseSubclassProc = BaseSubclassProc;
        PInvoke.SetWindowSubclass(_hwnd, _baseSubclassProc, 314159, 0);
        Window.AppWindow.Changed += OnAppWindowStateChanged;
        Window.AppWindow.Closing += OnAppWindowClosing;
        Window.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        Window.Activated += OnWindowActivated;
        Window.Closed += OnWindowClosed;
        Window.Content = this;
        Loaded += OnLoaded;
        RegisterPropertyChangedCallback(RequiresPointerProperty, OnRequestedThemeChanged);
    }

    private readonly Window _window;
    private readonly HWND _hwnd;
    private readonly WindowStyleHelper _styleHelper;
    private readonly WindowSubclassProcHelper _subclassProcHelper;
    private readonly SUBCLASSPROC _baseSubclassProc;

    #region DependencyProperty

    public bool CanMinimize
    {
        get => (bool) GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    public static readonly DependencyProperty CanMinimizeProperty = DependencyProperty.Register(
        nameof(CanMinimize),
        typeof(bool),
        typeof(ContentWindow),
        new PropertyMetadata(true, OnCanMinimizeChanged)
    );

    public bool CanMaximize
    {
        get => (bool) GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }

    public static readonly DependencyProperty CanMaximizeProperty = DependencyProperty.Register(
        nameof(CanMaximize),
        typeof(bool),
        typeof(ContentWindow),
        new PropertyMetadata(true, OnCanMaximizeChanged)
    );

    public bool CanResize
    {
        get => (bool) GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(
        nameof(CanResize),
        typeof(bool),
        typeof(ContentWindow),
        new PropertyMetadata(true, OnCanResizeChanged)
    );

    public new double Width
    {
        get => (double) GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public static new readonly DependencyProperty WidthProperty = DependencyProperty.Register(
        nameof(Width),
        typeof(double),
        typeof(ContentWindow),
        new PropertyMetadata(double.NaN, OnWidthChanged)
    );
    
    public new double Height
    {
        get => (double) GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public static new readonly DependencyProperty HeightProperty = DependencyProperty.Register(
        nameof(Height),
        typeof(double),
        typeof(ContentWindow),
        new PropertyMetadata(double.NaN, OnHeightChanged)
    );

    public bool SizeToContent
    {
        get => (bool) GetValue(SizeToContentProperty);
        set => SetValue(SizeToContentProperty, value);
    }

    public static readonly DependencyProperty SizeToContentProperty = DependencyProperty.Register(
        nameof(SizeToContent),
        typeof(bool),
        typeof(ContentWindow),
        new PropertyMetadata(default(bool))
    );

    #endregion

    public event EventHandler? StateChanged;
    public event EventHandler? Activated;
    public event EventHandler? Deactivated;
    public event EventHandler? Closed;
    public event CancelEventHandler? Closing;

    public Window Window => _window;

    public WindowState WindowState
    {
        get => field;
        private set
        {
            if (field == value)
                return;

            field = value;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string Title
    {
        get => Window.Title;
        set => Window.Title = value;
    }

    public SystemBackdrop? SystemBackdrop
    {
        get => Window.SystemBackdrop;
        set => Window.SystemBackdrop = value;
    }

    public bool ExtendsContentIntoTitleBar
    {
        get => Window.ExtendsContentIntoTitleBar;
        set => Window.ExtendsContentIntoTitleBar = value;
    }

    public TitleBarHeightOption TitleBarHeightOption
    {
        get => Window.AppWindow.TitleBar.PreferredHeightOption;
        set => Window.AppWindow.TitleBar.PreferredHeightOption = value;
    }

    public Window? Owner
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            nint ownerHwnd = Owner is null ? nint.Zero : Win32Interop.GetWindowFromWindowId(Owner.AppWindow.Id);
            PInvoke.SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWLP_HWNDPARENT, new HWND(ownerHwnd));
        }
    }

    public static Window? GetWindow(UIElement element) => (element.XamlRoot.Content as ContentWindow)?.Window;

    private bool _shouldShow;

    public void Show()
    {
        _shouldShow = true;
        if (IsLoaded)
        {
            Window.AppWindow.Show();
        }
    }

    public void Hide()
    {
        Window.AppWindow.Hide();
    }

    public void Activate()
    {
        Window.Activate();
    }

    public bool TryMinimize()
    {
        CancelEventArgs e = new(false);
        OnMaximizing(e);
        if (e.Cancel)
            return false;
        (Window.AppWindow.Presenter as OverlappedPresenter)?.Maximize();
        return true;
    }

    public bool TryMaximize()
    {
        CancelEventArgs e = new(false);
        OnMaximizing(e);
        if (e.Cancel)
            return false;
        (Window.AppWindow.Presenter as OverlappedPresenter)?.Minimize();
        return true;
    }

    public bool TryRestore()
    {
        CancelEventArgs e = new(false);
        OnMaximizing(e);
        if (e.Cancel)
            return false;
        (Window.AppWindow.Presenter as OverlappedPresenter)?.Restore();
        return true;
    }

    public bool TryClose()
    {
        CancelEventArgs e = new(false);
        OnMaximizing(e);
        if (e.Cancel)
            return false;
        Window.AppWindow.Hide();
        Window.Close();
        return true;
    }

    public void Resize(Size size)
    {
        double dpiScale = (double) PInvoke.GetDpiForWindow(_hwnd) / 96;
        if (Window.ExtendsContentIntoTitleBar)
        {
            Window.AppWindow.ResizeClient(new SizeInt32
            (
                _Width: IsValidLength(size.Width) ? (int) Math.Ceiling(size.Width * dpiScale) : Window.AppWindow.ClientSize.Width,
                _Height: IsValidLength(size.Height) ? (int) Math.Ceiling((size.Height - 30) * dpiScale) : Window.AppWindow.ClientSize.Height
            ));
        }
        else
        {
            Window.AppWindow.ResizeClient(new SizeInt32
            (
                _Width: IsValidLength(size.Width) ? (int) Math.Ceiling(size.Width * dpiScale) : Window.AppWindow.ClientSize.Width,
                _Height: IsValidLength(size.Height) ? (int) Math.Ceiling(size.Height * dpiScale) : Window.AppWindow.ClientSize.Height
            ));
        }
    }

    public void ResizeToContent()
    {
        Resize(DesiredSize);
    }

    public void AddSubclassProc(WindowSubclassProc proc)
    {
        _subclassProcHelper.AddSubclassProc(proc);
    }

    public void RemoveSubclassProc(WindowSubclassProc proc)
    {
        _subclassProcHelper.RemoveSubclassProc(proc);
    }

    protected virtual void OnMinimizing(CancelEventArgs e)
    {
        if (!CanMinimize)
        {
            e.Cancel = true;
            return;
        }
    }

    protected virtual void OnMaximizing(CancelEventArgs e)
    {
        if (!CanMaximize)
        {
            e.Cancel = true;
            return;
        }
    }

    protected virtual void OnRestoring(CancelEventArgs e)
    {
    }

    protected virtual void OnClosing(CancelEventArgs e)
    {
        Closing?.Invoke(this, e);
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        sender.Hide();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (SizeToContent)
        {
            ResizeToContent();
        }
        if (_shouldShow)
        {
            Show();
        }
    }

    private static bool IsValidLength(double length) => double.IsNormal(length) && double.IsPositive(length);

    private static void OnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ContentWindow self = (ContentWindow) d;
        double newWidth = (double) e.NewValue;
        self.Resize(new Size(newWidth, self.Height));
    }

    private static void OnHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ContentWindow self = (ContentWindow) d;
        double newHeight = (double) e.NewValue;
        self.Resize(new Size(self.Width, newHeight));
    }

    private static void OnCanMinimizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ContentWindow self = (ContentWindow) d;
        bool canMinimize = (bool) e.NewValue;
        self._styleHelper.CanMinimize = canMinimize;
        (self.Window.AppWindow.Presenter as OverlappedPresenter)?.IsMinimizable = canMinimize;
    }

    private static void OnCanMaximizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ContentWindow self = (ContentWindow) d;
        bool canMaximize = (bool) e.NewValue;
        self._styleHelper.CanMaximize = canMaximize;
        (self.Window.AppWindow.Presenter as OverlappedPresenter)?.IsMaximizable = canMaximize;
    }

    private static void OnCanResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ContentWindow self = (ContentWindow) d;
        bool canResize = (bool) e.NewValue;
        (self.Window.AppWindow.Presenter as OverlappedPresenter)?.IsResizable = canResize;
    }

    private void OnAppWindowStateChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            if (sender.Presenter is OverlappedPresenter overlappedPresenter)
            {
                WindowState = overlappedPresenter.State switch
                {
                    OverlappedPresenterState.Minimized => WindowState.Minimized,
                    OverlappedPresenterState.Maximized => WindowState.Maximized,
                    OverlappedPresenterState.Restored => WindowState.Normal,
                    _ => throw new InvalidOperationException(nameof(OverlappedPresenterState))
                };
            }
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        switch (args.WindowActivationState)
        {
            case WindowActivationState.Deactivated:
                Deactivated?.Invoke(this, EventArgs.Empty);
                break;

            case WindowActivationState.CodeActivated:
            case WindowActivationState.PointerActivated:
                Activated?.Invoke(this, EventArgs.Empty);
                break;

            default:
                break;
        }
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void OnRequestedThemeChanged(DependencyObject d, DependencyProperty p)
    {
        ElementTheme theme = (ElementTheme) d.GetValue(p);
        Window.AppWindow.TitleBar.PreferredTheme = theme switch
        {
            ElementTheme.Default => TitleBarTheme.UseDefaultAppMode,
            ElementTheme.Light => TitleBarTheme.Light,
            ElementTheme.Dark => TitleBarTheme.Dark,
            _ => TitleBarTheme.UseDefaultAppMode
        };
    }

    private LRESULT BaseSubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        CancelEventArgs e;
        switch (uMsg)
        {
            case WindowMessages.WM_SYSCOMMAND:
                switch ((wParam & 0xFFF0))
                {
                    case SYS_COMMAND_WPARAM.SC_MINIMIZE:
                        e = new CancelEventArgs(false);
                        OnMinimizing(e);
                        if (e.Cancel)
                            return new LRESULT(0);
                        break;

                    case SYS_COMMAND_WPARAM.SC_MAXIMIZE:
                        e = new CancelEventArgs(false);
                        OnMaximizing(e);
                        if (e.Cancel)
                            return new LRESULT(0);
                        break;

                    case SYS_COMMAND_WPARAM.SC_RESTORE:
                        e = new CancelEventArgs(false);
                        OnRestoring(e);
                        if (e.Cancel)
                            return new LRESULT(0);
                        break;

                    case SYS_COMMAND_WPARAM.SC_CLOSE:
                        e = new CancelEventArgs(false);
                        OnClosing(e);
                        if (e.Cancel)
                            return new LRESULT(0);
                        WindowState = WindowState.Closed;
                        break;

                    default:
                        break;
                }
                break;

            default:
                break;
        }
        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }
}
