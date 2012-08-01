using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using BeatMachine.EchoNest;
using System.ComponentModel;
using System.Collections.ObjectModel;
using BeatMachine.EchoNest.Model;
using XnaSong = Microsoft.Xna.Framework.Media.Song;
using XnaMediaLibrary = Microsoft.Xna.Framework.Media.MediaLibrary;

// TODO Add a logger for debugging

// TODO Unit tests

namespace BeatMachine.Model
{
    public class DataModel : INotifyPropertyChanged
    {
        private const int uploadTake = 300;
        private int uploadSkip = 0;

        // More than 100 will fail due to EchoNest hardcoded limit
        private const int downloadTake = 100;
        private int downloadSkip = 0;

        private string catalogId;
        public string CatalogId
        {
            get { return catalogId; }
            set
            {
                catalogId = value;
                OnPropertyChanged("CatalogId");
            }
        }

        private List<AnalyzedSong> songsOnDevice;
        public List<AnalyzedSong> SongsOnDevice
        {
            get { return songsOnDevice; }
            set
            {
                songsOnDevice = value; 
                OnPropertyChanged("SongsOnDevice"); 
            }
        }

        private bool songsOnDeviceLoaded;
        public bool SongsOnDeviceLoaded
        {
            get { return songsOnDeviceLoaded; }
            set
            {
                songsOnDeviceLoaded = value;
                OnPropertyChanged("SongsOnDeviceLoaded");
            }
        }

        private List<AnalyzedSong> analyzedSongs;
        public List<AnalyzedSong> AnalyzedSongs
        {
            get { return analyzedSongs; }
            set
            {
                analyzedSongs = value;
                OnPropertyChanged("AnalyzedSongs");
            }
        }

        private List<string> analyzedSongIds;
        public List<string> AnalyzedSongIds
        {
            get { return analyzedSongIds; }
            set
            {
                analyzedSongIds = value;
                OnPropertyChanged("AnalyzedSongIds");
            }
        }

        private bool analyzedSongsLoaded;
        public bool AnalyzedSongsLoaded
        {
            get { return analyzedSongsLoaded; }
            set
            {
                analyzedSongsLoaded = value;
                OnPropertyChanged("AnalyzedSongsLoaded");
            }
        }

        private List<AnalyzedSong> songsToAnalyze;
        public List<AnalyzedSong> SongsToAnalyze
        {
            get { return songsToAnalyze; }
            set
            {
                songsToAnalyze = value;
                OnPropertyChanged("SongsToAnalyze");
            }
        }

        private List<string> songsToAnalyzeIds;
        public List<string> SongsToAnalyzeIds
        {
            get { return songsToAnalyzeIds; }
            set
            {
                songsToAnalyzeIds = value;
                OnPropertyChanged("SongsToAnalyzeIds");
            }
        }

        private bool songsToAnalyzeLoaded;
        public bool SongsToAnalyzeLoaded
        {
            get { return songsToAnalyzeLoaded; }
            set
            {
                songsToAnalyzeLoaded = value;
                OnPropertyChanged("SongsToAnalyzeLoaded");
            }
        }

        private bool songsToAnalyzeBatchUploadReady;
        public bool SongsToAnalyzeBatchUploadReady
        {
            get { return songsToAnalyzeBatchUploadReady; }
            set
            {
                songsToAnalyzeBatchUploadReady = value;
                OnPropertyChanged("SongsToAnalyzeBatchUploadReady");
            }
        }

        private bool songsToAnalyzeBatchDownloadReady;
        public bool SongsToAnalyzeBatchDownloadReady
        {
            get { return songsToAnalyzeBatchDownloadReady; }
            set
            {
                songsToAnalyzeBatchDownloadReady = value;
                OnPropertyChanged("SongsToAnalyzeBatchDownloadReady");
            }
        }




        public DataModel()
        {
            SongsOnDevice = new List<AnalyzedSong>();
            SongsOnDeviceLoaded = false;

            AnalyzedSongs = new List<AnalyzedSong>();
            AnalyzedSongIds = new List<string>();
            AnalyzedSongsLoaded = false;

            SongsToAnalyze = new List<AnalyzedSong>();
            SongsToAnalyzeIds = new List<string>();
            SongsToAnalyzeLoaded = false;

            CatalogId = String.Empty;
        }

        private EchoNestApi CreateApiInstance()
        {
            return new EchoNestApi("R2O4VVBVN5EFMCJRP", false);
        }

