namespace ReactiveHub.Integration.Twitter
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using ProjectTemplate.WebRequests;

    public class UserContext : ApplicationContext
    {
        private readonly OAuthManager manager;

        private bool isStreaming;

        public UserContext(string consumerToken, string consumerSecret, string userToken, string userSecret, IWebRequestService requestService)
            : base(consumerToken, consumerSecret, requestService)
        {
            this.manager = new OAuthManager(Token, Secret, userToken, userSecret);
        }

        /// <summary>
        /// Sends a tweet to twitter
        /// </summary>
        /// <param name="message">The message to tweet</param>
        /// <param name="replyTo">If specified the new tweet will be a reply to the specified tweet</param>
        /// <returns>An <see cref="IObservable{Tweet}"/> which will return the tweet that has been posted</returns>
        public IObservable<Tweet> PostTweet(string message, Tweet replyTo = null)
        {
            const string Url = "https://api.twitter.com/1.1/statuses/update.json";

            if (replyTo != null && !message.Contains("@" + replyTo.Sender))
            {
                return this.PostTweet(message, replyTo.Sender, replyTo);
            }

            var postFields = new Dictionary<string, string> { { "status", message } };
            if (replyTo != null)
            {
                postFields.Add("in_reply_to_status_id", replyTo.Id.ToString(CultureInfo.InvariantCulture));
            }

            return this.SendPost(Url, postFields).Select(Tweet.FromJsonString);
        }

        /// <summary>
        /// Sends a tweet publicly to another twitter user
        /// </summary>
        /// <param name="message">The message to tweet</param>
        /// <param name="recipient">The user the tweet should be sent to</param>
        /// <param name="replyTo">If specified the new tweet will be a reply to the specified tweet</param>
        /// <returns>An <see cref="IObservable{Tweet}"/> which will return the tweet that has been posted</returns>
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
            const string Url = "https://api.twitter.com/1.1/favorites/create.json";

            return SendPost(Url, new Dictionary<string, string>
            {
                {
                    "id",
                    tweet.Id.ToString(
                        CultureInfo.InvariantCulture)
                }
            }).Select(_ => Unit.Default);

            /* Old code:
            return Task.Factory.StartNew(
              () =>
              {
                  var postFields = new Dictionary<string, string>
                                                      {
                                                        {
                                                          "id",
                                                          tweet.Id.ToString(
                                                            CultureInfo.InvariantCulture)
                                                        }
                                                      };
                  var authenticationHeader = manager.GenerateAuthzHeader(url, "POST", postFields);

                  var postData = string.Join(
                    "&",
                    postFields.Select(
                      x => string.Format("{0}={1}", OAuthManager.PercentEncode(x.Key), OAuthManager.PercentEncode(x.Value))));
                  var request = (HttpWebRequest)WebRequest.Create(url);
                  request.Method = "POST";

                  var byteArray = Encoding.UTF8.GetBytes(postData);
                  request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                  request.Headers.Add("Authorization", authenticationHeader);
                  request.ContentLength = byteArray.Length;
                  using (var dataStream = request.GetRequestStream())
                  {
                      dataStream.Write(byteArray, 0, byteArray.Length);
                  }

                  request.GetResponse();
              });*/
        }

        /// <summary>
        /// Tracks the keywords via the Twitter streaming API
        /// </summary>
        /// <param name="queryString">The query string containing the keywords to track.</param>
        /// <remarks>This is a streaming operation.</remarks>
        /// <exception cref="InvalidOperationException">You tried to start this streaming operation while another streaming operation is running</exception>
        /// <returns>A task that can be awaited to make sure the cancellation has been processed</returns>
        public IObservable<Tweet> TrackKeywords(string queryString)
        {
            if (isStreaming)
            {
                throw new InvalidOperationException("Only one streaming operation is permitted at a time");
            }

            isStreaming = true;

            var url = "https://stream.twitter.com/1.1/statuses/filter.json?track=" + OAuthManager.PercentEncode(queryString);

            var authenticationHeader = manager.GenerateAuthzHeader(url, "GET");

            var request = WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", authenticationHeader);

            /*
             * REVIEW:
             * - response is not disposed
             * - reader is not disposed
             * - will the endless Observable.Generate terminate when I unsubscribe? -> NO!
             */
            return request.GetResponseAsync()
                .ToObservable()
                .Select(response => new StreamReader(response.GetResponseStream()))
                .Select(reader => Observable.Generate(string.Empty, _ => true, _ => reader.ReadLine(), x => x))
                .Merge()
                .Where(buffer => !string.IsNullOrWhiteSpace(buffer))
                .Select(Tweet.FromJsonString);

            /* Old non-reactive code, this snippet is based on:
                        request.BeginGetResponse(
                          result =>
                          {
                              try
                              {
                                  using (var response = request.EndGetResponse(result))
                                  {
                                      using (var reader = new StreamReader(response.GetResponseStream()))
                                      {
                                          var buffer = string.Empty;
                                          var nextChar = reader.Read();
                                          while (nextChar != -1 && !cancellationToken.IsCancellationRequested)
                                          {
                                              buffer += (char)nextChar;
                                              Trace.Write((char)nextChar);

                                              if (buffer.EndsWith("\r\n"))
                                              {
                                                  if (buffer != "\r\n")
                                                  {
                                                      try
                                                      {
                                                          callback(Tweet.FromJsonString(buffer));
                                                      }
                                                      catch (Exception e)
                                                      {
                                                          Trace.WriteLine("Exception in Tweet callback: " + e.Message);
                                                      }
                                                  }

                                                  buffer = string.Empty;
                                              }

                                              nextChar = reader.Read();
                                          }
                                      }
                                  }
                              }
                              finally
                              {
                                  isStreaming = false;
                              }
                          },
                          null);*/
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
            var authenticationHeader = manager.GenerateAuthzHeader(url, "GET");

            return
                this.RequestService.CreateGet(new Uri(url),
                    new Dictionary<string, string> {{"Authorization", authenticationHeader}}).SendAndReadAllText();
        }

        private IObservable<string> SendPost(string url, Dictionary<string, string> postFields)
        {
            var authenticationHeader = manager.GenerateAuthzHeader(url, "POST", postFields);

            Action<IWebRequest> configureRequest = r =>
                {
                    r.Method = "POST";
                    r.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
                    r.Headers["Authorization"] = authenticationHeader;
                };

            var postData = string.Join(
              "&",
              postFields.Select(
                x => string.Format("{0}={1}", OAuthManager.PercentEncode(x.Key), OAuthManager.PercentEncode(x.Value))));

            return this.RequestService.Create(new Uri(url), configureRequest, Encoding.UTF8.GetBytes(postData)).SendAndReadAllText();
        }
    }
}