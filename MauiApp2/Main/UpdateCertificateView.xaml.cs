using MauiViewModels;
using MauiViewModels.Main;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services.Certificate;

namespace MauiApp2.Main
{
    public partial class UpdateCertificateView : BaseContentPage
    {
        public UpdateCertificateView()
        {
            InitializeComponent();
        }

        public void NumbersPad_OnNumberClicked(string value)
        {
            ((UpdateCertificateViewModel)BindingContext).NumbersPad_OnNumberClicked(value);
        }

        public void ClearPin1_OnClicked(object sender, EventArgs e)
        {
            ((UpdateCertificateViewModel)BindingContext).ClearPin1_OnClicked();
        }

        public void ClearPin2_OnClicked(object sender, EventArgs e)
        {
            ((UpdateCertificateViewModel)BindingContext).ClearPin2_OnClicked();
        }

        public void ClearPinOld_OnClicked(object sender, EventArgs e)
        {
            ((UpdateCertificateViewModel)BindingContext).ClearPinOld_OnClicked();
        }

        public void Button_Cancel(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;
            ((UpdateCertificateViewModel)BindingContext).Button_Cancel();
            element.IsEnabled = true;
        }
    }
}