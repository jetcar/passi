using System;

namespace MauiViewModels.Tools
{
    public class NumbersPad
    {
        public NumbersPad()
        {
        }

        public delegate void NumbersPadDelegate(string value);

        public event NumbersPadDelegate NumberClicked;

        public void ImageButton_OnClicked(string value)
        {
            if (NumberClicked != null)
                NumberClicked.Invoke(value);
        }
    }
}