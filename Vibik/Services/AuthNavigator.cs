using Core.Interfaces;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Vibik.Services;

public sealed class AuthNavigator(LoginPage loginPage) : IAuthNavigator
{
    public async Task RedirectToLoginAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Preferences.Remove("current_user");
            if (Application.Current is not null)
                Application.Current.MainPage = new NavigationPage(loginPage);
        });
    }
}
