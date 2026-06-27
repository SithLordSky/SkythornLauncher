using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SkythornLauncher.Controls;

public sealed class MenuButton : Button
{
    public static readonly DependencyProperty NormalImageSourceProperty =
        DependencyProperty.Register(
            nameof(NormalImageSource),
            typeof(ImageSource),
            typeof(MenuButton),
            new PropertyMetadata(null, OnImageSourceChanged));

    public static readonly DependencyProperty HoverImageSourceProperty =
        DependencyProperty.Register(
            nameof(HoverImageSource),
            typeof(ImageSource),
            typeof(MenuButton),
            new PropertyMetadata(null, OnImageSourceChanged));

    public static readonly DependencyProperty NormalTopOffsetProperty =
        DependencyProperty.Register(nameof(NormalTopOffset), typeof(double), typeof(MenuButton), new PropertyMetadata(0.0));

    public static readonly DependencyProperty NormalHeightProperty =
        DependencyProperty.Register(nameof(NormalHeight), typeof(double), typeof(MenuButton), new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty HoverTopOffsetProperty =
        DependencyProperty.Register(nameof(HoverTopOffset), typeof(double), typeof(MenuButton), new PropertyMetadata(0.0));

    public static readonly DependencyProperty HoverHeightProperty =
        DependencyProperty.Register(nameof(HoverHeight), typeof(double), typeof(MenuButton), new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty HoverLeftOffsetProperty =
        DependencyProperty.Register(nameof(HoverLeftOffset), typeof(double), typeof(MenuButton), new PropertyMetadata(0.0));

    public static readonly DependencyProperty HoverWidthProperty =
        DependencyProperty.Register(nameof(HoverWidth), typeof(double), typeof(MenuButton), new PropertyMetadata(double.NaN));

    private Image? _normalImage;
    private Image? _hoverImage;

    public MenuButton()
    {
        Background = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        FocusVisualStyle = null;
        Opacity = 0.01;
        Template = CreateTemplate();
        Loaded += MenuButton_Loaded;
    }

    public ImageSource? NormalImageSource
    {
        get => (ImageSource?)GetValue(NormalImageSourceProperty);
        set => SetValue(NormalImageSourceProperty, value);
    }

    public ImageSource? HoverImageSource
    {
        get => (ImageSource?)GetValue(HoverImageSourceProperty);
        set => SetValue(HoverImageSourceProperty, value);
    }

    public double NormalTopOffset
    {
        get => (double)GetValue(NormalTopOffsetProperty);
        set => SetValue(NormalTopOffsetProperty, value);
    }

    public double NormalHeight
    {
        get => (double)GetValue(NormalHeightProperty);
        set => SetValue(NormalHeightProperty, value);
    }

    public double HoverTopOffset
    {
        get => (double)GetValue(HoverTopOffsetProperty);
        set => SetValue(HoverTopOffsetProperty, value);
    }

    public double HoverHeight
    {
        get => (double)GetValue(HoverHeightProperty);
        set => SetValue(HoverHeightProperty, value);
    }

    public double HoverLeftOffset
    {
        get => (double)GetValue(HoverLeftOffsetProperty);
        set => SetValue(HoverLeftOffsetProperty, value);
    }

    public double HoverWidth
    {
        get => (double)GetValue(HoverWidthProperty);
        set => SetValue(HoverWidthProperty, value);
    }

    private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MenuButton button)
        {
            return;
        }

        if (e.Property == NormalImageSourceProperty && button._normalImage != null)
        {
            button._normalImage.Source = e.NewValue as ImageSource;
        }
        else if (e.Property == HoverImageSourceProperty && button._hoverImage != null)
        {
            button._hoverImage.Source = e.NewValue as ImageSource;
        }
    }

    private static ControlTemplate CreateTemplate()
    {
        var template = new ControlTemplate(typeof(MenuButton));
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        template.VisualTree = borderFactory;
        return template;
    }

    private void MenuButton_Loaded(object sender, RoutedEventArgs e)
    {
        if (_normalImage != null || NormalImageSource == null || Parent is not Canvas canvas)
        {
            return;
        }

        var left = Canvas.GetLeft(this);
        if (double.IsNaN(left))
        {
            left = 0;
        }

        var normalHeight = double.IsNaN(NormalHeight) ? Height : NormalHeight;
        var hoverHeight = double.IsNaN(HoverHeight) ? Height : HoverHeight;
        var hoverWidth = double.IsNaN(HoverWidth) ? Width : HoverWidth;

        _normalImage = CreateArtImage(NormalImageSource, Width, normalHeight);
        _hoverImage = CreateArtImage(HoverImageSource, hoverWidth, hoverHeight);
        _hoverImage.Visibility = Visibility.Collapsed;

        var artCanvas = new Canvas
        {
            Width = Width,
            Height = Height,
            IsHitTestVisible = false
        };

        artCanvas.Children.Add(_normalImage);
        artCanvas.Children.Add(_hoverImage);
        Canvas.SetLeft(_normalImage, 0);
        Canvas.SetTop(_normalImage, NormalTopOffset);
        Canvas.SetLeft(_hoverImage, HoverLeftOffset);
        Canvas.SetTop(_hoverImage, HoverTopOffset);

        canvas.Children.Insert(canvas.Children.IndexOf(this), artCanvas);
        Canvas.SetLeft(artCanvas, left);
        Canvas.SetTop(artCanvas, Canvas.GetTop(this));
        Panel.SetZIndex(artCanvas, LayoutMetrics.MenuButtonArtZIndex);
    }

    private static Image CreateArtImage(ImageSource? source, double width, double height)
    {
        var image = new Image
        {
            Source = source,
            Stretch = Stretch.Fill,
            Width = width,
            Height = height,
            IsHitTestVisible = false
        };

        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
        return image;
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (_normalImage != null)
        {
            _normalImage.Visibility = Visibility.Collapsed;
        }

        if (_hoverImage != null)
        {
            _hoverImage.Visibility = Visibility.Visible;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_normalImage != null)
        {
            _normalImage.Visibility = Visibility.Visible;
        }

        if (_hoverImage != null)
        {
            _hoverImage.Visibility = Visibility.Collapsed;
        }
    }
}
