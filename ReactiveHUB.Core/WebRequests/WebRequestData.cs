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

        public bool Equals(WebRequestData other)
        {
            return Equals(RequestFactory, other.RequestFactory) && Equals(DataToSend, other.DataToSend) && Equals(Service, other.Service);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WebRequestData && Equals((WebRequestData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (RequestFactory != null ? RequestFactory.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (DataToSend != null ? DataToSend.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Service != null ? Service.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}