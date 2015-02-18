using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace ReactiveHub.Integration.Twitter
{
    public class Tweet
    {
        public string Message { get; set; }

        public string Sender { get; set; }

        public long Id { get; set; }

        public DateTime Time { get; set; }

        public static Tweet FromJsonString(string inputJson)
        {
            return FromJsonObject(new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(inputJson));
        }

        public static Tweet FromJsonObject(IDictionary<string, object> status)
        {
            return new Tweet
                     {
                         Id = (long)status["id"],
                         Message = (string)status["text"],
                         Sender = (string)((Dictionary<string, object>)status["user"])["screen_name"],
                         Time = DateTime.ParseExact((string)status["created_at"], Constants.TwitterDateFormat, CultureInfo.InvariantCulture)
                     };
        }
    }
}