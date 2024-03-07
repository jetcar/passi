using MauiViewModels.Registration;

namespace MauiApp2.Registration
{
    public partial class FinishConfirmationView : BaseContentPage
    {
        public FinishConfirmationView()
        {
            InitializeComponent();
        }

        public void NumbersPad_OnNumberClicked(string value)
        {
            ((FinishConfirmationViewModel)BindingContext).NumbersPad_OnNumberClicked(value);
        }

        public void ClearPin1_OnClicked(object sender, EventArgs e)
        {
            ((FinishConfirmationViewModel)BindingContext).ClearPin1_OnClicked();
        }

        public void ClearPin2_OnClicked(object sender, EventArgs e)
        {
            ((FinishConfirmationViewModel)BindingContext).ClearPin2_OnClicked();
        }

        public void SkipButton_OnClicked(object sender, EventArgs e)
        {
            ((FinishConfirmationViewModel)BindingContext).SkipButton_OnClicked();
        }
    }
}