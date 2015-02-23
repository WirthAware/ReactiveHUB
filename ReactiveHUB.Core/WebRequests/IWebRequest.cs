namespace ProjectTemplate.WebRequests
{
    using System;
    using System.Net;

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
    }
}