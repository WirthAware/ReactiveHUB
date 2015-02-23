namespace ProjectTemplate.WebRequests
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Extension methods for the <see cref="WebRequestData"/> struct, that enable a fluent syntax
    /// </summary>
    public static class WebReqestExtensions
    {
        public static IObservable<Unit> Send(this WebRequestData requestData)
        {
            return requestData.Service.Send(requestData);
        }
    }
}