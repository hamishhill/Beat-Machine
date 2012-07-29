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

// TODO Add a logger for debuggin

// TODO Figure out of all the locks are needed

// TODO Unit tests


namespace BeatMachine.Model
{
    public class DataModel : INotifyPropertyChanged
    {
        private static int uploadTake = 300;
        private static int uploadSkip = 0;

        // More than 100 will fail due to EchoNest hardcoded limit
        private static int downloadTake = 100;
        private static int downloadSkip = 0;
        

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

        private int songsToAnalyzeBatchSize;
        public int SongsToAnalyzeBatchSize
        {
            get { return songsToAnalyzeBatchSize; }
            set
            {
                songsToAnalyzeBatchSize = value;
                OnPropertyChanged("SongsToAnalyzeBatchSize");
            }
        }

        private List<AnalyzedSong> analyzedSongs;
        public List<AnalyzedSong> AnalyzedSongs
        {
            get { return analyzedSongs; }
            set {
                analyzedSongs = value;
                OnPropertyChanged("AnalyzedSongs"); 
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

        public DataModel()
        {
            SongsOnDevice = new List<AnalyzedSong>();
            SongsOnDeviceLoaded = false;

            AnalyzedSongs = new List<AnalyzedSong>();
            AnalyzedSongsLoaded = false;

            SongsToAnalyze = new List<AnalyzedSong>();
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

            lock (SongsOnDevice)
            {
                using (var mediaLib = new XnaMediaLibrary())
                {
                    foreach(XnaSong s in mediaLib.Songs)
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
        }

        public void GetAnalyzedSongs(object state)
        {
            using (BeatMachineDataContext context = new BeatMachineDataContext(
                BeatMachineDataContext.DBConnectionString))
            {
                lock (AnalyzedSongs)
                {
                    var loadedSongs = from AnalyzedSong song in context.AnalyzedSongs
                                      select song;

                    AnalyzedSongs.AddRange(loadedSongs.ToArray<AnalyzedSong>());
                    AnalyzedSongsLoaded = true;
                }
            }


        }

        public void DiffSongs(object state)
        {         
            lock (AnalyzedSongs)
            {       
                lock (SongsOnDevice)
                {
                    lock (SongsToAnalyze)
                    {
                        List<string> analyzedSongIds = AnalyzedSongs
                            .Select<AnalyzedSong, string>((x) => x.SongId).ToList<string>();

                        if (AnalyzedSongs.Count != SongsOnDevice.Count)
                        {
                            foreach (AnalyzedSong song in SongsOnDevice)
                            {
                                if (!analyzedSongIds.Contains(song.SongId))
                                {
                                    SongsToAnalyze.Add(song);
                                }
                            }

                            SongsToAnalyzeLoaded = true;
                        }
                    }
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

            lock (SongsToAnalyze)
            {
                api.CatalogUpdateCompleted += new EventHandler<EchoNestApiEventArgs>(Api_CatalogUpdateCompleted);

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

                SongsToAnalyzeBatchSize = list.Count;

                if (SongsToAnalyzeBatchSize != 0)
                {
                    api.CatalogUpdateAsync(new Catalog
                    {
                        Id = CatalogId,
                        SongActions = list,
                    }, null, null);
                }
            } 
        }

        void Api_CatalogUpdateCompleted(object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                uploadSkip++;
                SongsToAnalyzeBatchUploadReady = true;

            }
            else
            {
                // AnalyzeSongs needs to run again
                ExecutionQueue.Enqueue(new WaitCallback(AnalyzeSongs),
                    ExecutionQueue.Policy.Queued);
            }
        }
    

        public void DownloadAnalyzedSongs(object state)
        {
            if (SongsToAnalyzeBatchUploadReady)
            {
                // First time around in this batch download
                downloadSkip = 0;
                SongsToAnalyzeBatchUploadReady = false;
            };

            EchoNestApi api = CreateApiInstance();
            api.CatalogReadCompleted += new EventHandler<EchoNestApiEventArgs>(Api_CatalogReadCompleted);
            
            api.CatalogReadAsync(CatalogId,
                new Dictionary<string, string>
            {
                {"bucket", "audio_summary"},
                {"results", downloadTake.ToString()},
                {"start", downloadSkip.ToString()} 

            }, null);
        }

        void Api_CatalogReadCompleted(object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                Catalog cat = (Catalog)e.GetResultData();

                using (BeatMachineDataContext context = new BeatMachineDataContext(
                    BeatMachineDataContext.DBConnectionString))
                {
                    // TODO This check doesn't work well, it won't terminate 
                    // especially in the case where the catalog has more items that
                    // the client doesn't know about

                    if (!(cat.Items.Count == 0 &&
                        context.AnalyzedSongs.Count() >=
                        SongsToAnalyzeBatchSize))
                    {
                        context.AnalyzedSongs.InsertAllOnSubmit(
                            cat.Items.Select<Song, AnalyzedSong>(
                            s => new AnalyzedSong
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
                            }
                           ));
                        context.SubmitChanges();

                        downloadSkip = context.AnalyzedSongs.Count();

                        DownloadAnalyzedSongsNeedsToRunAgain();
                    }
                    else
                    {
                        SongsToAnalyzeBatchDownloadReady = true;
                    }
                }
            } else {
                DownloadAnalyzedSongsNeedsToRunAgain();
            }
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

            lock (CatalogId)
            {
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
            api.CatalogCreateCompleted += new EventHandler<EchoNestApiEventArgs>(Api_CatalogCreateCompleted);
            api.CatalogCreateAsync(Guid.NewGuid().ToString(), "song", 
                null, null);
        }

        void Api_CatalogCreateCompleted(object sender, EchoNestApiEventArgs e)
        {
            if (e.Error == null)
            {
                Catalog cat = (Catalog)e.GetResultData();

                // TODO If isolated storage fails here, then the next time they 
                // run the app, we will create another catalog. We need to handle
                // this somehow - perhaps use the unique device ID (which is bad
                // practice apparently) as the catalog name to make sure there is
                // only one catalog created per device ever.

                lock (CatalogId)
                {
                    // Store in isolated storage
                    IsolatedStorageSettings.ApplicationSettings["CatalogId"] =
                        cat.Id;
                    IsolatedStorageSettings.ApplicationSettings.Save();

                    CatalogId = cat.Id;
                }
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
                new EventHandler<EchoNestApiEventArgs>(Api_CatalogUpdateCompleted1);

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

        void Api_CatalogUpdateCompleted1(object sender,
            EchoNestApiEventArgs e)
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
                lock (CatalogId)
                {
                    CatalogId = (string)e.UserState;
                }
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
