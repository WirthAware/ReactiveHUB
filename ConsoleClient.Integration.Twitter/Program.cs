namespace ConsoleClient.Integration.Twitter
{
    using System;
    using System.Configuration;
    using System.Globalization;
    using System.Linq;
    using System.Reactive;
    using System.Threading;

    using ProjectTemplate.WebRequests;

    using ReactiveHub.Integration.Twitter;

    /// <summary>
    /// A console client to run the twitter integration during development without having to open the ReactiveHUB
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            var service = new WebRequestService();
            var observer = Observer.Create<Tweet>(PrintTweet, Error, Done);

            IDisposable context;
            IDisposable subscription;

            switch (args[0].ToLowerInvariant())
            {
                case "search":
                    {
                        if (args.Length < 2)
                        {
                            PrintUsage();
                            return;
                        }

                        var applicationContext = new ApplicationContext(
                            ConfigurationManager.AppSettings["api_key"],
                            ConfigurationManager.AppSettings["api_secret"],
                            service);
                        var queryString = string.Join(" ", args.Skip(1));
                        subscription = applicationContext.Search(queryString).Subscribe(observer);
                        context = applicationContext;

                        break;
                    }

                case "post":
                    {
                        if (args.Length < 2)
                        {
                            PrintUsage();
                            return;
                        }

                        var userContext = new UserContext(
                            ConfigurationManager.AppSettings["api_key"],
                            ConfigurationManager.AppSettings["api_secret"],
                            ConfigurationManager.AppSettings["user_token"],
                            ConfigurationManager.AppSettings["user_secret"],
                            service);

                        var tweetText = string.Join(" ", args.Skip(1));
                        subscription = userContext.PostTweet(tweetText).Subscribe(observer);
                        context = userContext;

                        break;
                    }

                default:
                    PrintUsage();
                    return;
            }

            using (context)
            using (subscription)
            {
                Console.ReadLine();
            }
        }

        private static void Error(Exception obj)
        {
            Console.WriteLine("Exception: {0}", obj);
        }

        private static void PrintTweet(Tweet tweet)
        {
            Console.WriteLine("{0}: {1} ({2:g})", tweet.Sender, tweet.Message, tweet.Time);
        }

        private static void Done()
        {
            Console.WriteLine("DONE");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("The following command line options are available:");
            Console.WriteLine("search <keyword(s)> - searches for a keyword.");
            Console.WriteLine("post <text> - create a new tweet with the defined text");
        }
    }
}
