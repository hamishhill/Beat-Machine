using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using BeatMachine.Model;

namespace BeatMachine.ViewModel
{
  
    public class SongsViewModel : ViewModelBase
    {
        public SongsViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        public const string SongsPropertyName = "Songs";
        private ObservableCollection<AnalyzedSong> songs;

        public ObservableCollection<AnalyzedSong> Songs
        {
            get
            {
                return songs;
            }

            set
            {
                if (songs == value)
                {
                    return;
                }

                songs = value;
                RaisePropertyChanged(SongsPropertyName);
            }
        }

    }
}