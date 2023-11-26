using System.Diagnostics;
using MauiApp2.StorageModels;
using MauiApp2.utils.Services;

namespace MauiApp2.Admin
{
    public partial class AdminButton : ContentView
    {
        INavigationService _navigationService;
        public AdminButton()
        {
            _navigationService = App.Services.GetService<INavigationService>();
            this.IsVisible = Debugger.IsAttached;
            if (!App.IsTest)
                InitializeComponent();
        }

        private void AdminButton_OnClicked(object sender, EventArgs e)
        {
            var button = ((Button)sender);
            var account = (AccountDb)button.BindingContext;
            _navigationService.PushModalSinglePage((new AdminView(account) { }));
        }
    }
}