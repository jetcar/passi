using ChatViewModel;

namespace MauiApp1
{
    public partial class MainPage : BaseContentPage
    {

        private readonly MainView _bindingContext;
        public MainPage(MainView bindingContext)
        {
            _bindingContext = bindingContext;
            InitializeComponent();
            BindingContext = _bindingContext;
            App.FirstPage = this;

        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            await _bindingContext.OnCounterClicked(sender, e);
        }
    }


}
