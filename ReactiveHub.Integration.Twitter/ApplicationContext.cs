// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationContext.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the ApplicationContext type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Web;
    using System.Web.Script.Serialization;

    using ProjectTemplate.WebRequests;

    using ReactiveHub.Integration.Twitter.Models;

    public class ApplicationContext : IDisposable
    {
        protected readonly IWebRequestService RequestService;

        /// <summary>
        /// The bearer token is the authentication token when authenticating as application
        /// </summary>
        private IObservable<string> bearerToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        /// <param name="token">The OAuth token for application authentication.</param>
        /// <param name="secret">The secret for the token.</param>
        /// <param name="requestService">The service to use for making web requests</param>
        public ApplicationContext(string token, string secret, IWebRequestService requestService)
        {
            this.RequestService = requestService;

            Token = token;
            Secret = secret;

            Initialize();
        }

        protected string Token { get; set; }

        protected string Secret { get; set; }

        public UserContext CreateUserContext(string userToken, string userSecret)
        {
            return new UserContext(Token, Secret, userToken, userSecret, this.RequestService);
        }

        /// <summary>
        /// Performs a twitter search
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        public IObservable<Tweet> Search(string queryString)
        {
            Trace.WriteLine("Searching for '" + queryString + "'");

            return SearchInternal(string.Format("https://api.twitter.com/1.1/search/tweets.json?q={0}", HttpUtility.UrlEncode(queryString)));
        }

        /// <summary>
        /// Performs a twitter search
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="tweetSinceId">All tweets that are earlier than the tweet specified by this Id are excluded from the search results</param>
        public IObservable<Tweet> Search(string queryString, long tweetSinceId)
        {
            Trace.WriteLine("Searching for '" + queryString + "' ignoring tweets before #" + tweetSinceId);

            return SearchInternal(string.Format("https://api.twitter.com/1.1/search/tweets.json?q={0}&since_id={1}", HttpUtility.UrlEncode(queryString), tweetSinceId));
        }

        /// <summary>
        /// Regularly performs a twitter search and performs an <see cref="Action"/> for each newly found tweet
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="interval">The interval between two searches. It has a minimum of 10s</param>
        /// <param name="scheduler">The scheduler to schedule the polling on</param>
        public IObservable<Tweet> Poll(string queryString, TimeSpan interval, IScheduler scheduler = null)
        {
            return Poll(queryString, 0, interval, scheduler);
        }

        /// <summary>
        /// Regularly performs a twitter search and performs an <see cref="Action"/> for each newly found tweet
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="initialTweetSinceId">If present all tweets that are earlier than the tweet specified by this Id are excluded from the search results</param>
        /// <param name="interval">The interval between two searches. It has a minimum of 10s</param>
        /// <param name="scheduler">The scheduler to schedule the polling on</param>
        public IObservable<Tweet> Poll(string queryString, long initialTweetSinceId, TimeSpan interval, IScheduler scheduler = null)
        {
            if (interval < TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException("interval", "Intervals below 10s are not allowed.");
            }

            return Observable
                .Create<Tweet>(observer =>
                {
                    var b = new BooleanDisposable();
                    var t = new MultipleAssignmentDisposable();
                    var res = new CompositeDisposable(new IDisposable[] {b, t});

                    var recentId = initialTweetSinceId;

                    Action<Tweet> tweetReceived = tweet =>
                    {
                        observer.OnNext(tweet);
                        recentId = Math.Max(recentId, tweet.Id);
                    };

                    Action doSearch = null; 
                    doSearch = () =>
                    {
                        if (b.IsDisposed)
                        {
                            observer.OnCompleted();
                            return;
                        }

                        t.Disposable = Search(queryString, recentId).Subscribe(tweetReceived, observer.OnError, () =>
                        {
                            if (!b.IsDisposed)
                            {
                                t.Disposable = scheduler.Schedule(interval, doSearch);
                            }
                            else
                            {
                                observer.OnCompleted();
                            }
                        });
                    };


                    t.Disposable = scheduler.Schedule(doSearch);

                    return res;
                });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                InvalidateBearerToken().Wait();
            }

            // Release unmanaged resources here
        }

        protected virtual IObservable<string> SendRequest(string url)
        {
            return bearerToken.SelectMany(token => this.RequestService
                .CreateGet(
                    new Uri(url),
                    new Dictionary<string, string>
                        {
                            {
                                "Authorization", "Bearer " + token
                            }
                        })
                .SendAndReadAllText());
        }

        protected virtual void Initialize()
        {
            var base64Credentials = CombineApiKey();

            var bearerTokenSubject = new AsyncSubject<string>();

            bearerToken = bearerTokenSubject;

            this.RequestService.CreatePost(
                new Uri("https://api.twitter.com/oauth2/token?grant_type=client_credentials"),
                "grant_type=client_credentials", new Dictionary<string, string>
                {
                    {
                        "Authorization", "Basic " + base64Credentials
                    }
                }).SendAndReadAllText()
                .Select(data =>
                {
                    var serializer = new JavaScriptSerializer();
                    var json = serializer.Deserialize<Dictionary<string, object>>(data);

                    return (string) json["access_token"];
                }).Subscribe(bearerTokenSubject);
        }

        private IObservable<Unit> InvalidateBearerToken()
        {
            return bearerToken
                .SelectMany(token =>
                    this.RequestService
                        .CreatePost(
                            new Uri("https://api.twitter.com/oauth2/invalidate_token?access_token=" + token),
                            string.Empty,
                            new Dictionary<string, string>
                                {
                                    {
                                        "Authorization", "Basic " + CombineApiKey()
                                    }
                                })
                            .Send());
        }

        private string CombineApiKey()
        {
            var key = HttpUtility.UrlEncode(Token);
            var secret = HttpUtility.UrlEncode(Secret);
            var credentials = key + ":" + secret;
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            return base64Credentials;
        }

        private IObservable<Tweet> SearchInternal(string url)
        {
            return SendRequest(url)
                .SelectMany(reply =>
                {
                    var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reply);
                    var statuses = ((ArrayList)json["statuses"]).OfType<Dictionary<string, object>>();

                    return statuses.Select(Tweet.FromJsonObject).ToObservable();
                });
        }
    }
}