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
using System.Collections.Generic;
using BeatMachine.EchoNest.Model;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.ComponentModel;

namespace BeatMachine.EchoNest
{
    public class EchoNestApi
    {
        private const string baseUrl = "http://developer.echonest.com/api/v4";
        
        public event EventHandler<EchoNestApiEventArgs> SongSearchCompleted;
        public event EventHandler<EchoNestApiEventArgs> CatalogCreateCompleted;
        public event EventHandler<EchoNestApiEventArgs> CatalogListCompleted;
        public event EventHandler<EchoNestApiEventArgs> CatalogUpdateCompleted;
        public event EventHandler<EchoNestApiEventArgs> CatalogStatusCompleted;
        public event EventHandler<EchoNestApiEventArgs> CatalogReadCompleted;

        // For multipart POST requests
        private const string MultipartPrefix = "--";
        private const string MultipartBoundary = "----------bmlolz";
        private const string MultipartNewLine = "\r\n";

        private static class EchoNestPaths
        {
            public const string SongSearch = "/song/search";
            public const string CatalogCreate = "/catalog/create";
            public const string CatalogList = "/catalog/list";
            public const string CatalogUpdate = "/catalog/update";
            public const string CatalogStatus = "/catalog/status";
            public const string CatalogRead = "/catalog/read";
        }

        public string ApiKey
        {
            get; set;

        }

        public EchoNestApi(string apiKey)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// Search for a song as documented here 
        /// http://developer.echonest.com/docs/v4/song.html#search 
        /// </summary>
        /// <param name="parameters">See link for list of parameters</param>
        public void SongSearchAsync(Dictionary<string, string> parameters)
        {
            SendHttpRequest(EchoNestPaths.SongSearch, parameters, null);
        }

        /// <summary>
        /// Create a catalog as documented here
        /// http://developer.echonest.com/docs/v4/catalog.html#create
        /// </summary>
        /// <param name="name">The name of the catalog</param>
        /// <param name="type">The type of the catalog, values are "artist" or 
        /// "song"</param>
        /// <param name="parameters">See link for list of parameters</param>
        public void CatalogCreateAsync(string name, string type, 
            Dictionary<string, string> parameters)
        {
            InitializeParameters(ref parameters);
            parameters["name"] = name;
            parameters["type"] = type;

            // TODO Add support for artist catalogs

            SendHttpRequest(EchoNestPaths.CatalogCreate, parameters, null);
        }

        /// <summary>
        /// Lists catalogs for this API key as documented here
        /// http://developer.echonest.com/docs/v4/catalog.html#list
        /// </summary>
        /// <param name="parameters">See link for list of parameters</param>
        public void CatalogListAsync(Dictionary<string, string> parameters)
        {
            SendHttpRequest(EchoNestPaths.CatalogList, parameters, null);
        }

        /// <summary>
        /// Updates a catalog as documented here
        /// http://developer.echonest.com/docs/v4/catalog.html#update
        /// </summary>
        /// <param name="cat">Catalog to update, which should have at least the
        /// Id set, as well as either the SongActions or ArtistActions 
        /// properties. An update can either include songs or artists, but not 
        /// both, so one of these two has to be either null or contain 0 
        /// members.</param>
        /// <param name="parameters">See link for list of parameters</param>
        public void CatalogUpdateAsync(Catalog cat, 
            Dictionary<string, string> parameters)
        {
            InitializeParameters(ref parameters);
            parameters["id"] = cat.Id;
            parameters["data_type"] = "json";

            // TODO Add support for artist catalogs
            SendHttpRequest(EchoNestPaths.CatalogUpdate, parameters, 
                cat.SongActions);
        }

        /// <summary>
        /// Gets the status of a catalog update as described here
        /// http://developer.echonest.com/docs/v4/catalog.html#status
        /// </summary>
        /// <param name="ticket">The ticket obtained from calling the 
        /// CatalogUpdate method</param>
        /// <param name="parameters">See link for list of parameters</param>
        public void CatalogStatusAsync(string ticket,
            Dictionary<string, string> parameters)
        {
            InitializeParameters(ref parameters);
            parameters["ticket"] = ticket;
            SendHttpRequest(EchoNestPaths.CatalogStatus, parameters, null);

        }

