// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IWebRequest.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   An interface for <see cref="System.Net.WebRequest" />, so it can be mocked
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ProjectTemplate.WebRequests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// An interface for <see cref="System.Net.WebRequest"/>, so it can be mocked
    /// </summary>
    public interface IWebRequest
    {
        string ContentType { get; set; }

        ICredentials Credentials { get; set; }

        WebHeaderCollection Headers { get; set; }

        string Method { get; set; }

        Uri RequestUri { get; }

        bool UseDefaultCredentials { get; set; }

        void Abort();

        Task<Stream> GetRequestStream();

        Task<IWebResponse> GetResponse();
    }
}