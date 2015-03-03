// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserContextTests.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the UserContextTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter.Tests
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

    using ReactiveHub.Integration.Twitter.Models;

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
                                         Text = "This is a reference Tweet",
                                         Sender = new TwitterUser { DisplayName = "originalUser" }, 
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
                Text = "This is a reference Tweet",
                Sender = new TwitterUser { DisplayName = "originalUser" },
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

        [TestMethod]
        public void TrackKeyword()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var getRequest = CreateRequestData(serviceMock);

            serviceMock.Setup(
                x =>
                x.CreateGet(
                    It.Is<Uri>(uri => uri.ToString().StartsWith(EndpointUris.TrackKeyword)),
                    It.IsAny<Dictionary<string, string>>())).Returns(getRequest);

            var streamedLines = new Subject<string>();

            serviceMock.Setup(
                x => x.SendAndReadLinewise(getRequest, It.IsAny<Encoding>(), It.IsAny<bool>(), It.IsAny<IScheduler>()))
                .Returns(streamedLines);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var returnValue = Record(sut.TrackKeywords("Ukraine"));

            var results = returnValue.Item1;

            CheckForExceptions(results);
            results.Should().NotContain(x => x.Kind == NotificationKind.OnNext);
            results.Should().NotContain(x => x.Kind == NotificationKind.OnCompleted);

            streamedLines.OnNext(Properties.Resources.PostResult);

            CheckForExceptions(results);
            results.Count(x => x.Kind == NotificationKind.OnNext).Should().Be(1);
            results.Should().NotContain(x => x.Kind == NotificationKind.OnCompleted);

            streamedLines.OnNext(Properties.Resources.PostResult);
            CheckForExceptions(results);
            results.Count(x => x.Kind == NotificationKind.OnNext).Should().Be(2);

            returnValue.Item2.Dispose();
            streamedLines.HasObservers.Should().BeFalse();

            streamedLines.OnNext(Properties.Resources.PostResult);
            CheckForExceptions(results);
            results.Count(x => x.Kind == NotificationKind.OnNext).Should().Be(2);
        }

        [TestMethod]
        public void CanOnlyHaveOneTrackingRunning()
        {
            var serviceMock = new Mock<IWebRequestService>(MockBehavior.Strict);
            var getRequest = CreateRequestData(serviceMock);

            serviceMock.Setup(
                x =>
                x.CreateGet(
                    It.Is<Uri>(uri => uri.ToString().StartsWith(EndpointUris.TrackKeyword)),
                    It.IsAny<Dictionary<string, string>>())).Returns(getRequest);

            var streamedLines = new Subject<string>();

            serviceMock.Setup(
                x => x.SendAndReadLinewise(getRequest, It.IsAny<Encoding>(), It.IsAny<bool>(), It.IsAny<IScheduler>()))
                .Returns(streamedLines);

            var sut = new UserContext("123", "456", "789", "ABC", serviceMock.Object);

            var observable = sut.TrackKeywords("Ukraine");
            var firstResult = Record(observable);

            // Can't subscribe twice since that would lead to two requests running at the same time
            var anotherResult = Record(observable);
            CheckForExceptions(firstResult.Item1);
            ExpectException<Tweet, InvalidOperationException>(anotherResult.Item1);
            
            // Can't create another subscription
            anotherResult = Record(sut.TrackKeywords("Ukraine"));
            CheckForExceptions(firstResult.Item1);
            ExpectException<Tweet, InvalidOperationException>(anotherResult.Item1);

            // When I dispose the first subscription, I can create a second one.
            firstResult.Item2.Dispose();
            anotherResult = Record(sut.TrackKeywords("Ukraine"));

            CheckForExceptions(firstResult.Item1);
            CheckForExceptions(anotherResult.Item1);
        }

        private static void ExpectException<TResult, TException>(IEnumerable<Notification<TResult>> results)
        {
            results.Single(x => x.Kind == NotificationKind.OnError).Exception.Should().BeOfType<TException>();
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