using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ReactiveHub.Integration.Twitter
{
    public class UserContext : ApplicationContext
    {
        private bool isStreaming;

        private readonly OAuthManager manager;

        public UserContext(string consumerToken, string consumerSecret, string userToken, string userSecret)
            : base(consumerToken, consumerSecret)
        {
            manager = new OAuthManager(Token, Secret, userToken, userSecret);
        }

        /// <summary>
        /// Sends a tweet to twitter
        /// </summary>
        /// <param name="message">The message to tweet</param>
        /// <param name="replyTo">If specified the new tweet will be a reply to the specified tweet</param>
        /// <returns>A <see cref="Task"/> returning the tweet that has been posted</returns>
        public async Task<Tweet> PostTweetAsync(string message, Tweet replyTo = null)
        {
            const string url = "https://api.twitter.com/1.1/statuses/update.json";

            if (replyTo != null && !message.Contains("@" + replyTo.Sender))
            {
                return await PostTweetAsync(message, replyTo.Sender, replyTo);
            }

            return await Task.Factory.StartNew(
              () =>
              {
                  var postFields = new Dictionary<string, string>
                                                      {
                                                        {"status", message}
                                                      };
                  if (replyTo != null)
                  {
                      postFields.Add("in_reply_to_status_id", replyTo.Id.ToString(CultureInfo.InvariantCulture));
                  }

                  var authenticationHeader = manager.GenerateAuthzHeader(
                    url,
                    "POST",
                    postFields);

                  var postData = string.Join("&", postFields.Select(x => string.Format("{0}={1}", OAuthManager.PercentEncode(x.Key), OAuthManager.PercentEncode(x.Value))));
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

                  var response = request.GetResponse();
                  using (var reader = new StreamReader(response.GetResponseStream()))
                  {
                      return Tweet.FromJsonObject(new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(reader.ReadToEnd()));
                  }
              });
        }

        /// <summary>
        /// Sends a tweet to another twitter user
        /// </summary>
        /// <param name="message">The message to tweet</param>
        /// <param name="recipient">The user the tweet should be sent to</param>
        /// <param name="replyTo">If specified the new tweet will be a reply to the specified tweet</param>
        /// <returns>A <see cref="Task"/> returning the tweet that has been posted</returns>
        public async Task<Tweet> PostTweetAsync(string message, string recipient, Tweet replyTo = null)
        {
            return await PostTweetAsync("@" + recipient + " " + message, replyTo);
        }

        /// <summary>
        /// Likes the given tweet
        /// </summary>
        /// <param name="tweet">The tweet to like</param>
        public Task Like(Tweet tweet)
        {
            const string url = "https://api.twitter.com/1.1/favorites/create.json";
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
              });
        }

        /// <summary>
        /// Tracks the keywords via the Twitter streaming API
        /// </summary>
        /// <param name="queryString">The query string containing the keywords to track.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to stop tracking</param>
        /// <param name="callback">The <see cref="Action"/> to perform for each tweet</param>
        /// <remarks>This is a streaming operation.</remarks>
        /// <exception cref="InvalidOperationException">You tried to start this streaming operation while another streaming operation is running</exception>
        /// <returns>A task that can be awaited to make sure the cancellation has been processed</returns>
        public void TrackKeywords(string queryString, CancellationToken cancellationToken, Action<Tweet> callback)
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
              null);
        }

        protected override Task<string> RequestAsync(string url)
        {
            return Task.Factory.StartNew(
              () =>
              {
                  var authenticationHeader = manager.GenerateAuthzHeader(url, "GET");

                  var request = WebRequest.Create(url);
                  request.Method = "GET";
                  request.Headers.Add("Authorization", authenticationHeader);
                  using (var response = request.GetResponse())
                  using (var reader = new StreamReader(response.GetResponseStream()))
                  {
                      return reader.ReadToEnd();
                  }
              });
        }
    }
}