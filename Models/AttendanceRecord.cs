using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FehlzeitApp.Models
{
    public class DayData : INotifyPropertyChanged
    {
        private string _status = "";
        private Brush _background = Brushes.Transparent;
        private Brush _text = Brushes.Black;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
        
        public Brush BackgroundColor 
        {
            get => _background;
            set { _background = value; OnPropertyChanged(); }
        }
        
        public Brush TextColor
        {
            get => _text; 
            set { _text = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class AttendanceRecord : INotifyPropertyChanged
    {
        private string _name = "";
        private int _mitarbeiterId;
        
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
        
        public int MitarbeiterId
        {
            get => _mitarbeiterId;
            set { _mitarbeiterId = value; OnPropertyChanged(); }
        }
        
        // Day properties (Day01 through Day31)
        public DayData Day01 { get; } = new DayData();
        public DayData Day02 { get; } = new DayData();
        public DayData Day03 { get; } = new DayData();
        public DayData Day04 { get; } = new DayData();
        public DayData Day05 { get; } = new DayData();
        public DayData Day06 { get; } = new DayData();
        public DayData Day07 { get; } = new DayData();
        public DayData Day08 { get; } = new DayData();
        public DayData Day09 { get; } = new DayData();
        public DayData Day10 { get; } = new DayData();
        public DayData Day11 { get; } = new DayData();
        public DayData Day12 { get; } = new DayData();
        public DayData Day13 { get; } = new DayData();
        public DayData Day14 { get; } = new DayData();
        public DayData Day15 { get; } = new DayData();
        public DayData Day16 { get; } = new DayData();
        public DayData Day17 { get; } = new DayData();
        public DayData Day18 { get; } = new DayData();
        public DayData Day19 { get; } = new DayData();
        public DayData Day20 { get; } = new DayData();
        public DayData Day21 { get; } = new DayData();
        public DayData Day22 { get; } = new DayData();
        public DayData Day23 { get; } = new DayData();
        public DayData Day24 { get; } = new DayData();
        public DayData Day25 { get; } = new DayData();
        public DayData Day26 { get; } = new DayData();
        public DayData Day27 { get; } = new DayData();
        public DayData Day28 { get; } = new DayData();
        public DayData Day29 { get; } = new DayData();
        public DayData Day30 { get; } = new DayData();
        public DayData Day31 { get; } = new DayData();

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void UpdateDay(string dayKey, string status, Brush background, Brush text)
        {
            var dayProp = GetType().GetProperty(dayKey);
            if (dayProp != null)
            {
                var day = (DayData)dayProp.GetValue(this);
                day.Status = status;
                day.BackgroundColor = background;
                day.TextColor = text;
            }
        }
        
        public DayData GetDay(string dayKey)
        {
            var dayProp = GetType().GetProperty(dayKey);
            return dayProp != null ? (DayData)dayProp.GetValue(this) : null;
        }
    }
}
