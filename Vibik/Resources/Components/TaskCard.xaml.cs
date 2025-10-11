using System.Windows.Input;

namespace Vibik.Resources.Components;

public partial class TaskCard
{
    private static int moneyOnTheAccount = 30; // #ЗАГЛУШКА: вытаскиваем количество монет пользователя из БД
    public TaskCard()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(TaskCard));
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    public static readonly BindableProperty DaysPassedProperty = BindableProperty.Create(
        nameof(DaysPassed), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsTexts);
    public int DaysPassed { get => (int)GetValue(DaysPassedProperty); set => SetValue(DaysPassedProperty, value); }

    public static readonly BindableProperty CostProperty = BindableProperty.Create(
        nameof(Cost), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsColorAndTexts);
    public int Cost { get => (int)GetValue(CostProperty); set => SetValue(CostProperty, value); }

    public static readonly BindableProperty AvailableCoinsProperty = BindableProperty.Create(
        nameof(SwapCost), typeof(int), typeof(TaskCard), 0, propertyChanged: OnAffectsColorAndTexts);
    public int SwapCost { get => (int)GetValue(AvailableCoinsProperty); set => SetValue(AvailableCoinsProperty, value); }

    public static readonly BindableProperty CoinsColorProperty = BindableProperty.Create(
        nameof(CoinsColor), typeof(Color), typeof(TaskCard), Colors.Gray);
    public Color CoinsColor { get => (Color)GetValue(CoinsColorProperty); set => SetValue(CoinsColorProperty, value); }

    public static readonly BindableProperty RefreshCommandProperty = BindableProperty.Create(
        nameof(RefreshCommand), typeof(ICommand), typeof(TaskCard));
    public ICommand? RefreshCommand { get => (ICommand?)GetValue(RefreshCommandProperty); set => SetValue(RefreshCommandProperty, value); }


    public string DaysPassedText => $"Со старта прошло: {DaysPassed} дн.";
    public string CostText => $"Стоимость: {Cost} м.";
    public string AvailableCoinsText => $"{SwapCost} м.";

    private static void OnAffectsTexts(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TaskCard c)
        {
            c.OnPropertyChanged(nameof(DaysPassedText));
        }
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
        var ratio = Cost <= moneyOnTheAccount; 

        var ok = GetColorFromResources("AccentGreen", Colors.Green);
        var low = GetColorFromResources("NoMoneyRed", Colors.Red);

        CoinsColor = ratio ? ok : low;
    }

    private static Color GetColorFromResources(string key, Color fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var v) == true && v is Color c)
            return c;
        return fallback;
    }
}