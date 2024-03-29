﻿using MauiViewModels.ViewModels;

namespace MauiApp2
{
    public partial class MainView : BaseContentPage
    {
        private readonly MauiViewModels.MainView _bindingContext;

        public MainView()
        {
            InitializeComponent();
            _bindingContext = new MauiViewModels.MainView();
            BindingContext = _bindingContext;
            App.FirstPage = this;
        }

        public void Button_AddAccount(object sender, EventArgs e)
        {
            _bindingContext.Button_AddAccount();
        }

        public void Button_DeleteAccount(object sender, EventArgs e)
        {
            var account = (AccountModel)((Button)sender).BindingContext;
            _bindingContext.Button_DeleteAccount(account);
        }

        private void Button_PreDeleteAccount(object sender, EventArgs e)
        {
            var account = (AccountModel)((ImageButton)sender).BindingContext;
            _bindingContext.Button_PreDeleteAccount(account);
        }

        public void Cell_OnTapped(object sender, EventArgs e)
        {
            var cell = sender as ViewCell;
            cell.IsEnabled = false;

            var account = (AccountModel)((ViewCell)sender).BindingContext;
            _bindingContext.Cell_OnTapped(account);
        }

        public void Button_Sync(object sender, EventArgs e)
        {
            _bindingContext.Button_Sync();
        }

        public void Button_ShowDeleteAccount(object sender, EventArgs e)
        {
            _bindingContext.Button_ShowDeleteAccount();
        }

        private void Menu_button(object sender, EventArgs e)
        {
            _bindingContext.Menu_button();
        }
    }
}