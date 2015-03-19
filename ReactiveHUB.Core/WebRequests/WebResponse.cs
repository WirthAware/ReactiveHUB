// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebResponse.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   An internal implementation of <see cref="IWebResponse" /> wrapping the <see cref="System.Net.WebResponse" />
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate.WebRequests
{
    using System;
    using System.IO;
    using System.Net;

    using ReactiveHub.Contracts.WebRequests;

    /// <summary>
    /// An internal implementation of <see cref="IWebResponse"/> wrapping the <see cref="System.Net.WebResponse"/>
    /// </summary>
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