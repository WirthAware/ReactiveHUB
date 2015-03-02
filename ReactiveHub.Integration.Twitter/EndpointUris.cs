namespace ReactiveHub.Integration.Twitter
{
    public static class EndpointUris
    {
        public const string LikeUrl = "https://api.twitter.com/1.1/favorites/create.json";

        public const string PostTweetUrl = "https://api.twitter.com/1.1/statuses/update.json";

        public const string TrackKeyword = "https://stream.twitter.com/1.1/statuses/filter.json?track=";
    }
}