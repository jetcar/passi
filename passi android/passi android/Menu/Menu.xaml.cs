using System;
using System.Collections.ObjectModel;
using System.Linq;
using passi_android.utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Menu
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Menu : ContentPage
	{
        private ObservableCollection<Provider> _provider;
        private bool _isDeleteVisible;

        public Menu ()
		{
			InitializeComponent ();
            BindingContext = this;
            Providers = new ObservableCollection<Provider>(MainPage.Providers);

        }

        public ObservableCollection<Provider> Providers
        {
            get { return _provider ?? (_provider = new ObservableCollection<Provider>()); }
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
            var account = (Provider)((ImageButton)sender).BindingContext;
            account.IsDeleteVisible = !account.IsDeleteVisible;
        }

        private void Button_DeleteProvider(object sender, EventArgs e)
        {
            var provider = (Provider)((Button)sender).BindingContext;
            if(provider.IsDefault && Providers.Count(x=>x.IsDefault) == 1)
                return;
            Providers.Remove(provider);
            MainPage.Providers = Providers.ToList();
            SecureRepository.DeleteProvider(provider);
        }

        private void Cell_OnTapped(object sender, EventArgs e)
        {
            
        }

        private void Button_Add(object sender, EventArgs e)
        {
            
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