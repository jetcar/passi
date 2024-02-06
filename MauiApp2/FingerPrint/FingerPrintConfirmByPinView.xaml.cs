using MauiViewModels.StorageModels;

namespace MauiApp2.FingerPrint
{
    public partial class FingerPrintConfirmByPinView : BaseContentPage
    {
        private readonly MauiViewModels.FingerPrint.FingerPrintConfirmByPinView _bindingContext;

        public FingerPrintConfirmByPinView(AccountDb accountDb)
        {
            InitializeComponent();
            _bindingContext = new MauiViewModels.FingerPrint.FingerPrintConfirmByPinView(accountDb);
            BindingContext = _bindingContext;
        }

        private void Cancel_OnClicked(object sender, EventArgs e)
        {
            _bindingContext.Cancel_OnClicked();
        }

        private void NumbersPad_OnNumberClicked(string value)
        {
            _bindingContext.NumbersPad_OnNumberClicked(value);
        }
    }
}