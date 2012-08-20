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

namespace BeatMachine
{
    public partial class Play : PhoneApplicationPage
    {
        public Play()
        {
            InitializeComponent();
            LoadSongs();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoadSongs();
        }

        private void LoadSongs()
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
                    songs = context.AnalyzedSongs.ToList();
                }

                songsHeader.Dispatcher.BeginInvoke(() =>
                    songsHeader.Text = String.Format("songs ({0})", songs.Count)
                    );

                result.Dispatcher.BeginInvoke(() =>
                        result.ItemsSource = new ObservableCollection<string>()
                        );

                if (songs.Count > 0)
                {
                    // Perf optimization for loading large number of items
                    // inside ListBox: let the UI thread "breathe" by loading
                    // in batches
                    int batchSize = 100;
                    while (songs.Any())
                    {
                        result.Dispatcher.BeginInvoke(() =>
                        {
                            foreach (AnalyzedSong s in songs.Take(batchSize))
                            {
                                (result.ItemsSource as ObservableCollection<string>).
                                    Add(s.ToString());
                            }
                            songs = songs.Skip(batchSize).ToList();
                        });
                    }
                }
                else
                {
                    result.Dispatcher.BeginInvoke(() =>
                        (result.ItemsSource as ObservableCollection<string>).
                        Add("No analyzed songs available")
                        );
                }


            }));
        }
    }
}