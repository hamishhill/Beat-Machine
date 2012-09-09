using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Threading;
using BeatMachine.Model;
using System.Data.Linq;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Microsoft.Xna.Framework.Media;
using BeatMachine.ViewModel;

namespace BeatMachine.View
{
    public class PlayViewModelToTimingLabelConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PlayViewModel vm = value as PlayViewModel;
            TimeSpan zero = new TimeSpan();
            return String.Format("{0:hh\\:mm\\:ss} / {1:hh\\:mm\\:ss}",
                (vm.Player != null && vm.Player.PlayPosition != null) ? vm.Player.PlayPosition : zero,
                (vm.Player != null && vm.Player.ActiveSong != null && vm.Player.ActiveSong.Duration != null) ? vm.Player.ActiveSong.Duration : zero);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }

    public partial class Play : PhoneApplicationPage
    {
        public Play()
        {
            InitializeComponent();            
        }
    }
}