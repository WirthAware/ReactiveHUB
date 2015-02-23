namespace ProjectTemplate.WebRequests
{
    using System;
    using System.IO;
    using System.Net;

    public class WebResponse : IWebResponse
    {
        private readonly System.Net.WebResponse res;

        public WebResponse(System.Net.WebResponse res)
        {
            this.res = res;
        }

        public long ContentLength
        {
            get
            {
                return this.res.ContentLength;
            }
        }

        public string ContentType
        {
            get
            {
                return this.res.ContentType;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.res.Headers;
            }
        }

        public Uri ResponseUri
        {
            get
            {
                return this.res.ResponseUri;
            }
        }

        public bool SupportsHeaders
        {
            get
            {
                return this.res.SupportsHeaders;
            }
        }

        public void Dispose()
        {
            this.res.Dispose();
        }

        public Stream GetResponseStream()
        {
            return this.res.GetResponseStream();
        }
    }
}