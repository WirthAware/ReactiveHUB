﻿namespace ReactiveHub.Integration.Twitter.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Web;

    using FluentAssertions;

    using Microsoft.Reactive.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using ProjectTemplate.WebRequests;

    [TestClass]
    public class UserContextTests
    {
        private const string TweetText = "I'm trying to use the @twitterapi to post a tweet";

        [TestMethod]
        public void UsingSearchApi()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);

            var searchRequest = CreateRequestData(serviceMock);
            var searchResult = new Subject<string>();

            serviceMock.Setup(
                x =>
                x.CreateGet(
                    new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString"),
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
                x.CreateGet(
                    new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString"),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);

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
                x.CreateGet(
                    new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=123"),
                    It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(searchResult);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var results = Record(sut.Search("MySearchString", 123)).Item1;

            serviceMock.Verify(
                x =>
                x.CreateGet(
                    new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=123"),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);

            serviceMock.Verify(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()), Times.Once);

            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnCompleted);
            results.Count(x => x.Kind == NotificationKind.OnNext).Should().Be(15);
            CheckForExceptions(results);
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
                x.CreateGet(
                    new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=0"),
                    It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(
                x =>
                x.CreateGet(
                    new Uri(
                    "https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                    It.IsAny<Dictionary<string, string>>())).Returns(searchRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(searchRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(searchResult);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var results = Record(sut.Poll("MySearchString", TimeSpan.FromSeconds(10), scheduler));

            var responses = results.Item1;
            var subscription = results.Item2;

            // First poll is done immediately
            scheduler.AdvanceBy(1);

            responses.Count(x => x.Kind == NotificationKind.OnNext).Should().Be(15, "all search results should be returned");
            CheckForExceptions(responses);
            responses.Should().NotContain(x => x.Kind == NotificationKind.OnCompleted);

            serviceMock.Verify(
                x =>
                x.CreateGet(
                    new Uri("https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=0"),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);

            // Next poll is not done for the next 10 seconds
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);

            serviceMock.Verify(
                x =>
                x.CreateGet(
                    new Uri(
                    "https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Never);

            // After 10 seconds the next poll is executed with the correct since_id
            scheduler.AdvanceBy(1);

            serviceMock.Verify(
                x =>
                x.CreateGet(
                    new Uri(
                    "https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);

            responses.Count(x => x.Kind == NotificationKind.OnNext).Should().Be(30, "all search results should be returned");
            CheckForExceptions(responses);
            responses.Should().NotContain(x => x.Kind == NotificationKind.OnCompleted);

            // No more polls after subscription is disposed
            subscription.Dispose();

            scheduler.AdvanceBy(TimeSpan.FromSeconds(20).Ticks);

            serviceMock.Verify(
                x =>
                x.CreateGet(
                    new Uri(
                    "https://api.twitter.com/1.1/search/tweets.json?q=MySearchString&since_id=570254350381162496"),
                    It.IsAny<Dictionary<string, string>>()),
                Times.Once);

            CheckForExceptions(responses);
        }

        [TestMethod]
        public void PostingTweet()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var postRequest = CreateRequestData(serviceMock);

            var requestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            var headers = new WebHeaderCollection();
            requestMock.SetupProperty(x => x.Method);
            requestMock.SetupProperty(x => x.ContentType);
            requestMock.Setup(x => x.Headers).Returns(headers);

            byte[] requestData = null;

            serviceMock.Setup(x => x.Create(new Uri(EndpointUris.PostTweetUrl), It.IsAny<Action<IWebRequest>>(), It.IsAny<byte[]>()))
                .Callback<Uri, Action<IWebRequest>, byte[]>(
                    (uri, action, data) =>
                        {
                            action(requestMock.Object);
                            requestData = data;
                        })
                    .Returns(postRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(postRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Properties.Resources.PostResult));

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var results = Record(sut.PostTweet(TweetText)).Item1;

            requestMock.VerifySet(x => x.Method = "POST");
            requestMock.VerifySet(x => x.ContentType = "application/x-www-form-urlencoded; charset=utf-8");
            headers.AllKeys.Should().Equal("Authorization");

            var postData = DecodePostBody(requestData);
            postData.Should().Equal(new Dictionary<string, string> { { "status", TweetText } });

            CheckForExceptions(results);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnNext);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnCompleted);
        }

        [TestMethod]
        public void PostingTweetToOtherUser()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var postRequest = CreateRequestData(serviceMock);

            var requestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            var headers = new WebHeaderCollection();
            requestMock.SetupProperty(x => x.Method);
            requestMock.SetupProperty(x => x.ContentType);
            requestMock.Setup(x => x.Headers).Returns(headers);

            byte[] requestData = null;

            serviceMock.Setup(x => x.Create(new Uri(EndpointUris.PostTweetUrl), It.IsAny<Action<IWebRequest>>(), It.IsAny<byte[]>()))
                .Callback<Uri, Action<IWebRequest>, byte[]>(
                    (uri, action, data) =>
                    {
                        action(requestMock.Object);
                        requestData = data;
                    })
                    .Returns(postRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(postRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Properties.Resources.PostResult));

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var results = Record(sut.PostTweet(TweetText, "targetUser")).Item1;

            requestMock.VerifySet(x => x.Method = "POST");
            requestMock.VerifySet(x => x.ContentType = "application/x-www-form-urlencoded; charset=utf-8");
            headers.AllKeys.Should().Equal("Authorization");

            var postData = DecodePostBody(requestData);
            postData.Should().Equal(new Dictionary<string, string> { { "status", "@targetUser " + TweetText } });

            CheckForExceptions(results);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnNext);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnCompleted);
        }

        [TestMethod]
        public void PostingReply()
        {
            var referenceTweet = new Tweet
                                     {
                                         Id = 123, 
                                         Message = "This is a reference Tweet", 
                                         Sender = "originalUser", 
                                         Time = DateTime.Now
                                     };

            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var postRequest = CreateRequestData(serviceMock);

            var requestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            var headers = new WebHeaderCollection();
            requestMock.SetupProperty(x => x.Method);
            requestMock.SetupProperty(x => x.ContentType);
            requestMock.Setup(x => x.Headers).Returns(headers);

            byte[] requestData = null;

            serviceMock.Setup(x => x.Create(new Uri(EndpointUris.PostTweetUrl), It.IsAny<Action<IWebRequest>>(), It.IsAny<byte[]>()))
                .Callback<Uri, Action<IWebRequest>, byte[]>(
                    (uri, action, data) =>
                    {
                        action(requestMock.Object);
                        requestData = data;
                    })
                    .Returns(postRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(postRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Properties.Resources.PostResult));

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var results = Record(sut.PostTweet(TweetText, referenceTweet)).Item1;

            requestMock.VerifySet(x => x.Method = "POST");
            requestMock.VerifySet(x => x.ContentType = "application/x-www-form-urlencoded; charset=utf-8");
            headers.AllKeys.Should().Equal("Authorization");

            var postData = DecodePostBody(requestData);
            postData.Should()
                .Equal(
                    new Dictionary<string, string>
                        {
                            { "status", "@originalUser " + TweetText }, 
                            { "in_reply_to_status_id", "123" }
                        });

            CheckForExceptions(results);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnNext);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnCompleted);
        }

        [TestMethod]
        public void LikeTweet()
        {
            var referenceTweet = new Tweet
            {
                Id = 123,
                Message = "This is a reference Tweet",
                Sender = "originalUser",
                Time = DateTime.Now
            };

            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var postRequest = CreateRequestData(serviceMock);

            var requestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            var headers = new WebHeaderCollection();
            requestMock.SetupProperty(x => x.Method);
            requestMock.SetupProperty(x => x.ContentType);
            requestMock.Setup(x => x.Headers).Returns(headers);

            byte[] requestData = null;

            serviceMock.Setup(x => x.Create(new Uri(EndpointUris.LikeUrl), It.IsAny<Action<IWebRequest>>(), It.IsAny<byte[]>()))
                .Callback<Uri, Action<IWebRequest>, byte[]>(
                    (uri, action, data) =>
                    {
                        action(requestMock.Object);
                        requestData = data;
                    })
                    .Returns(postRequest);

            serviceMock.Setup(x => x.SendAndReadAllText(postRequest, It.IsAny<Encoding>(), It.IsAny<IScheduler>()))
                .Returns(Observable.Return(Properties.Resources.PostResult));

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var results = Record(sut.Like(referenceTweet)).Item1;

            requestMock.VerifySet(x => x.Method = "POST");
            requestMock.VerifySet(x => x.ContentType = "application/x-www-form-urlencoded; charset=utf-8");
            headers.AllKeys.Should().Equal("Authorization");

            var postData = DecodePostBody(requestData);
            postData.Should()
                .Equal(
                    new Dictionary<string, string>
                        {
                            { "id", "123" }
                        });

            CheckForExceptions(results);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnNext);
            results.Should().ContainSingle(x => x.Kind == NotificationKind.OnCompleted);
        }

        private static WebRequestData CreateRequestData(IMock<IWebRequestService> serviceMock)
        {
            return new WebRequestData(() => null, Guid.NewGuid().ToByteArray(), serviceMock.Object);
        }

        private static void CheckForExceptions<T>(IEnumerable<Notification<T>> results)
        {
            var onError = results.SingleOrDefault(x => x.Kind == NotificationKind.OnError);
            if (onError == null)
            {
                return;
            }

            // Wrap in AggregateException to not destroy call stack of original exception
            throw new AggregateException(onError.Exception);
        } 

        private static Tuple<List<Notification<T>>, IDisposable> Record<T>(IObservable<T> observable)
        {
            var result = new List<Notification<T>>();

            var subscription = observable
                .Materialize()
                .Subscribe(result.Add);

            return new Tuple<List<Notification<T>>, IDisposable>(result, subscription);
        }

        private static Dictionary<string, string> DecodePostBody(byte[] postBody)
        {
            var postString = Encoding.UTF8.GetString(postBody);
            var parameters = postString.Split('&').Select(x => x.Split('=')).ToDictionary(x => HttpUtility.UrlDecode(x[0]), x => HttpUtility.UrlDecode(x[1]));
            return parameters;
        }
    }
}