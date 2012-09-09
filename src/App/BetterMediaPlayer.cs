using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using System.Linq;
using Microsoft.Xna.Framework.Media;
using System.Threading;

namespace BeatMachine
{
    public class BetterMediaPlayer : ViewModelBase
    {
        public event EventHandler<EventArgs> MovedNext;
        public event EventHandler<EventArgs> MovedPrevious;
        
        private List<Song> songs;
        private Song prevSong;
        private Song nextSong;
        private List<Song> collectionSongs;
        private int currentSongIndex;
        private Timer timer;

        public BetterMediaPlayer(List<Song> songs)
        {
            this.songs = songs;
            currentSongIndex = 0;
            activeSong = songs.ElementAtOrDefault<Song>(currentSongIndex);
            
            using (MediaLibrary library = new MediaLibrary())
            {
                collectionSongs = library.Songs.ToList();
            }

            MediaPlayer.ActiveSongChanged += (s, e) =>
                {
                    if (MediaPlayer.Queue.ActiveSong == nextSong)
                    {
                        MoveNext();
                    }
                    else if (MediaPlayer.Queue.ActiveSong == prevSong)
                    {
                        MovePrev();
                    }
                    
                };

            MediaPlayer.MediaStateChanged += (s, e) =>
            {
                if (MediaPlayer.State == MediaState.Stopped)
                {
                    MoveNext();
                }

                if (MediaPlayer.State == MediaState.Playing)
                {
                    timer.Change(0, 500);
                }
                else
                {
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                RaisePropertyChanged(StatePropertyName);
            };

            timer = new Timer(
                x => RaisePropertyChanged(PlayPositionPropertyName),
                null, Timeout.Infinite, Timeout.Infinite);

            // Needed, otherwise our counting logic is completely thrown off
            // because the order is not maintained between the collection passed
            // to MediaPlayer.Play and MediaPlayer.Queue
            MediaPlayer.IsShuffled = false;
            
            // TODO Ask the user to take over the music
            // TODO Figure out how to clear existing playlist
            MediaPlayer.Stop();

            // TODO Stop music when they quit the app
        }

        private void MoveNext()
        {
            currentSongIndex++;
            if (currentSongIndex >= songs.Count)
            {
                currentSongIndex = 0;
            }
            ActiveSong = songs[currentSongIndex];
            Play();
        }

        private void MovePrev()
        {
            currentSongIndex--;
            if (currentSongIndex < 0)
            {
                currentSongIndex = 0;
            }
            ActiveSong = songs[currentSongIndex];
            Play();
        }

        public void Play()
        {
            int index = collectionSongs.IndexOf(ActiveSong);

            using (MediaLibrary library = new MediaLibrary())
            {
                Song prev = collectionSongs.ElementAtOrDefault<Song>(index-1);
                Song next = collectionSongs.ElementAtOrDefault<Song>(index + 1);

                prevSong = prev;
                nextSong = next ?? collectionSongs.FirstOrDefault<Song>();

                MediaPlayer.Play(library.Songs);
                MediaPlayer.Queue.ActiveSongIndex = index;
            }
        }

        public void Resume()
        {
            MediaPlayer.Resume();
        }

        public void Pause()
        {
            MediaPlayer.Pause();
        }

        public const string ActiveSongPropertyName = "ActiveSong";
        private Song activeSong;

        public Song ActiveSong
        {
            get
            {
                return activeSong;
            }

            private set
            {
                if (activeSong == value)
                {
                    return;
                }

                activeSong = value;
                RaisePropertyChanged(ActiveSongPropertyName);
            }
        }

        public const string StatePropertyName = "State";
        public MediaState State
        {
            get
            {
                return MediaPlayer.State;
            }
        }

        public const string PlayPositionPropertyName = "PlayPosition";
        public TimeSpan PlayPosition
        {
            get
            {
                return MediaPlayer.PlayPosition;
            }
        }

    }
}
