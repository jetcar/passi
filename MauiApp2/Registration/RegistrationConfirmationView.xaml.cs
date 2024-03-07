namespace MauiApp2.Registration
{
    public partial class RegistrationConfirmationView : BaseContentPage
    {
        public RegistrationConfirmationView()
        {
            InitializeComponent();
        }

        public void NumbersPad_OnNumberClicked(string value)
        {
            ((MauiViewModels.Registration.RegistrationConfirmationViewModel)BindingContext).NumbersPad_OnNumberClicked(value);
        }

        public void CancelButton_OnClicked(object sender, EventArgs e)
        {
            ((MauiViewModels.Registration.RegistrationConfirmationViewModel)BindingContext).CancelButton_OnClicked();
        }
    }
}