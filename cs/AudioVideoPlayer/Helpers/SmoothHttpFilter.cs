using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Web.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Web.Http.Filters;
using System.Runtime.InteropServices.WindowsRuntime;


namespace AudioVideoPlayer.Helpers
{
    public class SmoothHttpFilter : IHttpFilter
    {
        private IHttpFilter innerFilter;

        public SmoothHttpFilter(IHttpFilter innerFilter)
        {
            if (innerFilter == null)
            {
                throw new ArgumentException("innerFilter cannot be null.");
            }
            this.innerFilter = innerFilter;
        }
        public string GetManifestHeader(string manifest)
        {
            string result = string.Empty;
            if(!string.IsNullOrEmpty(manifest))
            {
                int pos = manifest.IndexOf("<StreamIndex");
                if (pos > 0)
                    return manifest.Substring(0, pos);
            }
            return result;
        }
        public string GetManifestTail(string manifest)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(manifest))
            {
                int lastPos = -1;
                int pos = -1;
                do
                {
                    pos = manifest.IndexOf("<StreamIndex", pos + 1);
                    if (pos > 0)
                        lastPos = pos;
                }
                while (pos > 0);
                if(lastPos>0)
                {
                    pos = manifest.IndexOf("</StreamIndex>", lastPos);
                    if(pos>0)
                    {
                        return manifest.Substring(pos + 14);
                    }
                }
            }
            return result;
        }
        public int GetManifestStreamIndexCount(string manifest)
        {
            int result = 0;
            if (!string.IsNullOrEmpty(manifest))
            {
                int pos = -1;
                do
                {
                    pos = manifest.IndexOf("<StreamIndex", pos + 1);
                    if (pos > 0)
                        result++;
                }
                while (pos > 0);
            }
            return result;
        }
        public string GetManifestStreamIndex(string manifest,int Index)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(manifest))
            {
                int pos = -1;
                int i = 0;
                do
                {
                    
                    pos = manifest.IndexOf("<StreamIndex", pos + 1);
                    if (pos > 0)
                    {
                        if(Index == i)
                        {
                            int endPos = manifest.IndexOf("</StreamIndex>", pos );
                            if(endPos>0)
                            {
                                return manifest.Substring(pos, endPos + 14 - pos);
                            }
                        }
                        i++;
                    }
                }
                while (pos > 0);
            }
            return result;
        }
        public string GetManifestStreamIndexType(string StreamIndex)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(StreamIndex))
            {
                int pos = -1;
                pos = StreamIndex.IndexOf("Type=\"");
                if (pos > 0)
                {
                    int endPos = -1;
                    endPos = StreamIndex.IndexOf("\"", pos + 6);
                    if(endPos>0)
                    {
                        return StreamIndex.Substring(pos + 6, endPos - pos - 6);
                    }
                }
            }
            return result;
        }
        public string GetManifestStreamIndexSubtype(string StreamIndex)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(StreamIndex))
            {
                int pos = -1;
                pos = StreamIndex.IndexOf("Subtype=\"");
                if (pos > 0)
                {
                    int endPos = -1;
                    endPos = StreamIndex.IndexOf("\"", pos + 9);
                    if (endPos > 0)
                    {
                        return StreamIndex.Substring(pos + 9, endPos - pos - 9);
                    }
                }
            }
            return result;
        }
        public string GetManifestStreamIndexLanguage(string StreamIndex)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(StreamIndex))
            {
                int pos = -1;
                pos = StreamIndex.IndexOf("Language=\"");
                if (pos > 0)
                {
                    int endPos = -1;
                    endPos = StreamIndex.IndexOf("\"", pos + 10);
                    if (endPos > 0)
                    {
                        return StreamIndex.Substring(pos + 10, endPos - pos - 10);
                    }
                }
            }
            return result;
        }
        public string UpdateManifest(string manifest)
        {
            string result = string.Empty;
            string loc = string.Empty;
            loc = GetManifestHeader(manifest);
            StringBuilder stringBuilder = new StringBuilder(loc);
            StringBuilder stringAudioBuilder = new StringBuilder();
            StringBuilder stringTextBuilder = new StringBuilder();
            StringBuilder stringVideoBuilder = new StringBuilder();
            int Count = GetManifestStreamIndexCount(manifest);
            int i = 0;
            while(i < Count)
            {
                loc = GetManifestStreamIndex(manifest, i++);
                string StreamIndexTypeType = GetManifestStreamIndexType(loc);
                if(StreamIndexTypeType == "audio")
                {
                    stringAudioBuilder.Append(loc);
                }
                else if (StreamIndexTypeType == "text")
                {
                    string Lang = GetManifestStreamIndexLanguage(loc);
                    string Subtype = GetManifestStreamIndexSubtype(loc);
                    stringTextBuilder.Append(loc);
                }
                else 
                {
                    stringVideoBuilder.Append(loc);
                }

            }
            stringBuilder.Append(stringVideoBuilder.ToString());
            stringBuilder.Append(stringAudioBuilder.ToString());
            stringBuilder.Append(stringTextBuilder.ToString());
            loc = GetManifestTail(manifest);
            stringBuilder.Append(loc);
            return stringBuilder.ToString();
        }
        public IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> SendRequestAsync(HttpRequestMessage request)
        {
            return AsyncInfo.Run<HttpResponseMessage, HttpProgress>(async (cancellationToken, progress) =>
            {
                HttpResponseMessage response = await innerFilter.SendRequestAsync(request).AsTask(cancellationToken, progress);
                cancellationToken.ThrowIfCancellationRequested();
                if ((response != null)&& (response.Content != null) && (response.Content.Headers != null))
                {
                    if(response.Content.Headers.ContainsKey("Content-Type"))
                    {
                        if ( (response.Content.Headers["Content-Type"].StartsWith("text/xml")) ||
                         (response.Content.Headers["Content-Type"].StartsWith("application/vnd.ms-sstr+xml")))
                        {
                            // Read and update manifest 
                            // Put Video Stream Index at the beginning of the manifest
                            // Store the information related to subtitle
                            string manifest = await response.Content.ReadAsStringAsync();
                            string newManifest = UpdateManifest(manifest);
                            if (!string.IsNullOrEmpty(newManifest))
                                response.Content = new Windows.Web.Http.HttpStringContent(newManifest);

                            response.Content.Headers["Content-Type"] = "application/vnd.ms-sstr+xml";
                        }
                    }
                }
                return response;
            });
        }

        public void Dispose()
        {
            innerFilter.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
