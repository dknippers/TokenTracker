using OpenCodeCostMeter.Models;
using OpenCodeCostMeter.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace OpenCodeCostMeter;

public partial class MainWindow : Window
{
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(100);

    private WidgetSettings _settings = new();
    private readonly DispatcherTimer _hoverTimer;
    private bool _hoverShowPending;

    public MainWindow()
    {
        InitializeComponent();
        _hoverTimer = new DispatcherTimer { Interval = HoverDelay };
        _hoverTimer.Tick += (_, _) =>
        {
            _hoverTimer.Stop();
            if (_hoverShowPending)
                ToggleButton.Visibility = Visibility.Visible;
            else
                ToggleButton.Visibility = Visibility.Collapsed;
        };
    }

    public WidgetSettings Settings
    {
        get => _settings;
        set
        {
            _settings = value;
            ApplySettingsToVisuals();
        }
    }

    public event EventHandler? SettingsChanged;
    public WidgetViewModel ViewModel => (WidgetViewModel)DataContext;

    private void ApplySettingsToVisuals()
    {
        Topmost = _settings.AlwaysOnTop;
        Opacity = Math.Clamp(_settings.Opacity, 0.05, 1.0);
    }

    private void OnSettingsChanged()
    {
        ApplySettingsToVisuals();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCardMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
            e.Handled = true;
        }
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ForceNow();
    }

    private void OnCardMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var menu = BuildMenu();
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void OnCardMouseEnter(object sender, MouseEventArgs e)
    {
        _hoverTimer.Stop();
        _hoverShowPending = true;
        _hoverTimer.Start();
    }

    private void OnCardMouseLeave(object sender, MouseEventArgs e)
    {
        _hoverTimer.Stop();
        _hoverShowPending = false;
        _hoverTimer.Start();
    }

    private System.Windows.Controls.ContextMenu BuildMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        var onTop = new System.Windows.Controls.MenuItem
        {
            Header = "Always on top",
            IsCheckable = true,
            IsChecked = _settings.AlwaysOnTop
        };
        onTop.Click += (_, _) =>
        {
            _settings.AlwaysOnTop = onTop.IsChecked;
            Topmost = _settings.AlwaysOnTop;
            OnSettingsChanged();
        };
        menu.Items.Add(onTop);

        menu.Items.Add(MakeHeader("Poll interval"));
        var pollSliderItem = new System.Windows.Controls.MenuItem { IsHitTestVisible = true };
        var pollPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new System.Windows.Thickness(5) };
        var pollSlider = new System.Windows.Controls.Slider
        {
            Minimum = 5,
            Maximum = 60,
            Value = _settings.PollIntervalSeconds,
            TickFrequency = 5,
            IsSnapToTickEnabled = true,
            Width = 120,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        var pollLabel = new System.Windows.Controls.TextBlock
        {
            Text = $"{(int)_settings.PollIntervalSeconds}s",
            Width = 35,
            TextAlignment = System.Windows.TextAlignment.Right,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new System.Windows.Thickness(5, 0, 0, 0)
        };
        pollSlider.ValueChanged += (_, e) =>
        {
            _settings.PollIntervalSeconds = e.NewValue;
            ViewModel.SetInterval(e.NewValue);
            pollLabel.Text = $"{(int)e.NewValue}s";
            OnSettingsChanged();
        };
        pollPanel.Children.Add(pollSlider);
        pollPanel.Children.Add(pollLabel);
        pollSliderItem.Header = pollPanel;
        menu.Items.Add(pollSliderItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        menu.Items.Add(MakeHeader("Opacity"));
        var sliderItem = new System.Windows.Controls.MenuItem { IsHitTestVisible = true };
        var panel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new System.Windows.Thickness(5) };
        var slider = new System.Windows.Controls.Slider
        {
            Minimum = 5,
            Maximum = 100,
            Value = _settings.Opacity * 100,
            TickFrequency = 5,
            IsSnapToTickEnabled = true,
            Width = 120,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        var label = new System.Windows.Controls.TextBlock
        {
            Text = $"{(int)(_settings.Opacity * 100)}%",
            Width = 35,
            TextAlignment = System.Windows.TextAlignment.Right,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            Margin = new System.Windows.Thickness(5, 0, 0, 0)
        };
        slider.ValueChanged += (_, e) =>
        {
            var val = e.NewValue / 100.0;
            _settings.Opacity = val;
            Opacity = val;
            label.Text = $"{(int)e.NewValue}%";
            OnSettingsChanged();
        };
        panel.Children.Add(slider);
        panel.Children.Add(label);
        sliderItem.Header = panel;
        menu.Items.Add(sliderItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var quit = new System.Windows.Controls.MenuItem { Header = "Quit" };
        quit.Click += (_, _) => Close();
        menu.Items.Add(quit);

        return menu;
    }

    private static System.Windows.Controls.MenuItem MakeHeader(string text)
    {
        var item = new System.Windows.Controls.MenuItem
        {
            Header = text,
            IsEnabled = false,
            FontWeight = FontWeights.SemiBold
        };
        return item;
    }
}