namespace ProjectTemplate.WebRequests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    using ProjectTemplate.Helpers;

    /// <summary>
    /// An internal implementation of <see cref="IWebRequest"/> wrapping the <see cref="System.Net.WebRequest"/>
    /// </summary>
    internal class WebRequest : IWebRequest
    {
        private readonly System.Net.WebRequest req;

        public WebRequest(System.Net.WebRequest req)
        {
            this.req = req;
        }

        public string ContentType
        {
            get
            {
                return this.req.ContentType;
            }

            set
            {
                this.req.ContentType = value;
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return this.req.Credentials;
            }

            set
            {
                this.req.Credentials = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.req.Headers;
            }

            set
            {
                this.req.Headers = value;
            }
        }

        public string Method
        {
            get
            {
                return this.req.Method;
            }

            set
            {
                this.req.Method = value;
            }
        }

        public Uri RequestUri
        {
            get
            {
                return this.req.RequestUri;
            }
        }

        public bool UseDefaultCredentials
        {
            get
            {
                return this.req.UseDefaultCredentials;
            }

            set
            {
                this.req.UseDefaultCredentials = value;
            }
        }

        public void Abort()
        {
            this.req.Abort();
        }

        public Task<Stream> GetRequestStream()
        {
            return Task.Factory.FromAsync<Stream>(this.req.BeginGetRequestStream, this.req.EndGetRequestStream, null);
        }

        public Task<IWebResponse> GetResponse()
        {
            return
                Task.Factory.FromAsync<System.Net.WebResponse>(this.req.BeginGetResponse, this.req.EndGetResponse, null)
                    .Then(res => (IWebResponse)new WebResponse(res));
        }
    }
}