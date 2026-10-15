using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Bs2Cooler.App;

public static class UiMotion
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(UiMotion), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
    public static readonly DependencyProperty InteractiveProperty = DependencyProperty.RegisterAttached("Interactive", typeof(bool), typeof(UiMotion), new PropertyMetadata(false, OnInteractiveChanged));
    public static readonly DependencyProperty RevealProperty = DependencyProperty.RegisterAttached("Reveal", typeof(bool), typeof(UiMotion), new PropertyMetadata(false, OnRevealChanged));
    public static readonly DependencyProperty AnimateTextProperty = DependencyProperty.RegisterAttached("AnimateText", typeof(bool), typeof(UiMotion), new PropertyMetadata(false, OnAnimateTextChanged));
    public static readonly DependencyProperty AnimateSelectionProperty = DependencyProperty.RegisterAttached("AnimateSelection", typeof(bool), typeof(UiMotion), new PropertyMetadata(false, OnAnimateSelectionChanged));
    private static readonly DependencyPropertyDescriptor TextDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
    public static bool GetEnabled(DependencyObject value) => (bool)value.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject value, bool enabled) => value.SetValue(EnabledProperty, enabled);
    private static bool AllowsMotion(DependencyObject value) => GetEnabled(value) && SystemParameters.ClientAreaAnimation;
    public static bool GetInteractive(DependencyObject value) => (bool)value.GetValue(InteractiveProperty);
    public static void SetInteractive(DependencyObject value, bool enabled) => value.SetValue(InteractiveProperty, enabled);
    public static bool GetReveal(DependencyObject value) => (bool)value.GetValue(RevealProperty);
    public static void SetReveal(DependencyObject value, bool enabled) => value.SetValue(RevealProperty, enabled);
    public static bool GetAnimateText(DependencyObject value) => (bool)value.GetValue(AnimateTextProperty);
    public static void SetAnimateText(DependencyObject value, bool enabled) => value.SetValue(AnimateTextProperty, enabled);
    public static bool GetAnimateSelection(DependencyObject value) => (bool)value.GetValue(AnimateSelectionProperty);
    public static void SetAnimateSelection(DependencyObject value, bool enabled) => value.SetValue(AnimateSelectionProperty, enabled);

    private static void OnInteractiveChanged(DependencyObject value, DependencyPropertyChangedEventArgs e)
    {
        if (value is not Control control) return;
        if ((bool)e.NewValue)
        {
            control.MouseEnter += OnPointerEnter; control.MouseLeave += OnPointerLeave;
            control.PreviewMouseLeftButtonDown += OnPress; control.PreviewMouseLeftButtonUp += OnRelease;
            control.LostMouseCapture += OnLostCapture;
            control.LostKeyboardFocus += OnLostKeyboardFocus;
            control.IsEnabledChanged += OnEnabledChanged;
            control.PreviewKeyDown += OnKeyDown; control.PreviewKeyUp += OnKeyUp;
        }
        else
        {
            control.MouseEnter -= OnPointerEnter; control.MouseLeave -= OnPointerLeave;
            control.PreviewMouseLeftButtonDown -= OnPress; control.PreviewMouseLeftButtonUp -= OnRelease;
            control.LostMouseCapture -= OnLostCapture;
            control.LostKeyboardFocus -= OnLostKeyboardFocus;
            control.IsEnabledChanged -= OnEnabledChanged;
            control.PreviewKeyDown -= OnKeyDown; control.PreviewKeyUp -= OnKeyUp;
        }
    }
    private static void OnPointerEnter(object sender, MouseEventArgs e) => Hover((Control)sender, 0.07);
    private static void OnPointerLeave(object sender, MouseEventArgs e) { Hover((Control)sender, 0); Press((Control)sender, false); }
    private static void OnPress(object sender, MouseButtonEventArgs e) { Hover((Control)sender, 0.14); Press((Control)sender, true); }
    private static void OnRelease(object sender, MouseButtonEventArgs e) { Hover((Control)sender, 0.07); Press((Control)sender, false); }
    private static void OnLostCapture(object sender, MouseEventArgs e) => Press((Control)sender, false);
    private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => Press((Control)sender, false);
    private static void OnEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) { if (!(bool)e.NewValue) { Hover((Control)sender, 0); Press((Control)sender, false); } }
    private static void OnKeyDown(object sender, KeyEventArgs e) { if (e.Key is Key.Space or Key.Enter) Press((Control)sender, true); }
    private static void OnKeyUp(object sender, KeyEventArgs e) { if (e.Key is Key.Space or Key.Enter) Press((Control)sender, false); }
    private static void Hover(Control control, double opacity)
    {
        if ((control.IsEnabled || opacity == 0) && control.Template?.FindName("InteractionOverlay", control) is UIElement overlay)
            AnimateOpacity(overlay, opacity, 120);
    }
    private static void Press(Control control, bool pressed)
    {
        if (control is not ButtonBase || (pressed && !control.IsEnabled)) return;
        if (control.RenderTransform == Transform.Identity)
        {
            control.RenderTransform = new ScaleTransform(1, 1);
            control.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        if (control.RenderTransform is not ScaleTransform scale) return;
        double target = pressed && AllowsMotion(control) ? 0.975 : 1;
        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(AllowsMotion(control) ? 100 : 0)) { EasingFunction = new QuadraticEase() };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }
    private static void AnimateOpacity(UIElement element, double target, int milliseconds)
    {
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(AllowsMotion(element) ? milliseconds : 0)));
    }
    public static void Pulse(FrameworkElement element)
    {
        if (!element.IsVisible || !AllowsMotion(element)) return;
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.55, 1, TimeSpan.FromMilliseconds(220)) { FillBehavior = FillBehavior.Stop });
    }
    private static void Reveal(FrameworkElement element)
    {
        if (!element.IsVisible || !AllowsMotion(element)) return;
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { FillBehavior = FillBehavior.Stop });
        if (element.RenderTransform == Transform.Identity) element.RenderTransform = new TranslateTransform();
        if (element.RenderTransform is TranslateTransform translation)
            translation.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(5, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop });
    }
    private static void OnRevealChanged(DependencyObject value, DependencyPropertyChangedEventArgs e)
    {
        if (value is not FrameworkElement element) return;
        if ((bool)e.NewValue) element.Loaded += OnRevealLoaded; else element.Loaded -= OnRevealLoaded;
    }
    private static void OnRevealLoaded(object sender, RoutedEventArgs e) => Reveal((FrameworkElement)sender);
    private static void OnAnimateTextChanged(DependencyObject value, DependencyPropertyChangedEventArgs e)
    {
        if (value is not TextBlock text) return;
        if ((bool)e.NewValue) { text.Loaded += OnTextLoaded; text.Unloaded += OnTextUnloaded; }
        else { text.Loaded -= OnTextLoaded; text.Unloaded -= OnTextUnloaded; TextDescriptor.RemoveValueChanged(text, OnTextChanged); }
    }
    private static void OnTextLoaded(object sender, RoutedEventArgs e) { TextDescriptor.RemoveValueChanged(sender, OnTextChanged); TextDescriptor.AddValueChanged(sender, OnTextChanged); }
    private static void OnTextUnloaded(object sender, RoutedEventArgs e) => TextDescriptor.RemoveValueChanged(sender, OnTextChanged);
    private static void OnTextChanged(object? sender, EventArgs e) { if (sender is FrameworkElement element) Pulse(element); }
    private static void OnAnimateSelectionChanged(DependencyObject value, DependencyPropertyChangedEventArgs e)
    {
        if (value is not TabControl tabs) return;
        if ((bool)e.NewValue) tabs.SelectionChanged += OnTabChanged; else tabs.SelectionChanged -= OnTabChanged;
    }
    private static void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        var tabs = (TabControl)sender;
        if (ReferenceEquals(e.Source, tabs) && tabs.Template?.FindName("PART_SelectedContentHost", tabs) is FrameworkElement content) Reveal(content);
    }
}
