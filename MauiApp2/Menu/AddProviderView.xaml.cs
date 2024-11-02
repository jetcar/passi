using MauiViewModels.Menu;

namespace MauiApp2.Menu
{
    public partial class AddProviderView : BaseContentPage
    {
        public AddProviderView()
        {
            InitializeComponent();
        }

        private void SaveButton_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;
            ((AddProviderViewModel)BindingContext).SaveButton_OnClicked();

            //save
            button.IsEnabled = true;
        }

        private void BackButton_OnClicked(object sender, EventArgs e)
        {

            ((AddProviderViewModel)BindingContext).CancelButton_OnClicked();
        }

       
    }
}