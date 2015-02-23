namespace ReactiveHUB.Core.Tests.WebRequests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.Reactive.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ProjectTemplate.WebRequests;

    using WebRequest = ProjectTemplate.WebRequests.WebRequest;

    [TestClass]
    public class WebRequestServiceTests
    {
        private static readonly Uri RequestUri;

        private static readonly byte[] TestData;

        static WebRequestServiceTests()
        {
            RequestUri = new Uri("http://www.google.com");
            TestData = new byte[] { 1, 2, 3, 4, 5 };
        }

        [TestMethod]
        public void GenericCreateInvokesCallbacksAndFillsData()
        {
            // A mocked factory function that records the passed uri and returns the given result
            IWebRequest returnedRequest = new WebRequest(System.Net.WebRequest.Create(RequestUri));
            Uri requestedUri = null;
            Func<Uri, IWebRequest> factory = u =>
                {
                    requestedUri = u;
                    return returnedRequest;
                };
            
            // A mocked modifier that records which request was modified
            IWebRequest modifiedRequest = null;
            Action<IWebRequest> modifier = r => modifiedRequest = r;

            // Create the SUT with the given factory
            var sut = new WebRequestService(factory: factory);
            
            // Use the generic create with the modifier
            var result = sut.Create(
                RequestUri,
                modifier,
                TestData);

            // The result should have the sut as service (for fluent syntax) and the given data
            Assert.AreSame(sut, result.Service);
            CollectionAssert.AreEqual(TestData, result.DataToSend);

            // Create the WebRequest
            var request = result.RequestFactory();

            // The created web request as well as the modified one should be the one returned by the factory
            Assert.AreEqual(returnedRequest, modifiedRequest);
            Assert.AreEqual(returnedRequest, request);

            // The factory should be called with the Uri from the create call
            Assert.AreEqual(RequestUri, requestedUri);
        }

        [TestMethod]
        public void CreateGetCreatesCorrectRequest()
        {
            var sut = new WebRequestService();
            var result = sut.CreateGet(RequestUri, new Dictionary<string, string> { { "Foo", "Bar" } });

            Assert.IsNull(result.DataToSend);
            Assert.AreSame(sut, result.Service);

            var request = result.RequestFactory();

            Assert.AreEqual(RequestUri, request.RequestUri);
            Assert.AreEqual("GET", request.Method);
            Assert.AreEqual("Bar", request.Headers["Foo"]);
        }

        [TestMethod]
        public void SendOnlyDoesNotReadTheResponseStream()
        {
            var scheduler = new TestScheduler();

            var webResponseMock = new Mock<IWebResponse>(MockBehavior.Strict);
            webResponseMock.Setup(x => x.Dispose());

            var webRequestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            webRequestMock.Setup(x => x.GetResponse()).Returns(Task.FromResult(webResponseMock.Object));

            var sut = new WebRequestService();
            var request = new WebRequestData(() => webRequestMock.Object, null, sut);

            var observable = sut.Send(request, scheduler);
            
            // Nothing should be done yet
            webRequestMock.Verify(x => x.GetRequestStream(), Times.Never());
            webRequestMock.Verify(x => x.GetResponse(), Times.Never());

            var count = 0;
            var completed = false;

            observable.Subscribe(_ => count++, () => completed = true);
 
            scheduler.AdvanceBy(1000);

            Assert.IsTrue(completed);
            Assert.AreEqual(1, count);

            webRequestMock.Verify(x => x.GetRequestStream(), Times.Never());
            webRequestMock.Verify(x => x.GetResponse(), Times.Once());
            webResponseMock.Verify(x => x.GetResponseStream(), Times.Never());
        }

        [TestMethod]
        public void SendOnlyAlsoWorksWithData()
        {
            var scheduler = new TestScheduler();

            var webResponseMock = new Mock<IWebResponse>(MockBehavior.Strict);
            webResponseMock.Setup(x => x.Dispose());

            var webRequestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            webRequestMock.Setup(x => x.GetResponse()).Returns(Task.FromResult(webResponseMock.Object));
            
            var requestStream = new MemoryStream();
            webRequestMock.Setup(x => x.GetRequestStream()).Returns(Task.FromResult<Stream>(requestStream));

            var sut = new WebRequestService();
            var request = new WebRequestData(() => webRequestMock.Object, TestData, sut);

            var observable = sut.Send(request, scheduler);

            // Nothing should be done yet
            webRequestMock.Verify(x => x.GetRequestStream(), Times.Never());
            webRequestMock.Verify(x => x.GetResponse(), Times.Never());

            var count = 0;
            var completed = false;

            observable.Subscribe(_ => count++, () => completed = true);

            scheduler.AdvanceBy(1000);

            Assert.IsTrue(completed);
            Assert.AreEqual(1, count);

            webRequestMock.Verify(x => x.GetRequestStream(), Times.Once());
            webRequestMock.Verify(x => x.GetResponse(), Times.Once());
            webResponseMock.Verify(x => x.GetResponseStream(), Times.Never());

            CollectionAssert.AreEqual(TestData, requestStream.ToArray());
        }
    }
}