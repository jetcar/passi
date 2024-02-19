using System.Collections.ObjectModel;
using MauiViewModels.Menu;
using MauiViewModels.StorageModels;

namespace MauiApp2.Menu
{
    public partial class MenuView : BaseContentPage
    {
        public MenuView()
        {
            InitializeComponent();
        }

        private void Button_PreDeleteProvider(object sender, EventArgs e)
        {
            var provider = (ProviderDb)((Button)sender).BindingContext;
            ((MenuViewModel)BindingContext).Button_PreDeleteProvider(provider);
        }

        private void Button_DeleteProvider(object sender, EventArgs e)
        {
            var provider = (ProviderDb)((Button)sender).BindingContext;
            ((MenuViewModel)BindingContext).Button_DeleteProvider(provider);
        }

        private void Cell_OnTapped(object sender, EventArgs e)
        {
            var cell = sender as ViewCell;
            cell.IsEnabled = false;

            var provider = (ProviderDb)((ViewCell)sender).BindingContext;
            ((MenuViewModel)BindingContext).Cell_OnTapped(provider);

            cell.IsEnabled = true;
        }

        private void Button_Add(object sender, EventArgs e)
        {
            ((MenuViewModel)BindingContext).Button_Add();
        }

        private void Button_ShowDelete(object sender, EventArgs e)
        {
            ((MenuViewModel)BindingContext).Button_ShowDelete();
        }
    }
}