using MauiViewModels.FingerPrint;
using MauiViewModels.StorageModels;

namespace MauiApp2.FingerPrint
{
    public partial class FingerPrintConfirmByPinView : BaseContentPage
    {
        public FingerPrintConfirmByPinView()
        {
            InitializeComponent();
        }

        private void Cancel_OnClicked(object sender, EventArgs e)
        {
            ((FingerPrintConfirmByPinViewModel)BindingContext).Cancel_OnClicked();
        }

        private void NumbersPad_OnNumberClicked(string value)
        {
            ((FingerPrintConfirmByPinViewModel)BindingContext).NumbersPad_OnNumberClicked(value);
        }
    }
}