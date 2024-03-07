using MauiViewModels.Menu;

namespace MauiApp2.Menu
{
    public partial class EditProviderView : BaseContentPage
    {
        public EditProviderView()
        {
            InitializeComponent();
        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            ((EditProviderViewModel)BindingContext).SaveButton_OnClicked();
        }
    }
}