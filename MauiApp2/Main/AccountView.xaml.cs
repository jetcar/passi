﻿using MauiViewModels.Main;

namespace MauiApp2.Main
{
    public partial class AccountView : BaseContentPage
    {
        public AccountView()
        {
            InitializeComponent();
        }

        public void UpdateCertificate_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            ((AccountViewModel)BindingContext).UpdateCertificate_OnClicked();
            button.IsEnabled = true;
        }

        public void AddBiometric_Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            ((AccountViewModel)BindingContext).AddBiometric_Button_OnClicked();

            button.IsEnabled = true;
        }

        private void BackButton_OnClicked(object sender, EventArgs e)
        {
            ((AccountViewModel)BindingContext).BackButton_OnClicked();
        }
    }
}