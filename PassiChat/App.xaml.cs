namespace PassiChat
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
        public static MainPage FirstPage { get; set; }

    }
}
