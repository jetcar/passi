using System.Diagnostics;
using passi_maui.utils;

namespace passi_maui.Admin
{
    public partial class AdminButton : ContentView
    {
        public AdminButton()
        {
            this.IsVisible = Debugger.IsAttached;
            InitializeComponent();
        }

        private void AdminButton_OnClicked(object sender, EventArgs e)
        {
            var button = ((Button)sender);
            var account = (AccountDb)button.BindingContext;
            Navigation.PushModalSinglePage((new AdminPage(account) { }));
        }
    }
}