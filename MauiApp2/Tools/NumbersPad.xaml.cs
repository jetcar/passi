namespace MauiApp2.Tools
{
    public partial class NumbersPad : ContentView
    {
        public NumbersPad()
        {
            if (!App.IsTest)
                InitializeComponent();
        }

        public delegate void NumbersPadDelegate(string value);

        public event NumbersPadDelegate NumberClicked;

        public void ImageButton_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            var button = sender as Button;
            var imageButton = sender as ImageButton;
            var value = button?.CommandParameter?.ToString() ?? imageButton?.CommandParameter?.ToString();
            if (NumberClicked != null)
                NumberClicked.Invoke(value);
            element.IsEnabled = true;
        }
    }
}