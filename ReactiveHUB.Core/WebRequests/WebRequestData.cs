namespace ProjectTemplate.WebRequests
{
    using System;

    /// <summary>
    /// A struct that saves a web request and the data to send in order to enable a fluent syntax for using web requests
    /// </summary>
    public struct WebRequestData
    {
        public readonly Func<IWebRequest> RequestFactory;

        public readonly byte[] DataToSend;

        public readonly IWebRequestService Service;

        public WebRequestData(Func<IWebRequest> requestFactory, byte[] dataToSend, IWebRequestService service)
        {
            this.RequestFactory = requestFactory;
            this.DataToSend = dataToSend;
            this.Service = service;
        }
    }
}