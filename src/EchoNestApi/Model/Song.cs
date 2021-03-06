﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Text;

namespace BeatMachine.EchoNest.Model
{
    public class Song
    {
        public class Summary
        {
            public enum Scale
            {
                C = 0,
                CSharp = 1,
                D = 2,
                DSharp = 3,
                E = 4,
                F = 5,
                FSharp = 6,
                G = 7,
                GSharp = 8,
                A = 9,
                ASharp = 10,
                B = 11,
                CPrime = 12
            }

            public enum AudioMode
            {
                Major = 0,
                Minor = 1
            }

            [JsonProperty(PropertyName = "key")]
            public virtual Scale? Key
            {
                get;
                set;
            }

            [JsonProperty(PropertyName = "tempo")]
            public virtual float? Tempo
            {
                get;
                set;
            }

            [JsonProperty(PropertyName = "analysis_url")]
            public virtual Uri AnalysisUrl
            {
                get;
                set;
            }

            [JsonProperty(PropertyName = "audio_md5")]
            public virtual string AudioMd5
            {
                get;
                set;
            }

            /// <summary>
            /// Range is from 0 to 1
            /// </summary>
            [JsonProperty(PropertyName = "danceability")]
            public virtual float? Danceability
            {
                get;
                set;
            }

            /// <summary>
            /// In seconds
            /// </summary>
            [JsonProperty(PropertyName = "duration")]
            public virtual float? Duration
            {
                get;
                set;
            }

            /// <summary>
            /// Range is from 0 to 1
            /// </summary>
            [JsonProperty(PropertyName = "energy")]
            public virtual float? Energy
            {
                get;
                set;
            }


            /// <summary>
            /// Measured in dB
            /// </summary>
            [JsonProperty(PropertyName = "loudness")]
            public virtual float? Loudness
            {
                get;
                set;
            }


            [JsonProperty(PropertyName = "mode")]
            public virtual AudioMode? Mode
            {
                get;
                set;
            }

            /// <summary>
            /// Beats per measure
            /// </summary>
            [JsonProperty(PropertyName = "time_signature")]
            public virtual int? TimeSignature
            {
                get;
                set;
            }
        }


        /// <summary>
        /// Use in search or catalog update methods
        /// </summary>
        [JsonProperty(PropertyName = "artist_id")]
        public virtual string ArtistId
        {
            get;
            set;
        }

        /// <summary>
        /// Use in search and catalog read methods
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public virtual string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Use in catalog update method
        /// </summary>
        [JsonProperty(PropertyName = "item_id")]
        public virtual string ItemId
        {
            get;
            set;
        }

        /// <summary>
        /// Use in search or catalog update method
        /// </summary>
        [JsonProperty(PropertyName = "artist_name")]
        public virtual string ArtistName
        {
            get;
            set;
        }

        /// <summary>
        /// Use in search or catalog update method
        /// </summary>
        [JsonProperty(PropertyName = "release")]
        public virtual string Release
        {
            get;
            set;
        }

        /// <summary>
        /// Use in search method
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public virtual string Title
        {
            get;
            set;
        }

        /// <summary>
        /// Use in catalog update method
        /// </summary>
        [JsonProperty(PropertyName = "song_name")]
        public virtual string SongName
        {
            get;
            set;
        }

        /// <summary>
        /// Use in catalog read method
        /// </summary>
        [JsonProperty(PropertyName = "song_id")]
        public virtual string SongId
        {
            get;
            set;
        }

        /// <summary>
        /// Use in catalog read method
        /// </summary>
        [JsonProperty(PropertyName = "audio_summary")]
        public Summary AudioSummary
        {
            get;
            set;
        }

        /// <summary>
        /// Use in catalog read method
        /// </summary>
        [JsonProperty(PropertyName = "request")]
        public virtual Song Request
        {
            get;
            set;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} by {1}", SongName ?? "?", ArtistName ?? "?");
            sb.AppendLine();
            if (AudioSummary != null)
            {
                sb.AppendFormat("BPM: {0} ", AudioSummary.Tempo);
                sb.AppendFormat("Dan: {0} ", AudioSummary.Danceability);
                sb.AppendFormat("Dur: {0} ", AudioSummary.Duration);
                sb.AppendFormat("Ene: {0} ", AudioSummary.Energy);
                sb.AppendFormat("Key: {0} ", AudioSummary.Key);
                sb.AppendFormat("Lou: {0} ", AudioSummary.Loudness);
                sb.AppendFormat("Mod: {0} ", AudioSummary.Mode);
                sb.AppendFormat("Sig {0} ", AudioSummary.TimeSignature);
            }
            return sb.ToString();
        }
    }
}