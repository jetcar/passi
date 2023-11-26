using MauiApp2.StorageModels;

namespace MauiApp2.Menu
{

    public partial class ProviderView : BaseContentPage
    {
        public ProviderDb Provider { get; set; }

        public ProviderView(ProviderDb provider)
        {
            Provider = provider;
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;

        }

        private void EditButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            _navigationService.PushModalSinglePage(new EditProviderView(Provider));
            button.IsEnabled = true;
        }

    }
}