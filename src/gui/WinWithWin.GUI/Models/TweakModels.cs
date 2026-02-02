using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WinWithWin.GUI.Models
{
    public class TweakViewModel : INotifyPropertyChanged
    {
        private bool _isApplied;
        private bool _isFavorite;
        private bool _isSelected;
        private bool _isExpanded;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailedDescription { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Risk { get; set; } = "Safe";
        public string[] SupportedVersions { get; set; } = System.Array.Empty<string>();
        public string[] AffectedComponents { get; set; } = System.Array.Empty<string>();
        public bool IsReversible { get; set; } = true;

        public bool IsApplied
        {
            get => _isApplied;
            set
            {
                if (_isApplied != value)
                {
                    _isApplied = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FavoriteIcon));
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ExpandIcon));
                }
            }
        }

        public string FavoriteIcon => IsFavorite ? "★" : "☆";
        public string ExpandIcon => IsExpanded ? "▲" : "▼";
        
        public string AffectedComponentsText => AffectedComponents.Length > 0 
            ? string.Join(", ", AffectedComponents) 
            : "None specified";
        
        public string ReversibleText => IsReversible ? "✓ Fully reversible" : "⚠ May require manual steps to undo";

        public Brush RiskColor => Risk switch
        {
            "Safe" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),      // Green
            "Moderate" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),  // Orange
            "Advanced" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),  // Red
            _ => new SolidColorBrush(Color.FromRgb(76, 175, 80))
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TweakConfig
    {
        public string Version { get; set; } = "1.0.0";
        public TweakDefinition[] Tweaks { get; set; } = System.Array.Empty<TweakDefinition>();
    }

    public class TweakDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailedDescription { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Risk { get; set; } = "Safe";
        public string[] SupportedVersions { get; set; } = System.Array.Empty<string>();
        public string[] AffectedComponents { get; set; } = System.Array.Empty<string>();
        public bool IsReversible { get; set; } = true;
        public TweakFunctions? Functions { get; set; }
    }

    public class TweakFunctions
    {
        public string Test { get; set; } = string.Empty;
        public string Set { get; set; } = string.Empty;
        public string Undo { get; set; } = string.Empty;
    }

    public class PresetConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public string[] Tweaks { get; set; } = System.Array.Empty<string>();
    }
}