        /// <summary>
        /// Reads the contents of a catalog as described here
        /// http://developer.echonest.com/docs/v4/catalog.html#read
        /// </summary>
        /// <param name="catalogId">The catalog ID</param>
        /// <param name="parameters">See link for list of parameters</param>
        public void CatalogReadAsync(string catalogId,
            Dictionary<string, string> parameters)
        {
            InitializeParameters(ref parameters);
            parameters["id"] = catalogId;
            SendHttpRequest(EchoNestPaths.CatalogRead, parameters, null);
        }


        private void InitializeParameters(
            ref Dictionary<string, string> parameters)
        {
            if(parameters == null)
            {
                parameters = new Dictionary<string,string>();
            }
        }

        /// <summary>
        /// For HTTP calls, we use the HttpHelper class, which is nice since 
        /// it provides consistency across platforms. However we need some
        /// functionality on top of pure HTTP, such as adding the API key and
        /// throttling calls, so we wrap all requests through this method.
        /// </summary>
        private void SendHttpRequest(string path, 
            Dictionary<string, string> parameters, object body)
        {
            InitializeParameters(ref parameters);
            parameters["api_key"] = ApiKey;
            parameters["format"] = "json";
            StringBuilder sb = new StringBuilder(baseUrl)
                .Append(path);

            // TODO Throttle calls

            // Switch to GET or POST depending on which method we are about
            // to call
            bool isGet = true;
            switch (path)
            {
                case EchoNestPaths.SongSearch:
                case EchoNestPaths.CatalogList:
                case EchoNestPaths.CatalogStatus:
                case EchoNestPaths.CatalogRead:
                    break;

                case EchoNestPaths.CatalogCreate:
                case EchoNestPaths.CatalogUpdate:
                    isGet = false;
                    break;
            }

            HttpHelper h;
            if (isGet)
            {
                // If it is a GET, the parameters go in the query string
                sb.Append(CreateQueryString(parameters));
                h = new HttpHelper(sb.ToString());
            }
            else
            {
                // If it is a POST, the parameters will be a part of the body,
                // which happens a little further down. The body will be 
                // encoded using multipart/form-data, so here we need to 
                // specify the divider that will be used in the body to
                // separate the name-value pairs 
                h = new HttpHelper(sb.ToString());
                h.HttpWebRequest.Method = "POST";
                h.HttpWebRequest.ContentType = String.Concat(
                    "multipart/form-data; boundary=", MultipartBoundary);
            }

            h.OpenReadCompleted += new OpenReadCompletedEventHandler(
                SendHttpRequestOpenReadCompleted);

            if (isGet)
            {
                // No body to write first, just read the response
                h.OpenReadAsync(path);
            }
            else
            {
                // Write the request body (the response will be read once
                // SendHttpRequestOpenWriteCompleted fires)
                h.OpenWriteCompleted += new OpenWriteCompletedEventHandler(
                    SendHttpRequestOpenWriteCompleted);
                h.OpenWriteAsync(new Dictionary<string, object>
                {
                    {"path", path},
                    {"parameters", parameters},
                    {"body", body}
                });

            }

        }

        /// <summary>
        /// Handler for writing out the request body if needed
        /// </summary>
        void SendHttpRequestOpenWriteCompleted(object sender, 
            OpenWriteCompletedEventArgs e)
        {
            EchoNestApiEventArgs args = null;
            Dictionary<string, object> state = 
                (Dictionary<string, object>)e.UserState;
            string path = (string)state["path"];
            object body = state["body"];
            Dictionary<string, string> parameters = 
                (Dictionary<string, string>)state["parameters"];
            HttpHelper h = (HttpHelper)sender;

            if (e.Cancelled)
            {
                args = new EchoNestApiEventArgs(e.Error, true, null, null);
            }
            else if (e.Error == null)
            {
                try
                { 
                    using (var stream = e.Result)
                    {

                        parameters["data"] = JsonConvert.SerializeObject(
                            body, new JsonSerializerSettings
                            {
                                DefaultValueHandling =
                                DefaultValueHandling.Ignore
                            });

                        StreamWriter writer = new StreamWriter(stream);
                        writer.Write(CreateMultipartForm(parameters));
                        writer.Flush();
                        
                    }

                    h.OpenReadAsync(path);
                    return;
                }
                catch (Exception ex)
                {
                    args = new EchoNestApiEventArgs(ex,
                        h.HttpWebRequest.IsCancelled, null, null);
                }
            }
            else
            {
                var ex = e.Error as WebExceptionWrapper;
                if (ex != null)
                {
                    if (ex.GetResponse() != null)
                    {
                        h.OpenReadAsync(path);
                        return;
                    }
                }

                args = new EchoNestApiEventArgs(e.Error, false, null, null);
                
            }

            OnCompleted(path, args);
        }

