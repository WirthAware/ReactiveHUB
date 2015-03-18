// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Tweet.cs" company="Zühlke Engineering GmbH">
//   Zühlke Engineering GmbH
// </copyright>
// <summary>
//   Defines the Tweet type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReactiveHub.Integration.Twitter.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Web.Script.Serialization;

    using ProjectTemplate.Models;

    public class Tweet : Message
    {
        internal Tweet()
        {
        }

        public long Id { get; set; }

        public static Tweet FromJsonString(string inputJson)
        {
            return FromJsonObject(new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(inputJson));
        }

        public static Tweet FromJsonObject(IDictionary<string, object> status)
        {
            return new Tweet
                     {
                         Id = (long)status["id"],
                         Text = (string)status["text"],
                         Sender = TwitterUser.FromJsonObject((Dictionary<string, object>)status["user"]),
                         TimeStamp = DateTime.ParseExact((string)status["created_at"], Constants.TwitterDateFormat, CultureInfo.InvariantCulture)
                     };
        }
    }
}