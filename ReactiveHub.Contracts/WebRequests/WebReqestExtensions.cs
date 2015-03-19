// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebReqestExtensions.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Extension methods for the <see cref="WebRequestData" /> struct, that enable a fluent syntax
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Contracts.WebRequests
{
    using System;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Text;

    /// <summary>
    /// Extension methods for the <see cref="WebRequestData"/> struct, that enable a fluent syntax
    /// </summary>
    public static class WebReqestExtensions
    {
        public static IObservable<Unit> Send(this WebRequestData requestData, IScheduler scheduler = null)
        {
            return requestData.Service.Send(requestData, scheduler);
        }

        public static IObservable<byte> SendAndReadBytewise(this WebRequestData requestData, bool stopAtEndOfStream = false, IScheduler scheduler = null)
        {
            return requestData.Service.SendAndReadBytewise(requestData, stopAtEndOfStream, scheduler);
        }

        public static IObservable<string> SendAndReadLinewise(
            this WebRequestData data,
            Encoding encoding = null,
            bool stopAtEndOfStream = false,
            IScheduler sched = null)
        {
            return data.Service.SendAndReadLinewise(data, encoding, stopAtEndOfStream, sched);
        }

        public static IObservable<string> SendAndReadAllText(
            this WebRequestData data,
            Encoding encoding = null,
            IScheduler sched = null)
        {
            return data.Service.SendAndReadAllText(data, encoding, sched);
        }
    }
}