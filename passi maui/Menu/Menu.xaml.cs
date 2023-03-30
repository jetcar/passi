using System.Collections.ObjectModel;
using passi_maui.utils;

namespace passi_maui.Menu
{
    public partial class Menu : ContentPage
    {
        private ObservableCollection<ProviderDb> _provider;
        private bool _isDeleteVisible;

        public Menu()
        {
            InitializeComponent();
            BindingContext = this;
            Providers = new ObservableCollection<ProviderDb>(SecureRepository.LoadProviders());

        }

        public ObservableCollection<ProviderDb> Providers
        {
            get { return _provider ?? (_provider = new ObservableCollection<ProviderDb>()); }
            set
            {
                if (_provider != value)
                {
                    _provider = value;
                    OnPropertyChanged();
                }
            }
        }

        private void Button_PreDeleteProvider(object sender, EventArgs e)
        {
            var account = (ProviderDb)((ImageButton)sender).BindingContext;
            account.IsDeleteVisible = !account.IsDeleteVisible;
        }

        private void Button_DeleteProvider(object sender, EventArgs e)
        {
            var provider = (ProviderDb)((Button)sender).BindingContext;
            if (provider.IsDefault && Providers.Count(x => x.IsDefault) == 1)
                return;
            SecureRepository.DeleteProvider(provider);
            Providers.Remove(provider);

        }

        private void Cell_OnTapped(object sender, EventArgs e)
        {
            var cell = sender as ViewCell;
            cell.IsEnabled = false;

            var provider = (ProviderDb)((ViewCell)sender).BindingContext;

            Navigation.PushModalSinglePage(new ProviderView(provider));
            cell.IsEnabled = true;
        }

        private void Button_Add(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new AddProviderView());
        }

        private void Button_ShowDelete(object sender, EventArgs e)
        {
            IsDeleteVisible = !IsDeleteVisible;
            foreach (var provider in Providers)
            {
                provider.IsDeleteVisible = false;
            }
        }

        public bool IsDeleteVisible
        {
            get => _isDeleteVisible;
            set
            {
                if (value == _isDeleteVisible) return;
                _isDeleteVisible = value;
                OnPropertyChanged();
            }
        }

        private void Button_Back(object sender, EventArgs e)
        {
            Navigation.PopModal();
        }
    }
}