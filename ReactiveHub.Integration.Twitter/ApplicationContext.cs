using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace ReactiveHub.Integration.Twitter
{
    public class ApplicationContext : IDisposable
    {
        public string Token { get; private set; }

        public string Secret { get; private set; }

        /// <summary>
        /// The bearer token is the authentication token when authenticating as application
        /// </summary>
        private IObservable<string> bearerToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        /// <param name="token">The OAuth token for application authentication.</param>
        /// <param name="secret">The secret for the token.</param>
        public ApplicationContext(string token, string secret)
        {
            Token = token;
            Secret = secret;
            FetchBearerToken();
        }

        public UserContext CreateUserContext(string userToken, string userSecret)
        {
            return new UserContext(Token, Secret, userToken, userSecret);
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
        public IObservable<Tweet> Poll(string queryString, TimeSpan interval)
        {
            return Poll(queryString, 0, interval);
        }

        /// <summary>
        /// Regularly performs a twitter search and performs an <see cref="Action"/> for each newly found tweet
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="initialTweetSinceId">If present all tweets that are earlier than the tweet specified by this Id are excluded from the search results</param>
        /// <param name="interval">The interval between two searches. It has a minimum of 10s</param>
        public IObservable<Tweet> Poll(string queryString, long initialTweetSinceId, TimeSpan interval)
        {
            if (interval < TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException("interval", "Intervals below 10s are not allowed.");
            }

            // REVIEW: Is this correct? o.O - I need to send the highest TweedId from the previous searches with each request

            var recentId = initialTweetSinceId;

            return Observable
                .Interval(interval)
                .Select(_ =>
                {
                    var search = Search(queryString, recentId);

                    // Update recent id when the search is finished
                    search.Aggregate(recentId, (l, tweet) => Math.Max(l, tweet.Id)).Subscribe(i => recentId = i);

                    return search;
                })
                .Merge();
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
                InvalidateBearerTokenAsync().Wait();
            }

            // Release unmanaged resources here
        }

        protected virtual IObservable<string> SendRequest(string url)
        {
            return bearerToken.Select(token =>
            {
                var client1 = new WebClient();
                client1.Headers.Add("Authorization", "Bearer " + token);
                var result1 = Observable
                    .FromEvent<DownloadStringCompletedEventHandler, DownloadStringCompletedEventArgs>(
                        handler => client1.DownloadStringCompleted += handler,
                        handler => client1.DownloadStringCompleted -= handler)
                    .Select(args => args.Result)
                    .FirstAsync();
                client1.DownloadStringAsync(new Uri(url));

                return result1;
            }).Merge();
        }

        private void FetchBearerToken()
        {
            var base64Credentials = CombineApiKey();

            var client = new WebClient();
            client.Headers.Add("Authorization", "Basic " + base64Credentials);
            bearerToken = Observable
                .FromEvent<UploadStringCompletedEventHandler, UploadStringCompletedEventArgs>(
                    handler => client.UploadStringCompleted += handler,
                    handler => client.UploadStringCompleted -= handler)
                .Select<UploadStringCompletedEventArgs, string>(args =>
                {
                    var serializer = new JavaScriptSerializer();
                    var json = serializer.Deserialize<Dictionary<string, object>>(args.Result);

                    return (string)json["access_token"];
                }).FirstAsync();

            client.UploadStringAsync(new Uri("https://api.twitter.com/oauth2/token"), "grant_type=client_credentials");
        }

        private IObservable<Unit> InvalidateBearerTokenAsync()
        {
            return bearerToken.Select(token =>
            {
                var client = new WebClient();
                client.Headers.Add("Authorization", "Basic " + CombineApiKey());
                var result = Observable
                    .FromEvent<UploadStringCompletedEventHandler, UploadStringCompletedEventArgs>(
                        handler => client.UploadStringCompleted += handler,
                        handler => client.UploadStringCompleted -= handler)
                    .Select(args => Unit.Default)
                    .FirstAsync();
                client.UploadString("https://api.twitter.com/oauth2/invalidate_token", "access_token=" + token);
                return result;
            })
            .Merge();
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
                .Select(reply =>
                {
                    var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reply);
                    var statuses = ((ArrayList)json["statuses"]).OfType<Dictionary<string, object>>();

                    return statuses.Select(Tweet.FromJsonObject).ToObservable();
                })
                .Merge();
        }
    }
}