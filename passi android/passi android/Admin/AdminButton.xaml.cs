using passi_android.utils;
using passi_android.utils.Services;
using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Admin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AdminButton : ContentView
    {
        private INavigationService _navigationService;

        public AdminButton()
        {
            _navigationService = App.Services.GetService<INavigationService>();
            this.IsVisible = Debugger.IsAttached;
            if (!App.IsTest)
                InitializeComponent();
        }

        private void AdminButton_OnClicked(object sender, EventArgs e)
        {
            var button = ((Xamarin.Forms.Button)sender);
            var account = (AccountDb)button.BindingContext;
            _navigationService.PushModalSinglePage((new AdminView(account) { }));
        }
    }
}