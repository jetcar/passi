using System.Collections.ObjectModel;
using System.Linq;
using MauiViewModels.StorageModels;

namespace MauiViewModels.Menu
{
    public class MenuViewModel : PassiBaseViewModel
    {
        private ObservableCollection<ProviderDb> _provider;
        private bool _isDeleteVisible;

        public MenuViewModel()
        {
            Providers = new ObservableCollection<ProviderDb>(_secureRepository.LoadProviders().Result);
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

        public void Button_PreDeleteProvider(ProviderDb provider)
        {
            provider.IsDeleteVisible = !provider.IsDeleteVisible;
        }

        public void Button_DeleteProvider(ProviderDb provider)
        {
            if (provider.IsDefault && Providers.Count(x => x.IsDefault) == 1)
                return;
            _secureRepository.DeleteProvider(provider);
            Providers.Remove(provider);
        }

        public void Cell_OnTapped(ProviderDb provider)
        {
            _navigationService.PushModalSinglePage(new ProviderViewModel(provider));
        }

        public void Button_Add()
        {
            _navigationService.PushModalSinglePage(new AddProviderViewModel());
        }

        public void Button_ShowDelete()
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

        public void Back_button()
        {
            _navigationService.PopModal();
        }
    }
}