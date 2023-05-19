using System;
using System.Diagnostics;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Admin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AdminButton : ContentView
    {
        INavigationService Navigation;
        public AdminButton()
        {
            Navigation = App.Services.GetService<INavigationService>();
            this.IsVisible = Debugger.IsAttached;
            InitializeComponent();
        }

        private void AdminButton_OnClicked(object sender, EventArgs e)
        {
            var button = ((Xamarin.Forms.Button)sender);
            var account = (AccountDb)button.BindingContext;
            Navigation.PushModalSinglePage((new AdminPage(account) { }));
        }
    }
}