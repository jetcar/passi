﻿using AppCommon;
using MauiApp2.utils.Services;
using MauiApp2.utils.Services.Certificate;

namespace MauiApp2
{
    public class BaseContentPage : ContentPage
    {
        public static INavigationService _navigationService;
        protected IMainThreadService _mainThreadService;
        protected ISecureRepository _secureRepository;
        protected IRestService _restService;
        protected ICertHelper _certHelper;
        protected ICertificatesService _certificatesService;
        protected IMySecureStorage _mySecureStorage;
        protected IDateTimeService _dateTimeService;
        protected IFingerPrintService _fingerPrintService;
        protected ISyncService _syncService;

        public BaseContentPage()
        {
            _navigationService = App.Services.GetService<INavigationService>();
            _mainThreadService = App.Services.GetService<IMainThreadService>();
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _restService = App.Services.GetService<IRestService>();
            _certHelper = App.Services.GetService<ICertHelper>();
            _certificatesService = App.Services.GetService<ICertificatesService>();
            _mySecureStorage = App.Services.GetService<IMySecureStorage>();
            _dateTimeService = App.Services.GetService<IDateTimeService>();
            _fingerPrintService = App.Services.GetService<IFingerPrintService>();
            _syncService = App.Services.GetService<ISyncService>();
        }

        public bool Appeared { get; private set; }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Appeared = true;
        }

        protected override void OnDisappearing()
        {
            Appeared = false;
            base.OnDisappearing();
        }
    }
}