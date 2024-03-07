namespace MauiApp2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        public static MainView FirstPage { get; set; }
    }
}