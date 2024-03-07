using MauiViewModels.Notifications;

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