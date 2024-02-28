using System.Net;
using System.Timers;
using MauiViewModels.Notifications;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace MauiApp2.Notifications
{
    public partial class ConfirmByPinView : BaseContentPage
    {
        public ConfirmByPinView()
        {
            InitializeComponent();
        }

        public void NumbersPad_OnNumberClicked(string value)
        {
            ((ConfirmByPinViewModel)BindingContext).NumbersPad_OnNumberClicked(value);
        }

        public void Cancel_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            ((ConfirmByPinViewModel)BindingContext).Cancel_OnClicked();
            element.IsEnabled = true;
        }
    }
}