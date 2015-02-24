namespace ProjectTemplate.WebRequests
{
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Text;

    /// <summary>
    /// A service for creating and making web requests. 
    /// It should bridge the gap between the <see cref="System.Net.WebRequest"/> and reactive programming
    /// </summary>
    public interface IWebRequestService
    {
        WebRequestData Create(Uri uri, Action<IWebRequest> modifierAction = null, byte[] data = null);

        WebRequestData CreateGet(Uri uri, Dictionary<string, string> headers = null);

        IObservable<Unit> Send(WebRequestData data, IScheduler sched = null);

        IObservable<byte> SendAndReadBytewise(WebRequestData data, bool stopAtEndOfStream = false, IScheduler sched = null);

        IObservable<string> SendAndReadLinewise(WebRequestData data, Encoding encoding = null, bool stopAtEndOfStream = false, IScheduler sched = null);

        IObservable<string> SendAndReadAllText(
            WebRequestData data,
            Encoding encoding = null,
            IScheduler sched = null);
    }
}