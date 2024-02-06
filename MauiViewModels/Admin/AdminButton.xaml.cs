using System;
using System.Diagnostics;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MauiViewModels.Admin
{
    public class AdminButton
    {
        private INavigationService _navigationService;

        public AdminButton()
        {
            _navigationService = App.Services.GetService<INavigationService>();
            this.IsVisible = Debugger.IsAttached;
        }

        public bool IsVisible { get; set; }

        private void AdminButton_OnClicked(AccountDb account)
        {
            _navigationService.PushModalSinglePage((new AdminView(account) { }));
        }
    }
}