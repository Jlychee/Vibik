using Core.Interfaces;

namespace Vibik.Services;

public sealed class AuthNavigator(IServiceProvider serviceProvider) : IAuthNavigator
{
    public async Task RedirectToLoginAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Preferences.Remove("current_user");
            var app = Application.Current;
            if (app is null) return;

            var loginPage = serviceProvider.GetRequiredService<LoginPage>();
            app.MainPage = new NavigationPage(loginPage);
        });
    }
}
