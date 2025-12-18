using System.Diagnostics;
using Vibik.Utils;
using Vibik.Utils.Image;

namespace Vibik.Resources.Components;

public partial class PhotoTile
{
    private static readonly BindableProperty PathOrUrlProperty =
        BindableProperty.Create(nameof(PathOrUrl), typeof(string), typeof(PhotoTile)
            , propertyChanged: OnPathChanged);

    private static readonly BindableProperty IsAddTileProperty =
        BindableProperty.Create(nameof(IsAddTile), typeof(bool), typeof(PhotoTile),
            false, propertyChanged: OnIsAddChanged);

    private static readonly BindableProperty TileSizeProperty =
        BindableProperty.Create(nameof(TileSize), typeof(double), typeof(PhotoTile), 120d);

    public string? PathOrUrl
    {
        get => (string?)GetValue(PathOrUrlProperty);
        init => SetValue(PathOrUrlProperty, value);
    }

    public bool IsAddTile
    {
        get => (bool)GetValue(IsAddTileProperty);
        set => SetValue(IsAddTileProperty, value);
    }

    public double TileSize
    {
        get => (double)GetValue(TileSizeProperty);
        init => SetValue(TileSizeProperty, value);
    }

    public event EventHandler<string?>? PhotoTapped;
    public event EventHandler? AddTapped;

    public PhotoTile()
    {
        InitializeComponent();

        RootFrame.SetBinding(WidthRequestProperty, new Binding(nameof(TileSize), source: this));
        RootFrame.SetBinding(HeightRequestProperty, new Binding(nameof(TileSize), source: this));
        ImageView.SetBinding(WidthRequestProperty, new Binding(nameof(TileSize), source: this));
        ImageView.SetBinding(HeightRequestProperty, new Binding(nameof(TileSize), source: this));
    }
    
    private static void OnIsAddChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhotoTile tile) return;
        var isAdd = (bool)newValue;
        tile.ImageRoot.IsVisible = !isAdd;
        tile.AddRoot.IsVisible = isAdd;
        Debug.Assert(Application.Current != null);
        tile.RootFrame.BorderColor = isAdd ? (Color)Application.Current.Resources["MilkChocolate"] : Colors.Transparent;
    }

    private static void OnPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PhotoTile tile) return;

        var path = newValue as string;
        if (!string.IsNullOrWhiteSpace(path))
        {
            tile.IsAddTile = false;
            tile.ImageRoot.IsVisible = true;
            tile.AddRoot.IsVisible = false;
            tile.RootFrame.BorderColor = Colors.Transparent;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            tile.ImageView.Source = ImageSourceFinder.ResolveImage(path);
            tile.ImageView.Opacity = 1;
            tile.ImageView.InvalidateMeasure();
            tile.RootFrame.InvalidateMeasure();
        });
    }

    private void OnTapped(object? sender, TappedEventArgs e)
    {
        if (IsAddTile)
            AddTapped?.Invoke(this, EventArgs.Empty);
        else
            PhotoTapped?.Invoke(this, PathOrUrl);
    }
}
