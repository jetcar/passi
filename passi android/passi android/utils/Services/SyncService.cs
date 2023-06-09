using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using passi_android.Notifications;
using passi_android.ViewModels;
using WebApiDto;
using WebApiDto.Auth;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;

namespace passi_android.utils.Services
{
    public class SyncService : ISyncService
    {
        private object locker = new object();
        private IMainThreadService _mainThreadService;
        private INavigationService _navigationService;
        private ISecureRepository _secureRepository;
        private IRestService _restService;
        public SyncService(IMainThreadService mainThreadService, INavigationService navigationService, ISecureRepository secureRepository, IRestService restService)
        {
            _mainThreadService = mainThreadService;
            _navigationService = navigationService;
            _secureRepository = secureRepository;
            _restService = restService;
        }

        public void PollNotifications()
        {
            if (PollingTask?.IsCompleted != false)
                PollingTask = Task.Run(() =>
                {
                    lock (locker)
                    {

                        var accounts = new ObservableCollection<AccountViewModel>();
                        _secureRepository.LoadAccountIntoList(accounts);
                        var providers = _secureRepository.LoadProviders();
                        var groupedAccounts = accounts.GroupBy(x => x.ProviderGuid);
                        foreach (var groupedAccount in groupedAccounts)
                        {
                            var providerGuid = groupedAccount.ToList().First().ProviderGuid ?? providers.First(x => x.IsDefault).Guid;
                            var provider = _secureRepository.LoadProviders().First(x => x.Guid == providerGuid);
                            var getAllSessionDto = new GetAllSessionDto()
                            {
                                DeviceId = _secureRepository.GetDeviceId()
                            };

                            var guids = accounts.Select(x => x.Guid.ToString()).ToList();
                            var task = _restService.ExecutePostAsync(provider, provider.SyncAccounts, new SyncAccountsDto()
                            {
                                DeviceId = _secureRepository.GetDeviceId(),
                                Guids = guids
                            });

                            var restResponse = task.Result;
                            if (restResponse.IsSuccessful)
                            {
                                var serverAccounts = JsonConvert.DeserializeObject<List<AccountMinDto>>(restResponse.Content);
                                foreach (var account in groupedAccount)
                                {
                                    if (serverAccounts.All(x => x.UserGuid != account.Guid))
                                    {
                                        var loadedAccount = _secureRepository.GetAccount(account.Guid);
                                        if (loadedAccount != null && loadedAccount.IsConfirmed)
                                        {
                                            loadedAccount.Inactive = true;
                                            _secureRepository.UpdateAccount(loadedAccount);
                                        }
                                    }
                                }

                                if (App.AccountSyncCallback != null)
                                    App.AccountSyncCallback.Invoke();
                            }

                            var task2 = _restService.ExecutePostAsync(provider, provider.CheckForStartedSessions, getAllSessionDto);

                            var response = task2.Result;
                            if (response.IsSuccessful)
                            {
                                var msg = JsonConvert.DeserializeObject<NotificationDto>(response.Content);
                                if (msg != null)
                                {
                                    if (_secureRepository.CheckSessionKey(msg.SessionId))
                                    {
                                        _mainThreadService.BeginInvokeOnMainThread(() =>
                                        {
                                            _navigationService.PushModalSinglePage(new NotificationVerifyRequestView(msg));
                                        });
                                    }
                                }
                            }
                        }

                    }

                });
        }

        public Task PollingTask { get; private set; }
    }

    public interface ISyncService
    {
        void PollNotifications();
        Task PollingTask { get; }
    }
}