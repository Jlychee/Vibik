using System.Windows.Input;
using Core;
using Core.Application;
using Task = Shared.Models.Task;

namespace Vibik.Resources.Components;

public partial class TaskCard
{
    private static readonly BindableProperty TaskApiProperty =
        BindableProperty.Create(nameof(TaskApi), typeof(ITaskApi), typeof(TaskCard));

    private static readonly BindableProperty UserApiProperty =
        BindableProperty.Create(nameof(UserApi), typeof(IUserApi), typeof(TaskCard));

    public IUserApi UserApi
    {
        get => (IUserApi) GetValue(UserApiProperty);
        set => SetValue(UserApiProperty, value);
    }
    public ITaskApi? TaskApi
    {
        get => (ITaskApi?)GetValue(TaskApiProperty);
        set => SetValue(TaskApiProperty, value);
    }

    public TaskCard()
    {
        InitializeComponent();
        BindingContext = this;
    }
    
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(TaskCard));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public static readonly BindableProperty DaysPassedProperty =
        BindableProperty.Create(nameof(DaysPassed), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsTexts);
    public int DaysPassed { get => (int)GetValue(DaysPassedProperty); set => SetValue(DaysPassedProperty, value); }

    public static readonly BindableProperty CostProperty =
        BindableProperty.Create(nameof(Cost), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsColorAndTexts);
    public int Cost { get => (int)GetValue(CostProperty); set => SetValue(CostProperty, value); }

    public static readonly BindableProperty SwapCostProperty =
        BindableProperty.Create(nameof(SwapCost), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsColorAndTexts);
    public int SwapCost { get => (int)GetValue(SwapCostProperty); set => SetValue(SwapCostProperty, value); }

    public static readonly BindableProperty AvailableCoinsProperty =
        BindableProperty.Create(nameof(AvailableCoins), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsColorAndTexts);

    public int AvailableCoins
    {
        get => (int)GetValue(AvailableCoinsProperty);
        set => SetValue(AvailableCoinsProperty, value);
    }
    
    public static readonly BindableProperty CoinsColorProperty =
        BindableProperty.Create(nameof(CoinsColor), typeof(Color), typeof(TaskCard), Colors.Gray);
    public Color CoinsColor { get => (Color)GetValue(CoinsColorProperty); set => SetValue(CoinsColorProperty, value); }

    public static readonly BindableProperty RefreshCommandProperty =
        BindableProperty.Create(nameof(RefreshCommand), typeof(ICommand), typeof(TaskCard));
    public ICommand? RefreshCommand { get => (ICommand?)GetValue(RefreshCommandProperty); set => SetValue(RefreshCommandProperty, value); }

    public static readonly BindableProperty IconSourceProperty =
        BindableProperty.Create(nameof(IconSource), typeof(ImageSource), typeof(TaskCard), default(ImageSource));
    public ImageSource? IconSource { get => (ImageSource?)GetValue(IconSourceProperty); set => SetValue(IconSourceProperty, value); }

    public static readonly BindableProperty ItemProperty =
        BindableProperty.Create(nameof(Item), typeof(Task), typeof(TaskCard));
    public Task? Item { get => (Task?)GetValue(ItemProperty); set => SetValue(ItemProperty, value); }

    public string DaysPassedText => DaysPassed == 0 ? "со старта сегодня" : $"со старта прошло {DaysPassed} дн.";
    public string CostText => $"награда: {Cost}";
    public string AvailableCoinsText => $"монет: {AvailableCoins}";

    private static void OnAffectsTexts(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TaskCard c)
            c.OnPropertyChanged(nameof(DaysPassedText));
    }

    private static void OnAffectsColorAndTexts(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TaskCard c)
        {
            c.UpdateCoinsColor();
            c.OnPropertyChanged(nameof(CostText));
            c.OnPropertyChanged(nameof(AvailableCoinsText));
        }
    }

    private void UpdateCoinsColor()
    {
        var enoughMoney = AvailableCoins >= SwapCost;

        var ok  = GetColorFromResources("AccentGreen", Colors.White);
        var low = GetColorFromResources("NoMoneyRed",  Color.FromArgb("#80FFFFFF"));

        CoinsColor = enoughMoney ? ok : low;
    }

    private static Color GetColorFromResources(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c)
            return c;
        return fallback;
    }
    
    private async void OnCardTapped(object? sender, TappedEventArgs e)
    {
        var fromGesture = (sender as TapGestureRecognizer)?.CommandParameter as Task;
        var item = fromGesture ?? Item;
        if (item is null) return;
        await Navigation.PushAsync(new TaskDetailsPage(item));
    }
}
