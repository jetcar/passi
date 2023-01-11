using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using passi_android.Menu;

namespace passi_android.utils
{
    public class Account : INotifyPropertyChanged
    {
        private Guid _guid;
        private bool _isConfirmed;
        private string _thumbprint;
        private string _deviceId;
        private string _email;
        private DateTime _validFrom;
        private DateTime _validTo;
        private bool _isDeleteVisible;
        private bool _inactive;
        [JsonIgnore]
        public ProviderDb Provider { get; set; }
        public string ProviderName { get; set; }
        public Guid Guid
        {
            get => _guid;
            set => _guid = value;
        }

        public bool IsConfirmed
        {
            get => _isConfirmed;
            set => _isConfirmed = value;
        }

        public string ConfirmedLabel
        {
            get { return IsConfirmed ? "Confirmed" : "Waiting confirmation"; }
        }

        public string Thumbprint
        {
            get => _thumbprint;
            set => _thumbprint = value;
        }

        public string DeviceId
        {
            get => _deviceId;
            set => _deviceId = value;
        }

        public string Email
        {
            get => _email;
            set => _email = value;
        }

        public DateTime ValidFrom
        {
            get => _validFrom;
            set => _validFrom = value;
        }

        public DateTime ValidTo
        {
            get => _validTo;
            set => _validTo = value;
        }

        public string ValidToLabel
        {
            get
            {
                return ValidTo.ToShortDateString();
            }
        }

        [JsonIgnore]
        public bool IsDeleteVisible
        {
            get => _isDeleteVisible;
            set
            {
                _isDeleteVisible = value;
                OnPropertyChanged();
            }
        }

        public bool Inactive
        {
            get => _inactive;
            set
            {
                _inactive = value;
                OnPropertyChanged();
                OnPropertyChanged("Active");
            }
        }

        public bool Active
        {
            get => !Inactive;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}