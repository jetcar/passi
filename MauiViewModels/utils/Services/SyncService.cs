using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MauiViewModels.Notifications;
using MauiViewModels.ViewModels;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.Auth;

namespace MauiViewModels.utils.Services;

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
                    var accounts = new ObservableCollection<AccountModel>();
                    _secureRepository.LoadAccountIntoList(accounts);
                    var providers = _secureRepository.LoadProviders();
                    var groupedAccounts = accounts.GroupBy(x => x.ProviderGuid);
                    foreach (var groupedAccount in groupedAccounts)
                    {
                        var providerGuid = groupedAccount.ToList().First().ProviderGuid ?? providers.Result.First(x => x.IsDefault).Guid;
                        var provider = providers.Result.First(x => x.Guid == providerGuid);
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
                            var accountChanged = false;
                            foreach (var account in groupedAccount)
                            {
                                var serverAccount = serverAccounts.FirstOrDefault(x => x.UserGuid == account.Guid);
                                if (serverAccount == null)
                                {
                                    var loadedAccount = _secureRepository.GetAccount(account.Guid);
                                    if (loadedAccount != null && loadedAccount.IsConfirmed)
                                    {
                                        loadedAccount.Inactive = true;
                                        _secureRepository.UpdateAccount(loadedAccount);
                                        accountChanged = true;
                                    }
                                }
                                else
                                {
                                    var loadedAccount = _secureRepository.GetAccount(account.Guid);
                                    if (loadedAccount != null && loadedAccount.IsConfirmed && loadedAccount.Inactive)
                                    {
                                        loadedAccount.Inactive = false;
                                        _secureRepository.UpdateAccount(loadedAccount);
                                        accountChanged = true;
                                    }
                                }
                            }

                            if (CommonApp.AccountSyncCallback != null && accountChanged)
                                CommonApp.AccountSyncCallback.Invoke();
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
                                        _navigationService.PushModalSinglePage(new NotificationVerifyRequestViewModel(msg));
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