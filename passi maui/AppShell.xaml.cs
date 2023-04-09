using passi_maui.Admin;
using passi_maui.FingerPrint;
using passi_maui.Menu;
using passi_maui.Notifications;
using passi_maui.Registration;
using passi_maui.Tools;

namespace passi_maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(AdminPage), typeof(AdminPage));
            Routing.RegisterRoute(nameof(FingerPrintConfirmByPinView), typeof(FingerPrintConfirmByPinView));
            Routing.RegisterRoute(nameof(FingerPrintView), typeof(FingerPrintView));
            Routing.RegisterRoute(nameof(AddProviderView), typeof(AddProviderView));
            Routing.RegisterRoute(nameof(EditProviderView), typeof(EditProviderView));
            Routing.RegisterRoute(nameof(Menu), typeof(Menu.Menu));
            Routing.RegisterRoute(nameof(ProviderView), typeof(ProviderView));
            Routing.RegisterRoute(nameof(ConfirmByPinView), typeof(ConfirmByPinView));
            Routing.RegisterRoute(nameof(NotificationVerifyRequestView), typeof(NotificationVerifyRequestView));
            Routing.RegisterRoute(nameof(FinishConfirmation), typeof(FinishConfirmation));
            Routing.RegisterRoute(nameof(RegistrationConfirmation), typeof(RegistrationConfirmation));
            Routing.RegisterRoute(nameof(LoadingPage), typeof(LoadingPage));
            var accountViewName = nameof(AccountView);
            Routing.RegisterRoute(accountViewName, typeof(AccountView));
            Routing.RegisterRoute(nameof(AddAccountPage), typeof(AddAccountPage));
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute(nameof(TermsAgreements), typeof(TermsAgreements));
            Routing.RegisterRoute(nameof(UpdateCertificate), typeof(UpdateCertificate));
        }

        public static MainPage MainPage { get; set; }
    }
}