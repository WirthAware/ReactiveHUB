// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebRequestData.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   A struct that saves a web request and the data to send in order to enable a fluent syntax for using web requests
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Contracts.WebRequests
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
            return Equals(this.RequestFactory, other.RequestFactory) && Equals(this.DataToSend, other.DataToSend) && Equals(this.Service, other.Service);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is WebRequestData && this.Equals((WebRequestData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (this.RequestFactory != null ? this.RequestFactory.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (this.DataToSend != null ? this.DataToSend.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (this.Service != null ? this.Service.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}