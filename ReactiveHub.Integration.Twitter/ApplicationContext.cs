using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace ReactiveHub.Integration.Twitter
{
    public class ApplicationContext : IDisposable
    {
        public string Token { get; private set; }

        public string Secret { get; private set; }


        /// <summary>
        /// The format string that is used to parse <see cref="DateTime"/> from the JSON reply
        /// </summary>
        public const string TwitterDateFormat = "ddd MMM dd HH:mm:ss +ffff yyyy";

        /// <summary>
        /// The bearer token is the authentication token when authenticating as application
        /// </summary>
        private string bearerToken = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        /// <param name="token">The OAuth token for application authentication.</param>
        /// <param name="secret">The secret for the token.</param>
        public ApplicationContext(string token, string secret)
        {
            Token = token;
            Secret = secret;
        }

        public UserContext CreateUserContext(string userToken, string userSecret)
        {
            return new UserContext(Token, Secret, userToken, userSecret);
        }

        /// <summary>
        /// Gets the authentication token by authenticating as application
        /// </summary>
        /// <returns>A task that returns the authentication token</returns>
        /// <remarks>
        /// The methods in this library will automatically fetch a token when it is needed.
        /// The token is cached.
        /// </remarks>
        public async Task<string> GetBearerTokenAsync()
        {
            if (!string.IsNullOrEmpty(bearerToken))
            {
                return bearerToken;
            }

            return await Task.Factory.StartNew(
              () =>
              {
                  var base64Credentials = CombineApiKey();

                  var client = new WebClient();
                  client.Headers.Add("Authorization", "Basic " + base64Credentials);
                  var result = client.UploadString("https://api.twitter.com/oauth2/token", "grant_type=client_credentials");

                  var serializer = new JavaScriptSerializer();
                  var json = serializer.Deserialize<Dictionary<string, object>>(result);

                  bearerToken = (string)json["access_token"];

                  Trace.WriteLine("Got bearer token: " + bearerToken, "Information");

                  return bearerToken;
              });
        }

        /// <summary>
        /// Tells twitter that the authentication token is no longer valid.
        /// </summary>
        /// <returns>A task that can be awaited</returns>
        public async Task InvalidateBearerTokenAsync()
        {
            if (string.IsNullOrEmpty(bearerToken))
            {
                return;
            }

            await Task.Factory.StartNew(
              () =>
              {
                  var client = new WebClient();
                  client.Headers.Add("Authorization", "Basic " + CombineApiKey());
                  client.UploadString("https://api.twitter.com/oauth2/invalidate_token", "access_token=" + bearerToken);
                  Trace.WriteLine("Invalidated bearer token");
              });
        }

        /// <summary>
        /// Performs a twitter search
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <returns>A <see cref="Task"/> which returns the tweets found by the search</returns>
        public async Task<IEnumerable<Tweet>> SearchAsync(string queryString)
        {
            Trace.WriteLine("Searching for '" + queryString + "'");

            var reply = await RequestAsync(string.Format("https://api.twitter.com/1.1/search/tweets.json?q={0}", HttpUtility.UrlEncode(queryString)));

            var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reply);
            var statuses = ((ArrayList)json["statuses"]).OfType<Dictionary<string, object>>();

            var result = statuses.Select(Tweet.FromJsonObject).ToList();

            return result;
        }

        /// <summary>
        /// Performs a twitter search
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="tweetSinceId">All tweets that are earlier than the tweet specified by this Id are excluded from the search results</param>
        /// <returns>A <see cref="Task"/> which returns the tweets found by the search</returns>
        public async Task<IEnumerable<Tweet>> SearchAsync(string queryString, long tweetSinceId)
        {
            Trace.WriteLine("Searching for '" + queryString + "' ignoring tweets before " + tweetSinceId);

            var reply = await RequestAsync(string.Format("https://api.twitter.com/1.1/search/tweets.json?q={0}&since_id={1}", HttpUtility.UrlEncode(queryString), tweetSinceId));

            var json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reply);
            var statuses = ((ArrayList)json["statuses"]).OfType<Dictionary<string, object>>();

            var result = statuses.Select(Tweet.FromJsonObject).ToList();

            return result;
        }

        /// <summary>
        /// Regularly performs a twitter search and performs an <see cref="Action"/> for each newly found tweet
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to stop the polling</param>
        /// <param name="callback">The <see cref="Action"/> to perform for each tweet</param>
        /// <param name="interval">The interval between two searches. It has a minimum of 10s</param>
        public void Poll(string queryString, CancellationToken cancellationToken, Action<Tweet> callback, TimeSpan interval)
        {
            Poll(queryString, 0, cancellationToken, callback, interval);
        }

        /// <summary>
        /// Regularly performs a twitter search and performs an <see cref="Action"/> for each newly found tweet
        /// </summary>
        /// <param name="queryString">The term to search for</param>
        /// <param name="initialTweetSinceId">If present all tweets that are earlier than the tweet specified by this Id are excluded from the search results</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to stop the polling</param>
        /// <param name="callback">The <see cref="Action"/> to perform for each tweet</param>
        /// <param name="interval">The interval between two searches. It has a minimum of 10s</param>
        public void Poll(string queryString, long initialTweetSinceId, CancellationToken cancellationToken, Action<Tweet> callback, TimeSpan interval)
        {
            if (interval < TimeSpan.FromSeconds(10))
            {
                throw new ArgumentOutOfRangeException("interval", "Intervals below 10s are not allowed.");
            }

            var pollingThread = new Thread(
              () =>
              {
                  var recentId = initialTweetSinceId;

                  while (!cancellationToken.IsCancellationRequested)
                  {
                      var searchHandle = new ManualResetEvent(false);
                      var searchTask = SearchAsync(queryString, recentId);
                      searchTask.ContinueWith(t => searchHandle.Set(), cancellationToken);

                      WaitHandle.WaitAny(new[] { searchHandle, cancellationToken.WaitHandle });

                      if (cancellationToken.IsCancellationRequested)
                      {
                          return;
                      }

                      var statuses = searchTask.Result.OrderBy(x => x.Id).ToArray();
                      foreach (var tweet in statuses)
                      {
                          callback(tweet);
                      }

                      recentId = statuses.Any() ? statuses.Max(x => x.Id) : recentId;

                      // Wait 60secs or until the cancellationToken has been cancelled
                      cancellationToken.WaitHandle.WaitOne(interval);
                  }
              });
            pollingThread.Start();
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

        protected virtual async Task<string> RequestAsync(string url)
        {
            var client = new WebClient();
            client.Headers.Add("Authorization", "Bearer " + await GetBearerTokenAsync());
            return client.DownloadString(url);
        }

        private string CombineApiKey()
        {
            var key = HttpUtility.UrlEncode(Token);
            var secret = HttpUtility.UrlEncode(Secret);
            var credentials = key + ":" + secret;
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            return base64Credentials;
        }
    }
}