using ChatViewModel;

namespace MauiApp1
{
    public partial class MainPage : BaseContentPage
    {

        private readonly MainView _bindingContext;
        public MainPage()
        {
            InitializeComponent();
            _bindingContext = new MainView();
            BindingContext = _bindingContext;
            App.FirstPage = this;

        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            _bindingContext.OnCounterClicked(sender,e);
        }
    }

   
}
