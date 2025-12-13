using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

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
