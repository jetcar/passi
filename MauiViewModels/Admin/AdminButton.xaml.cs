using System.Diagnostics;
using MauiCommonServices;
using MauiViewModels.StorageModels;
using Microsoft.Extensions.DependencyInjection;

namespace MauiViewModels.Admin
{
    public class AdminButton
    {
        private INavigationService _navigationService;

        public AdminButton()
        {
            _navigationService = CommonApp.Services.GetService<INavigationService>();
            this.IsVisible = Debugger.IsAttached;
        }

        public bool IsVisible { get; set; }

        private void AdminButton_OnClicked(AccountDb account)
        {
            _navigationService.PushModalSinglePage((new AdminView(account) { }));
        }
    }
}