        private string CreateQueryString(Dictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder("?");
            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    sb.Append("&")
                        .Append(Uri.EscapeUriString(key))
                        .Append("=")
                        .Append(Uri.EscapeUriString(parameters[key]));
                }
            }

            return sb.ToString();
        }

        private string CreateMultipartForm(
            Dictionary<string, string> parameters)
        {
            StringBuilder sb = new StringBuilder();

            if (parameters != null)
            {
                foreach (string key in parameters.Keys)
                {
                    sb.Append(MultipartPrefix)
                        .Append(MultipartBoundary)
                        .Append(MultipartNewLine)
                        .AppendFormat(
                        "Content-Disposition: form-data; name=\"{0}\"", key);

                    if(String.Compare(key, "data") == 0)
                    {
                        sb.Append("; filename=\"update.json\"")
                            .Append(MultipartNewLine)
                            .Append("Content-Type: application/octet-stream");
                    }
                    
                    sb.Append(MultipartNewLine)
                        .Append(MultipartNewLine)
                        .Append(parameters[key])
                        .Append(MultipartNewLine);
                }
                sb.Append(MultipartPrefix)
                    .Append(MultipartBoundary)
                    .Append(MultipartPrefix)
                    .Append(MultipartNewLine);

            }

            return sb.ToString();

        }

        /// <summary>
        /// Handler for the response body
        /// </summary>
        void SendHttpRequestOpenReadCompleted(object sender,
            OpenReadCompletedEventArgs e)
        {
            EchoNestApiEventArgs args = null;
            string path = (string)e.UserState;
            HttpHelper h = (HttpHelper)sender;

            // TODO catch exception if network is not present

            if (e.Cancelled)
            {
                args = new EchoNestApiEventArgs(e.Error, true, null, null);
            }
            else if (e.Error == null)
            {
                try
                {
                    string responseString;
                    using (var stream = e.Result)
                    {
                        // TODO Handle 304 response?
                        // var response = h.HttpWebResponse;

                        using (var reader = new StreamReader(stream))
                        {
                            responseString = reader.ReadToEnd();
                        }
                    }

                    if (!ResponseContainsError(responseString, out args))
                    {
                        args = HandleResponse(path, responseString);
                    }

                }
                catch (Exception ex)
                {
                    args = new EchoNestApiEventArgs(ex, 
                        h.HttpWebRequest.IsCancelled, null, null);
                }

            }
            else
            {

                var ex = e.Error as WebExceptionWrapper;
                var response = ex.GetResponse();
                bool returnGenericError = true;

                if (response != null)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            string responseString;
                            using (var reader = new StreamReader(stream))
                            {
                                responseString = reader.ReadToEnd();
                            }

                            if (ResponseContainsError(responseString,
                                out args))
                            {
                                returnGenericError = false;
                            }
                        }
                    }
                }

                if (returnGenericError)
                {
                    args = new EchoNestApiEventArgs(ex, false, null, null);
                }
            }

            OnCompleted(path, args);
        }

        /// <summary>
        /// Regardless whether the response status code is 200 or something else, 
        /// the response could still contain an error if the "code" property in the
        /// JSON response is not "0", so check for that.
        /// </summary>
        /// <param name="args">Null if no error, otherwise will contain error
        /// information</param>
        /// <returns>True if the response contains an error status</returns>
        private bool ResponseContainsError(string responseString, 
            out EchoNestApiEventArgs args)
        {
            args = null;

            try
            {
                JToken jo =
                    JObject.Parse(responseString)["response"]["status"];
                int code = (int)jo["code"];
                if (code != 0)
                {
                    EchoNestApiException enx = new EchoNestApiException(
                        (EchoNestApiException.EchoNestApiExceptionType)code,
                        (string)jo["message"]);
                    args = new EchoNestApiEventArgs(enx, false, null, null);
                    return true;
                }
            }
            catch (Exception)
            {
                // If we're here it means we weren't able to extract a status
                // code out of the response
                return false;
            }

            return false;
        }


        /// <summary>
        /// We want to have convenience methods such as SongSearchAsync, but 
        /// they all go through the same SendHttpRequest method. We do 
        /// a simple trick to demultiplex the response handling for different 
        /// methods.
        /// </summary>
        private EchoNestApiEventArgs HandleResponse(string path, string response)
        {
            EchoNestApiEventArgs args = null;
            switch (path)
            {
                case EchoNestPaths.SongSearch:
                    args = HandleSongSearchResponse(response);
                    break;
                case EchoNestPaths.CatalogCreate:
                    args = HandleCatalogCreateResponse(response);
                    break;
                case EchoNestPaths.CatalogList:
                    args = HandleCatalogListResponse(response);
                    break;
                case EchoNestPaths.CatalogUpdate:
                    args = HandleCatalogUpdateResponse(response);
                    break;
                case EchoNestPaths.CatalogStatus:
                    args = HandleCatalogStatusResponse(response);
                    break;
                case EchoNestPaths.CatalogRead:
                    args = HandleCatalogReadResponse(response);
                    break;
            }

            return args;
        }

        private EchoNestApiEventArgs HandleCatalogUpdateResponse(
            string response)
        {
            JToken jo = JObject.Parse(response)["response"];
            return new EchoNestApiEventArgs(null, false, null,
                (string)jo["ticket"]);
        }

        private EchoNestApiEventArgs HandleSongSearchResponse(string response)
        {
            JToken jo = JObject.Parse(response)["response"]["songs"];
            List<Song> result = 
                JsonConvert.DeserializeObject<List<Song>>(jo.ToString());
            return new EchoNestApiEventArgs(null, false, null, result);

        }

        private EchoNestApiEventArgs HandleCatalogCreateResponse(string response)
        {
            JToken jo = JObject.Parse(response)["response"];
            Catalog result = JsonConvert.DeserializeObject<Catalog>(jo.ToString());
            return new EchoNestApiEventArgs(null, false, null, result);
        }

        private EchoNestApiEventArgs HandleCatalogListResponse(string response)
        {
            JToken jo = JObject.Parse(response)["response"]["catalogs"];
            List<Catalog> result =
                JsonConvert.DeserializeObject<List<Catalog>>(jo.ToString());
            return new EchoNestApiEventArgs(null, false, null, result);

        }

        private EchoNestApiEventArgs HandleCatalogStatusResponse(string response)
        {
            JToken jo = JObject.Parse(response)["response"];
            Ticket result = JsonConvert.DeserializeObject<Ticket>(
                jo.ToString());
            return new EchoNestApiEventArgs(null, false, null, result);
        }

        private EchoNestApiEventArgs HandleCatalogReadResponse(string response)
        {
            JToken jo = JObject.Parse(response)["response"]["catalog"];
            Catalog result = JsonConvert.DeserializeObject<Catalog>(
                jo.ToString());
            return new EchoNestApiEventArgs(null, false, null, result);


        }

        /// <summary>
        /// Demux callbacks from SendHttpRequest method.
        /// </summary>
        private void OnCompleted(string path, EchoNestApiEventArgs args)
        {
            EventHandler<EchoNestApiEventArgs> e = null;
            switch (path)
            {
                case EchoNestPaths.SongSearch:
                    e = SongSearchCompleted;
                    break;
                case EchoNestPaths.CatalogCreate:
                    e = CatalogCreateCompleted;
                    break;
                case EchoNestPaths.CatalogList:
                    e = CatalogListCompleted;
                    break;
                case EchoNestPaths.CatalogUpdate:
                    e = CatalogUpdateCompleted;
                    break;
                case EchoNestPaths.CatalogStatus:
                    e = CatalogStatusCompleted;
                    break;
                case EchoNestPaths.CatalogRead:
                    e = CatalogReadCompleted;
                    break;
            }

            if (e != null)
            {
                // HttpWebRequest does its work on a background thread, 
                // so dispatch to the UI thread for convenience
                Deployment.Current.Dispatcher.BeginInvoke(() => e(this, args));
            }
        }

    }
}
