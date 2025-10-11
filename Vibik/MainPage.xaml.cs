using Vibik.Resources.Components;

namespace Vibik;

public partial class MainPage
{
    public MainPage()
    {
        InitializeComponent();
        GenerateTaskCard();
    }

    private void GenerateTaskCard()
    {
        var tasks = new[]
        {
            new TaskToDo("Медовые шесть", 0, 15, 5),
            new TaskToDo("Лесной сбор", 3, 20, 12),
            new TaskToDo("Тыквенный пряник", 7, 30, 35),
            new TaskToDo("Семена акации", 1, 10, 8),
        };

        foreach (var task in tasks)
        {
            var card = new TaskCard
            {
                Title = task.Title,
                DaysPassed = task.Days,
                Cost = task.Cost,
                SwapCost = task.SwapCost,
                RefreshCommand = new Command(() =>
                    DisplayAlert("Обновление", $"Обновить: {task.Title}", "OK"))

            };
            CardsHost.Children.Add(card);
        }
    }
    private record TaskToDo(string Title, int Days, int Cost, int SwapCost);
    
    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Map());
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopToRootAsync();
        }
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }
}