using passi_maui.utils;

namespace passi_maui.Menu
{
    [QueryProperty("Provider", "Provider")]
    public partial class ProviderView : ContentPage
    {
        private ProviderDb _provider;

        public ProviderDb Provider
        {
            get => _provider;
            set
            {
                if (Equals(value, _provider)) return;
                _provider = value;
                OnPropertyChanged();
            }
        }

        public ProviderView()
        {
            InitializeComponent();
            BindingContext = this;

        }

        private void EditButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            Navigation.PushModalSinglePage(new EditProviderView(),new Dictionary<string, object>() { {"Provider",Provider}});
            button.IsEnabled = true;
        }


        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }
    }
}