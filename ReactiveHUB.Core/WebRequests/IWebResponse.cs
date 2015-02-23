namespace ProjectTemplate.WebRequests
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// An interface for <see cref="System.Net.WebResponse"/>, so it can be mocked
    /// </summary>
    public interface IWebResponse : IDisposable
    {
        long ContentLength { get; }

        string ContentType { get; }

        WebHeaderCollection Headers { get; }

        Uri ResponseUri { get; }

        bool SupportsHeaders { get; }

        Stream GetResponseStream();
    }
}