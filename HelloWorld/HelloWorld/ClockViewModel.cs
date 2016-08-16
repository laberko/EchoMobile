using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace HelloWorld
{
    public class ClockViewModel : INotifyPropertyChanged
    {
        private DateTime _dateTime;
        private Color _backColor;
        private Color _textColor;

        public event PropertyChangedEventHandler PropertyChanged;

        public ClockViewModel()
        {
            _dateTime = DateTime.Now;

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                DateTime = DateTime.Now;
                return true;
            });
        }

        public DateTime DateTime
        {
            private set
            {
                if (_dateTime == value) return;
                _dateTime = value;
                var colorNumber = _dateTime.Second * 4;
                Colour = Color.FromRgb(colorNumber, colorNumber, colorNumber);
                TextColour = Color.FromRgb(255 - colorNumber, 255 - colorNumber, 255 - colorNumber);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DateTime"));
            }
            get
            {
                return _dateTime;
            }
        }

        public Color Colour
        {
            private set
            {
                if (_backColor == value) return;
                _backColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Colour"));
            }
            get { return _backColor; }
        }

        public Color TextColour
        {
            private set
            {
                if (_textColor == value) return;
                _textColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TextColour"));
            }
            get { return _textColor; }
        }
    }
}
