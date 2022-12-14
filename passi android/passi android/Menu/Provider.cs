using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace passi_android.Menu
{
    public class Provider: INotifyPropertyChanged
    {
        private bool _isDeleteVisible;
        private string _name;

        public Provider()
        {
            this.Guid = Guid.NewGuid();
        }

        public Guid Guid { get; set; }

        [JsonIgnore]
        public bool IsDeleteVisible
        {
            get => _isDeleteVisible;
            set
            {
                if (value == _isDeleteVisible) return;
                _isDeleteVisible = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string WebApiUrl { get; set; }
        public string SignupPath { get; set; }
        public string SignupConfirmation { get; set; }
        public string SignupCheck { get; set; }
        public string TokenUpdate { get; set; }
        public string CancelCheck { get; set; }
        public string Authorize { get; set; }
        public string Time { get; set; }
        public string UpdateCertificate { get; set; }
        public string CheckForStartedSessions { get; set; }
        public string SyncAccounts { get; set; }
        public string DeleteAccount { get; set; }
        public bool IsDefault { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}