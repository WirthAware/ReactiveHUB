// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UserContext.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the UserContext type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Text;

    using ProjectTemplate.WebRequests;

    using ReactiveHub.Integration.Twitter.Models;

    public class UserContext : ApplicationContext
    {
        private readonly OAuthManager manager;

        private bool isStreaming;

        public UserContext(
            string consumerToken, 
            string consumerSecret, 
            string userToken, 
            string userSecret, 
            IWebRequestService requestService)
            : base(consumerToken, consumerSecret, requestService)
        {
            this.manager = new OAuthManager(this.Token, this.Secret, userToken, userSecret);
        }

        /// <summary>
        /// Sends a tweet to twitter
        /// </summary>
        /// <param name="message">
        /// The message to tweet
        /// </param>
        /// <param name="replyTo">
        /// If specified the new tweet will be a reply to the specified tweet
        /// </param>
        /// <returns>
        /// An <see cref="IObservable{Tweet}"/> which will return the tweet that has been posted
        /// </returns>
        public IObservable<Tweet> PostTweet(string message, Tweet replyTo = null)
        {
            if (replyTo != null && !message.Contains("@" + replyTo.Sender.DisplayName))
            {
                return this.PostTweet(message, replyTo.Sender.DisplayName, replyTo);
            }

            var postFields = new Dictionary<string, string> { { "status", message } };
            if (replyTo != null)
            {
                postFields.Add("in_reply_to_status_id", replyTo.Id.ToString(CultureInfo.InvariantCulture));
            }

            return this.SendPost(EndpointUris.PostTweetUrl, postFields).Select(Tweet.FromJsonString);
        }

        /// <summary>
        /// Sends a tweet publicly to another twitter user
        /// </summary>
        /// <param name="message">
        /// The message to tweet
        /// </param>
        /// <param name="recipient">
        /// The user the tweet should be sent to
        /// </param>
        /// <param name="replyTo">
        /// If specified the new tweet will be a reply to the specified tweet
        /// </param>
        /// <returns>
        /// An <see cref="IObservable{Tweet}"/> which will return the tweet that has been posted
        /// </returns>
        public IObservable<Tweet> PostTweet(string message, string recipient, Tweet replyTo = null)
        {
            return this.PostTweet("@" + recipient + " " + message, replyTo);
        }

        /// <summary>
        /// Likes the given tweet
        /// </summary>
        /// <param name="tweet">
        /// The tweet to like
        /// </param>
        /// <returns>
        /// An <see cref="IObservable{Unit}"/> which calls <see cref="IObserver{T}.OnNext"/> and then completes, once the like has been processed
        /// </returns>
        public IObservable<Unit> Like(Tweet tweet)
        {
            return
                this.SendPost(
                    EndpointUris.LikeUrl, 
                    new Dictionary<string, string> { { "id", tweet.Id.ToString(CultureInfo.InvariantCulture) } })
                    .Select(_ => Unit.Default);
        }

        /// <summary>
        /// Tracks the keywords via the Twitter streaming API
        /// </summary>
        /// <param name="queryString">
        /// The query string containing the keywords to track.
        /// </param>
        /// <param name="scheduler">
        /// The <see cref="IScheduler"/> to run the tracking steps on
        /// </param>
        /// <remarks>
        /// This is a streaming operation.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// You tried to start this streaming operation while another streaming operation is running
        /// </exception>
        /// <returns>
        /// A task that can be awaited to make sure the cancellation has been processed
        /// </returns>
        public IObservable<Tweet> TrackKeywords(string queryString, IScheduler scheduler = null)
        {
            if (scheduler == null)
            {
                scheduler = Scheduler.CurrentThread;
            }

            return Observable.Create<Tweet>(
                observer =>
                    {
                        var d = new MultipleAssignmentDisposable();
                        var b = new BooleanDisposable();
                        var res = new CompositeDisposable(new IDisposable[] { d, b });

                        Action<Action> schedule = a =>
                            {
                                if (b.IsDisposed)
                                {
                                    return;
                                }

                                d.Disposable = scheduler.Schedule(a);
                            };

                        Action sendRequest = () =>
                            {
                                var url = EndpointUris.TrackKeyword + OAuthManager.PercentEncode(queryString);

                                var authenticationHeader = this.manager.GenerateAuthzHeader(url, "GET");

                                d.Disposable =
                                    this.RequestService.CreateGet(
                                        new Uri(url), 
                                        new Dictionary<string, string> { { "Authorization", authenticationHeader } })
                                        .SendAndReadLinewise()
                                        .Where(buffer => !string.IsNullOrWhiteSpace(buffer))
                                        .Select(Tweet.FromJsonString)
                                        .Subscribe(observer);
                            };

                        Action checkStreaming = () =>
                            {
                                if (this.isStreaming)
                                {
                                    observer.OnError(
                                        new InvalidOperationException(
                                            "Only one streaming operation is permitted at a time"));
                                }
                                else
                                {
                                    this.isStreaming = true;
                                    res.Add(Disposable.Create(() => this.isStreaming = false));
                                    schedule(sendRequest);
                                }
                            };

                        schedule(checkStreaming);

                        return res;
                    });
        }

        protected override void Initialize()
        {
            // No bearer token to fetch
        }

        protected override void Dispose(bool disposing)
        {
            // No bearer token to invalidate
        }

        protected override IObservable<string> SendRequest(string url)
        {
            var authenticationHeader = this.manager.GenerateAuthzHeader(url, "GET");

            return
                this.RequestService.CreateGet(
                    new Uri(url), 
                    new Dictionary<string, string> { { "Authorization", authenticationHeader } }).SendAndReadAllText();
        }

        private IObservable<string> SendPost(string url, Dictionary<string, string> postFields)
        {
            var authenticationHeader = this.manager.GenerateAuthzHeader(url, "POST", postFields);

            Action<IWebRequest> configureRequest = r =>
                {
                    r.Method = "POST";
                    r.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                    r.Headers["Authorization"] = authenticationHeader;
                };

            var postData = string.Join(
                "&", 
                postFields.Select(
                    x =>
                    string.Format("{0}={1}", OAuthManager.PercentEncode(x.Key), OAuthManager.PercentEncode(x.Value))));

            return
                this.RequestService.Create(new Uri(url), configureRequest, Encoding.UTF8.GetBytes(postData))
                    .SendAndReadAllText();
        }
    }
}