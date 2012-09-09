using GalaSoft.MvvmLight;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Media;
using System;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.ComponentModel;
using System.Threading;
using BeatMachine.Model;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Threading;

namespace BeatMachine.ViewModel
{
    public class PlayViewModel : ViewModelBase
    {
        private const int BPMTolerance = 5;

        public PlayViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            PropertyChanged += (sender, e) =>
            {
                if (String.Equals(e.PropertyName, BPMPropertyName))
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(o =>
                    {
                        List<AnalyzedSong> songs;

                        using (BeatMachineDataContext context = new BeatMachineDataContext(
                            BeatMachineDataContext.DBConnectionString))
                        {
                            DataLoadOptions dlo = new DataLoadOptions();
                            dlo.LoadWith<AnalyzedSong>(p => p.AudioSummary);
                            context.LoadOptions = dlo;
                            context.ObjectTrackingEnabled = false;
                            songs = context.AnalyzedSongs
                                .Where(s => s.AudioSummary != null &&
                                    Math.Abs((int)s.AudioSummary.Tempo - BPM) >= BPMTolerance)
                                    .Shuffle()
                                    .ToList();
                        }

                        // Need to look up the actual MediaLibrary instances corresponding
                        // to the songs we have in the database
                        List<Song> mediaLibrarySongs = songs
                            .Select<AnalyzedSong, Song>(song => song.ToMediaLibrarySong())
                            .Where(song => song != null)
                            .ToList();

                        Messenger.Default.Send<NotificationMessage<List<Song>>>(
                            new NotificationMessage<List<Song>>(mediaLibrarySongs, null));

                    }));
                }
            };

            Messenger.Default.Register<PropertyChangedMessage<bool>>(this,
                m => 
                {                    
                    if (String.Equals(m.PropertyName, SongsAnalyzedPropertyName) &&
                        m.NewValue)
                    {
                        SongsAnalyzed = true;
                    }
                });

            Messenger.Default.Register<NotificationMessage<List<Song>>>(
                this,
                m => {
                    BetterMediaPlayer p = new BetterMediaPlayer(m.Content);
                    DispatcherHelper.InvokeAsync(() => Player = p);
                });

            PlayCommand = new RelayCommand(
                () => {
                    if (Player.State == MediaState.Paused)
                    {
                        Player.Resume();
                    }
                    else if (Player.State == MediaState.Playing)
                    {
                        Player.Pause();
                    }
                    else
                    {
                        Player.Play();
                    }

                },
                () => true);

            // TODO Remember user setting
            BPM = 120;
        }

        public RelayCommand PlayCommand
        {
            get;
            private set;
        }

        public const string SongsAnalyzedPropertyName = "SongsAnalyzed";
        private bool songsAnalyzed;

        public bool SongsAnalyzed
        {
            get
            {
                return songsAnalyzed;
            }

            private set
            {
                if (songsAnalyzed == value)
                {
                    return;
                }

                songsAnalyzed = value;
                RaisePropertyChanged(SongsAnalyzedPropertyName);
            }
        }

        public const string BPMPropertyName = "BPM";
        private int bpm;

        public int BPM
        {
            get
            {
                return bpm;
            }

            set
            {
                if (bpm == value)
                {
                    return;
                }

                bpm = value;
                RaisePropertyChanged(BPMPropertyName);
            }
        }

        public const string PlayerPropertyName = "Player";
        private BetterMediaPlayer player;

        
        public BetterMediaPlayer Player
        {
            get
            {
                return player;
            }

            set
            {
                if (player == value)
                {
                    return;
                }

                player = value;
                RaisePropertyChanged(PlayerPropertyName);
            }
        }
    }
}