        public void GetSongsOnDevice(object state)
        {
            Dictionary<string, AnalyzedSong> uniqueSongs =
                new Dictionary<string, AnalyzedSong>();

            using (var mediaLib = new XnaMediaLibrary())
            {
                foreach (XnaSong s in mediaLib.Songs)
                {
                    string id = string.Concat(s.Artist.Name, s.Name)
                        .Replace(" ", "");
                    uniqueSongs[id] = new AnalyzedSong
                    {
                        ItemId = id,
                        ArtistName = s.Artist.Name,
                        SongName = s.Name
                    };

                }

                SongsOnDevice.AddRange(uniqueSongs.Values
                    .OrderBy(s => s.ArtistName)
                    .ThenBy(s => s.SongName));
            }

            SongsOnDeviceLoaded = true;
        }

        public void GetAnalyzedSongs(object state)
        {
            using (BeatMachineDataContext context = new BeatMachineDataContext(
                BeatMachineDataContext.DBConnectionString))
            {
                context.DeferredLoadingEnabled = false;
                var loadedSongs = from AnalyzedSong song in context.AnalyzedSongs
                                  select song;

                AnalyzedSongs.AddRange(loadedSongs);
                AnalyzedSongIds.AddRange(
                    AnalyzedSongs.Select<AnalyzedSong, string>((x) => x.ItemId));
                AnalyzedSongsLoaded = true;
            }
        }

        public void DiffSongs(object state)
        {
            if (AnalyzedSongs.Count != SongsOnDevice.Count)
            {
                foreach (AnalyzedSong song in SongsOnDevice)
                {
                    if (!AnalyzedSongIds.Contains(song.ItemId))
                    {
                        SongsToAnalyze.Add(song);
                        SongsToAnalyzeIds.Add(song.ItemId);
                    }
                }

                // Stop at this point if everything is analyzed
                if (SongsToAnalyze.Count != 0)
                {
                    SongsToAnalyzeLoaded = true;
                }
            }
        }


        public void AnalyzeSongs(object state)
        {
            if (SongsToAnalyzeBatchDownloadReady)
            {
                SongsToAnalyzeBatchDownloadReady = false;
            }

            EchoNestApi api = CreateApiInstance();

            api.CatalogUpdateCompleted += new EventHandler<EchoNestApiEventArgs>(
                AnalyzeSongs_CatalogUpdateCompleted);

            List<CatalogAction<Song>> list = SongsToAnalyze
                .Skip(uploadSkip * uploadTake)
                .Take(uploadTake)
                .Select<AnalyzedSong, CatalogAction<Song>>(
                (s) =>
                {
                    return new CatalogAction<Song>
                    {
                        Item = (Song)s
                    };
                })
                .ToList();

            if (list.Count != 0)
            {
                api.CatalogUpdateAsync(new Catalog
                {
                    Id = CatalogId,
                    SongActions = list,
                }, null, null);
            }
            else
            {
                SongsToAnalyzeBatchUploadReady = true;
            }
        }

        void AnalyzeSongs_CatalogUpdateCompleted(object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                uploadSkip++;
            }
            AnalyzeSongsNeedsToRunAgain();
        }

        private void AnalyzeSongsNeedsToRunAgain()
        {
            ExecutionQueue.Enqueue(new WaitCallback(AnalyzeSongs),
                ExecutionQueue.Policy.Queued);
        }


        public void DownloadAnalyzedSongsAlreadyInRemoteCatalog(object state)
        {
            EchoNestApi api = CreateApiInstance();
            api.CatalogReadCompleted += new EventHandler<EchoNestApiEventArgs>(
                DownloadAnalyzedSongsAlreadyInRemoteCatalog_CatalogReadCompleted);

            api.CatalogReadAsync(CatalogId,
                new Dictionary<string, string>
                {
                    {"bucket", "audio_summary"},
                    {"results", downloadTake.ToString()},
                    {"start", (downloadSkip * downloadTake).ToString()} 
                 }, null);
        }

