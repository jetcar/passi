using MauiApp2.Menu;

namespace MauiApp2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        private void Menu_button(object sender, EventArgs e)
        {
            // BaseContentPage._navigationService.PushModalSinglePage(new MenuView());
        }
    }
}