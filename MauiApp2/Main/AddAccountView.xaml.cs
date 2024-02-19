using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using MauiApp2.Registration;
using MauiApp2.Tools;
using MauiViewModels.Main;
using Newtonsoft.Json;
using WebApiDto;
using WebApiDto.SignUp;

namespace MauiApp2.Main
{
    public partial class AddAccountView : BaseContentPage
    {
        public AddAccountView()
        {
            InitializeComponent();
        }

        public void Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            ((AddAccountViewModel)BindingContext).Button_OnClicked();
            button.IsEnabled = true;
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            ((AddAccountViewModel)BindingContext).CancelButton_OnClicked();
        }

        private void Picker_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedIndex = ((Picker)sender).SelectedIndex;
            ((AddAccountViewModel)BindingContext).Picker_OnSelectedIndexChanged(selectedIndex);
        }
    }
}