        void DownloadAnalyzedSongsAlreadyInRemoteCatalog_CatalogReadCompleted(
            object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                Catalog cat = (Catalog)e.GetResultData();

                if (cat.Items.Count > 0)
                {
                    var analyzedSongs = cat.Items.
                        Select<Song, AnalyzedSong>(s => new AnalyzedSong
                        {
                            ItemId = s.Request.ItemId,
                            ArtistName = s.ArtistName ?? s.Request.ArtistName,
                            SongName = s.SongName ?? s.Request.SongName,
                            AudioSummary = s.AudioSummary != null ?
                            new AnalyzedSong.Summary
                            {
                                Tempo = s.AudioSummary.Tempo,
                                ItemId = s.Request.ItemId
                            } : null
                        }).
                        Where<AnalyzedSong>(s => SongsToAnalyzeIds.Contains(s.ItemId));


                    if (analyzedSongs.Count<AnalyzedSong>() > 0)
                    {
                        using (BeatMachineDataContext context = new BeatMachineDataContext(
                            BeatMachineDataContext.DBConnectionString))
                        {
                            context.AnalyzedSongs.InsertAllOnSubmit(analyzedSongs);
                            context.SubmitChanges();
                        }

                        foreach (AnalyzedSong s in analyzedSongs)
                        {
                            SongsToAnalyze.Remove(
                                SongsToAnalyze.Where(x => String.Equals(x.ItemId, s.ItemId)).First());
                            SongsToAnalyzeIds.Remove(s.ItemId);
                        }
                    }
                }

                if (cat.Items.Count == downloadTake)
                {
                    // We grabbed a full downloadTake number of items, so
                    // there must be more items
                    downloadSkip++;
                    DownloadAnalyzedSongsAlreadyInRemoteCatalogNeedsToRunAgain();
                }
                else
                {
                    // If we grabbed less than that, then there are no more items
                    SongsToAnalyzeBatchDownloadReady = true;

                }
            }
            else
            {
                DownloadAnalyzedSongsAlreadyInRemoteCatalogNeedsToRunAgain();
            }
        }

        private void DownloadAnalyzedSongsAlreadyInRemoteCatalogNeedsToRunAgain()
        {
            ExecutionQueue.Enqueue(
                new WaitCallback(DownloadAnalyzedSongsAlreadyInRemoteCatalog),
               ExecutionQueue.Policy.Queued);
        }
    
        public void DownloadAnalyzedSongs(object state)
        {
            if (SongsToAnalyzeBatchUploadReady)
            {
                // First time around in this batch download
                SongsToAnalyzeBatchUploadReady = false;
                downloadSkip = 0;
            };

            if (SongsToAnalyze.Count != 0)
            {
                EchoNestApi api = CreateApiInstance();
                api.CatalogReadCompleted += new EventHandler<EchoNestApiEventArgs>(
                    DownloadAnalyzedSongs_CatalogReadCompleted);

                api.CatalogReadAsync(CatalogId, new Dictionary<string, string>{
                    {"bucket", "audio_summary"},
                    {"results", downloadTake.ToString()},
                    {"start", (downloadSkip * downloadTake).ToString()}
                }, null);
            }
        }

        void DownloadAnalyzedSongs_CatalogReadCompleted(
            object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                Catalog cat = (Catalog)e.GetResultData();

                if (cat.Items.Count > 0)
                {
                    var analyzedSongs = cat.Items.
                        Select<Song, AnalyzedSong>(s => new AnalyzedSong
                        {
                            ItemId = s.Request.ItemId,
                            ArtistName = s.ArtistName ?? s.Request.ArtistName,
                            SongName = s.SongName ?? s.Request.SongName,
                            AudioSummary = s.AudioSummary != null ?
                            new AnalyzedSong.Summary
                            {
                                Tempo = s.AudioSummary.Tempo,
                                ItemId = s.Request.ItemId
                            } : null
                        }).
                        Where<AnalyzedSong>(s => SongsToAnalyzeIds.Contains(s.ItemId));


                    if (analyzedSongs.Count<AnalyzedSong>() > 0)
                    {
                        using (BeatMachineDataContext context = new BeatMachineDataContext(
                            BeatMachineDataContext.DBConnectionString))
                        {
                            context.AnalyzedSongs.InsertAllOnSubmit(analyzedSongs);
                            context.SubmitChanges();
                        }

                        foreach (AnalyzedSong s in analyzedSongs)
                        {
                            SongsToAnalyze.Remove(
                               SongsToAnalyze.Where(x => String.Equals(x.ItemId, s.ItemId)).First());
                            SongsToAnalyzeIds.Remove(s.ItemId);
                        }
                    }

                    if (cat.Items.Count == downloadTake)
                    {
                        downloadSkip++;
                    }
                    else
                    {
                        downloadSkip = 0;
                    }
                }
            }
            DownloadAnalyzedSongsNeedsToRunAgain();
        }

        private void DownloadAnalyzedSongsNeedsToRunAgain()
        {
            ExecutionQueue.Enqueue(new WaitCallback(DownloadAnalyzedSongs),
                ExecutionQueue.Policy.Queued);
        }

   
        /// <summary>
        /// Ensures Model.CatalogId is populated. Will try the following:
        /// 1. Try loading it from the "CatalogId" setting in storage
        /// 2. If (1) is successful, it will try reading 1 song from the 
        /// catalog to make sure it is there on the web service
        /// 3. If (1) or (2) fails, it will create a new catalog on the 
        /// web service
        /// </summary>
        public void LoadCatalogId(object state)
        {
            bool loadedId = false;
            string id;

            if (String.IsNullOrEmpty(CatalogId))
            {
                if (IsolatedStorageSettings.ApplicationSettings.
                    TryGetValue<string>("CatalogId", out id))
                {
                    loadedId = true;
                }
            }
            else
            {
                loadedId = true;
                id = CatalogId;
            }

            if (!loadedId)
            {
                LoadCatalogIdNeedsToCreateCatalog();
            }
            else
            {
                LoadCatalogIdNeedsToCheckCatalogId(id);
            }   
        }

        private void LoadCatalogIdNeedsToCreateCatalog()
        {
            EchoNestApi api = CreateApiInstance();
            api.CatalogCreateCompleted += new EventHandler<EchoNestApiEventArgs>(
                LoadCatalogIdNeedsToCreateCatalog_CatalogCreateCompleted);
            api.CatalogCreateAsync(Guid.NewGuid().ToString(), "song", 
                null, null);
        }

        void LoadCatalogIdNeedsToCreateCatalog_CatalogCreateCompleted(
            object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                Catalog cat = (Catalog)e.GetResultData();

                // TODO If isolated storage fails here, then the next time they 
                // run the app, we will create another catalog. We need to handle
                // this somehow - perhaps use the unique device ID (which is bad
                // practice apparently) as the catalog name to make sure there is
                // only one catalog created per device ever.

                // Store in isolated storage
                IsolatedStorageSettings.ApplicationSettings["CatalogId"] =
                    cat.Id;
                IsolatedStorageSettings.ApplicationSettings.Save();

                CatalogId = cat.Id;

            }
            else
            {
                // Couldn't create successfully, try again later
                LoadCatalogIdNeedsToRunAgain();
            }
        }

        private void LoadCatalogIdNeedsToCheckCatalogId(string id)
        {
            EchoNestApi api = CreateApiInstance();
            api.CatalogUpdateCompleted +=
                new EventHandler<EchoNestApiEventArgs>(
                    LoadCatalogIdNeedsToCheckCatalogId_CatalogUpdateCompleted);

            // Issue dummy update to make sure it's there
            api.CatalogUpdateAsync(new Catalog
                {
                    Id = id,
                    SongActions = new List<CatalogAction<Song>>
                    {
                        new CatalogAction<Song>
                        {
                            Action = CatalogAction<Song>.ActionType.delete,
                            Item = new Song {
                                ItemId = "dummy"
                            }
                        }
                    }
                }, null, id);
        }

        void LoadCatalogIdNeedsToCheckCatalogId_CatalogUpdateCompleted(
            object sender, EchoNestApiEventArgs e)
        {
            if (e.Error != null)
            {
                if (e.Error is WebExceptionWrapper)
                {
                    // Transient network error
                    LoadCatalogIdNeedsToRunAgain();
                }
                else if (e.Error is EchoNestApiException)
                {
                    EchoNestApiException er = e.Error as EchoNestApiException;
                    if (er.Code == EchoNestApiException.EchoNestApiExceptionType.InvalidParameter &&
                        String.Equals(er.Message, "This catalog does not exist"))
                    {
                        // They either didn't have a CatalogId in local storage, or
                        // they did have one, but it wasn't on the cloud service. In
                        // both cases, we create a new one
                        LoadCatalogIdNeedsToCreateCatalog();
                    }
                }

            }
            else
            {
                // This catalog exists, everything is great 
                CatalogId = (string)e.UserState;
            }
        }

        private void LoadCatalogIdNeedsToRunAgain()
        {
            ExecutionQueue.Enqueue(new WaitCallback(LoadCatalogId),
                ExecutionQueue.Policy.Queued);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}