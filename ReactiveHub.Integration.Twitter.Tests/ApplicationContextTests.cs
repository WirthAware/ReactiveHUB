using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ProjectTemplate.WebRequests;

namespace ReactiveHub.Integration.Twitter.Tests
{
    [TestClass]
    public class ApplicationContextTests
    {
        private const string AccessTokenResultFormat = @"{{""token_type"":""bearer"",""access_token"":""{0}""}}";

        [TestMethod]
        public void FetchingAndInvalidatingBearerToken()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var token = Guid.NewGuid().ToString("N");

            var getTokenRequest = new WebRequestData(() => null, null, serviceMock.Object);
            var getTokenObservable = Observable.Return(string.Format(AccessTokenResultFormat, token));

            var invalidateTokenRequest = new WebRequestData(() => null, null, serviceMock.Object);
            var invalidateTokenObservable = Observable.Return(Unit.Default);

            serviceMock.Setup(
                x => x.CreatePost(new Uri("https://api.twitter.com/oauth2/token?grant_type=client_credentials"), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null))
                .Returns(getTokenRequest);

            serviceMock.Setup(
                x => x.CreatePost(new Uri("https://api.twitter.com/oauth2/invalidate_token?access_token=" + token), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null))
                .Returns(invalidateTokenRequest);

            serviceMock
                .Setup(x => x.SendAndReadAllText(getTokenRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(getTokenObservable);

            serviceMock.Setup(x => x.Send(invalidateTokenRequest, It.IsAny<IScheduler>()))
                .Returns(invalidateTokenObservable);

            var sut = new ApplicationContext("123", "456", serviceMock.Object);

            serviceMock.Verify(
                x =>
                    x.CreatePost(new Uri("https://api.twitter.com/oauth2/token?grant_type=client_credentials"),
                        It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null), Times.Once);

            serviceMock.Verify(x => x.SendAndReadAllText(getTokenRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()), Times.Once);

            serviceMock.Verify(
                x =>
                    x.CreatePost(new Uri("https://api.twitter.com/oauth2/invalidate_token?access_token=" + token),
                        It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null), Times.Never);

            serviceMock.Verify(x => x.Send(invalidateTokenRequest, It.IsAny<IScheduler>()), Times.Never);

            sut.Dispose();

            serviceMock.Verify(
                x =>
                    x.CreatePost(new Uri("https://api.twitter.com/oauth2/token?grant_type=client_credentials"),
                        It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null), Times.Once);

            serviceMock.Verify(x => x.SendAndReadAllText(getTokenRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()), Times.Once);

            serviceMock.Verify(
                x =>
                    x.CreatePost(new Uri("https://api.twitter.com/oauth2/invalidate_token?access_token=" + token),
                        It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null), Times.Once);

            serviceMock.Verify(x => x.Send(invalidateTokenRequest, It.IsAny<IScheduler>()), Times.Once);
        }

        [TestMethod]
        public void UsingSearchApi()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            SetupTokenLifetime(serviceMock);

            var searchRequest = CreateRequestData(serviceMock);
            var searchResult = new Subject<string>();

            serviceMock.Setup(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString"),
                        It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(searchResult);

            var sut = new ApplicationContext("123", "456", serviceMock.Object);

            var searchResults = new List<Tweet>();
            Exception ex = null;
            var completed = false;

            sut.Search("MySearchString").Subscribe(searchResults.Add, e => ex = new AggregateException(e), () => completed = true);

            if (ex != null)
            {
                throw ex;
            }

            searchResults.Should().BeEmpty("the search request did not return yet");
            completed.Should().BeFalse();

            searchResult.OnNext(Properties.Resources.SimpleSearchResult);
            searchResult.OnCompleted();

            searchResults.Count.Should().Be(15, "all search results should be returned");
            completed.Should().BeTrue();
        }

        private void SetupTokenLifetime(Mock<IWebRequestService> serviceMock)
        {
            var token = Guid.NewGuid().ToString("N");

            var getTokenRequest = CreateRequestData(serviceMock);
            var getTokenObservable = Observable.Return(string.Format(AccessTokenResultFormat, token));

            var invalidateTokenRequest = CreateRequestData(serviceMock);
            var invalidateTokenObservable = Observable.Return(Unit.Default);

            serviceMock.Setup(
                x => x.CreatePost(new Uri("https://api.twitter.com/oauth2/token?grant_type=client_credentials"), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null))
                .Returns(getTokenRequest);

            serviceMock.Setup(
                x => x.CreatePost(new Uri("https://api.twitter.com/oauth2/invalidate_token?access_token=" + token), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), null))
                .Returns(invalidateTokenRequest);

            serviceMock
                .Setup(x => x.SendAndReadAllText(getTokenRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(getTokenObservable);

            serviceMock.Setup(x => x.Send(invalidateTokenRequest, It.IsAny<IScheduler>()))
                .Returns(invalidateTokenObservable);
        }

        private static WebRequestData CreateRequestData(Mock<IWebRequestService> serviceMock)
        {
            return new WebRequestData(() => null, Guid.NewGuid().ToByteArray(), serviceMock.Object);
        }
    }
}