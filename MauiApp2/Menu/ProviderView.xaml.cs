using MauiViewModels.Menu;

namespace MauiApp2.Menu
{
    public partial class ProviderView : BaseContentPage
    {
        public ProviderView()
        {
            InitializeComponent();
        }

        private void EditButton_OnClicked(object sender, EventArgs e)
        {
            ((ProviderViewModel)BindingContext).EditButton_OnClicked();
        }
    }
}