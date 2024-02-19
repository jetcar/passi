using System.Net;
using System.Timers;
using MauiApp2.Tools;
using MauiViewModels;
using MauiViewModels.Notifications;
using MauiViewModels.StorageModels;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.Auth;
using Color = Microsoft.Maui.Graphics.Color;
using Timer = System.Timers.Timer;

namespace MauiApp2.Notifications
{
    public partial class NotificationVerifyRequestView : BaseContentPage
    {
        public NotificationVerifyRequestView()
        {
            InitializeComponent();
        }

        public void ImageButton1_OnClicked(object sender, EventArgs e)
        {
            ((NotificationVerifyRequestViewModel)BindingContext).ImageButton1_OnClicked();
        }

        public void ImageButton2_OnClicked(object sender, EventArgs e)
        {
            ((NotificationVerifyRequestViewModel)BindingContext).ImageButton2_OnClicked();
        }

        public void ImageButton3_OnClicked(object sender, EventArgs e)
        {
            ((NotificationVerifyRequestViewModel)BindingContext).ImageButton3_OnClicked();
        }

        public void Cancel_OnClicked(object sender, EventArgs e)
        {
            ((NotificationVerifyRequestViewModel)BindingContext).Cancel_OnClicked();
        }
    }
}