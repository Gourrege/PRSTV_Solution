using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_WPF.Models
{
    internal class BallotPreferenceRow : INotifyPropertyChanged
    {
        public long Id { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public long RandomBallotId { get; set; }

        private int? _preference;
        public int? Preference
        {
            get => _preference;
            set
            {
                if (_preference == value) return;
                _preference = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preference)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
