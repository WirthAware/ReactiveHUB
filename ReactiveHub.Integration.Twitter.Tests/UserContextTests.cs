using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ProjectTemplate.WebRequests;

namespace ReactiveHub.Integration.Twitter.Tests
{
    [TestClass]
    public class UserContextTests
    {
        [TestMethod]
        public void UsingSearchApi()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);

            var searchRequest = CreateRequestData(serviceMock);
            var searchResult = new Subject<string>();

            serviceMock.Setup(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString"),
                        It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(searchResult);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

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

            serviceMock.Verify(
    x =>
        x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString"),
            It.IsAny<Dictionary<string, string>>()), Times.Once);

            serviceMock.Verify(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()), Times.Once);
        }

        [TestMethod]
        public void UsingSearchApiWithSinceId()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);

            var searchRequest = CreateRequestData(serviceMock);
            var searchResult = Observable.Return(Properties.Resources.SimpleSearchResult);

            serviceMock.Setup(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=123"),
                        It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(searchResult);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var searchResults = new List<Tweet>();
            Exception ex = null;
            var completed = false;

            sut.Search("MySearchString", 123).Subscribe(searchResults.Add, e => ex = new AggregateException(e), () => completed = true);

            if (ex != null)
            {
                throw ex;
            }

            serviceMock.Verify(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=123"),
                        It.IsAny<Dictionary<string, string>>()), Times.Once);

            serviceMock.Verify(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()), Times.Once);

            searchResults.Count.Should().Be(15, "all search results should be returned");
            completed.Should().BeTrue();
        }

        [TestMethod]
        public void PollingSearchApi()
        {
            var scheduler = new TestScheduler();

            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);

            var searchRequest = CreateRequestData(serviceMock);
            var searchResult = Observable.Return(Properties.Resources.SimpleSearchResult);

            serviceMock.Setup(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=0"),
                        It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                        It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(searchResult);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var searchResults = new List<Tweet>();
            Exception ex = null;
            var completed = false;

            var subscription = sut.Poll("MySearchString", TimeSpan.FromSeconds(10), scheduler)
                .Subscribe(searchResults.Add, e => ex = new AggregateException(e), () => completed = true);

            // First poll is done immediately
            scheduler.AdvanceBy(1);

            searchResults.Count.Should().Be(15, "all search results should be returned");
            completed.Should().BeFalse();

            serviceMock.Verify(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=0"),
                        It.IsAny<Dictionary<string, string>>()), Times.Once);

            // Next poll is not done for the next 10 seconds
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);

            serviceMock.Verify(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                        It.IsAny<Dictionary<string, string>>()), Times.Never);

            // After 10 seconds the next poll is executed with the correct since_id
            scheduler.AdvanceBy(1);

            serviceMock.Verify(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                        It.IsAny<Dictionary<string, string>>()), Times.Once);

            searchResults.Count.Should().Be(30, "both search results should be returned");

            // No more polls after subscription is disposed
            subscription.Dispose();

            scheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

            serviceMock.Verify(
                x =>
                    x.CreateGet(new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                        It.IsAny<Dictionary<string, string>>()), Times.Once);

            // No errors occured while polling
            if (ex != null)
            {
                throw ex;
            }
        }

        private static WebRequestData CreateRequestData(IMock<IWebRequestService> serviceMock)
        {
            return new WebRequestData(() => null, Guid.NewGuid().ToByteArray(), serviceMock.Object);
        }
    }
}