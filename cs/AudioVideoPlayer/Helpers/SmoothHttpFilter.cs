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
                        if (response.Content.Headers["Content-Type"].StartsWith("text/xml"))
                            response.Content.Headers["Content-Type"] = "application/vnd.ms-sstr+xml";
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
