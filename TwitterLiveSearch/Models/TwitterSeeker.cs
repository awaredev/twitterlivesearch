using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using PusherServer;
using Timer = System.Timers.Timer;

namespace TwitterLiveSearch.Models
{
    public class TwitterSeeker : IDisposable
    {
        
        private static readonly string Appid = ConfigurationManager.AppSettings["APP_ID"];
        private static readonly string Appkey = ConfigurationManager.AppSettings["APP_KEY"];
        private static readonly string Appsecret = ConfigurationManager.AppSettings["APP_SECRET"];
        private static readonly string TwitterKey = ConfigurationManager.AppSettings["TWITTER_KEY"];
        private static readonly string TwitterSecret = ConfigurationManager.AppSettings["TWITTER_SECRET"];
        private static readonly string TwitterAuthUrl = ConfigurationManager.AppSettings["TWITTER_BEARERURL"];
        private static readonly string TwitterSearchUrl = ConfigurationManager.AppSettings["TWITTER_SEARCHURL"];
        private static readonly double TimerInterval = Convert.ToDouble(ConfigurationManager.AppSettings["SEARCH_INTERVAL"]);
        private static readonly string TwitterResultType = ConfigurationManager.AppSettings["SEARCH_TYPE"];

        public static readonly ConcurrentDictionary<Guid, TwitterSeeker> Seekers = new ConcurrentDictionary<Guid, TwitterSeeker>();

        private readonly string _searchString;
        private readonly string _channel;
        private long _lastTweetId;
        
        private Timer _timer;
        private object _locker = new object();
        private ManualResetEvent _timerDead = new ManualResetEvent(false);


        public TwitterSeeker(Guid id, string searchString)
        {
            _searchString = searchString;
            _channel = id.ToString();
        }

        public void StartSearch()
        {
            if(_timer == null)
                _timer = new Timer(TimerInterval);
            _timer.Elapsed -= TimerOnElapsed;
            _timer.Elapsed += TimerOnElapsed;
            //Do Search, then start timer for next search
            Search();
            _timer.Start();
        }

        public void PauseSearch()
        {
            if(_timer != null)
            {
                lock(_locker)
                {
                    _timerDead.Set();
                    _timer.Stop();
                }       
            }
        }

        public void ResumeSearch()
        {
            if (_timer != null)
            {
                lock (_locker)
                {
                    _timerDead.Reset();
                    _timer.Start();
                }
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock(_locker)
            {
                if (_timerDead.WaitOne(0))
                {
                    return;
                }
                try
                {
                    Search();
                }
                catch (Exception e)
                {
                    //Allows search to continue trying on error after next timer elapse
                }
            }
        }

        private void Search()
        {
            //Pause timer to prevent overlapping searches
            PauseSearch();
            //Validate to Twitter via Bearer Token and initiate search
            var bearerToken = GetBearerToken();
            var tweets = SearchTweets(bearerToken);
            //Send to Pusher if tweets found
            if (tweets != null && tweets.statuses.Any())
            {
                PushTweets(tweets);
            }
            ResumeSearch();
        }

        private void PushTweets(TwitterObject tweets)
        {
            //String containing data to send to pusher
            string pushString = string.Empty;
            //Create Pusher API object
            var pusher = new Pusher(Appid, Appkey, Appsecret);
            //Format found tweets and send when pusher limit reached
            foreach (var tweet in tweets.statuses)
            {
                //Convert tweet to HTML
                var tweetPartial = new TwitterLiveSearch.Views.Home.Tweet() { Model = tweet };
                var tweetStr = tweetPartial.TransformText();
                //If we've maxed out the Pusher length, send it before continuing
                if (pushString.Length + tweetStr.Length > 1024)
                {
                    var pushResult = pusher.Trigger(_channel, "tweets-event", new { tweets = pushString });
                    pushString = string.Empty;
                }
                //Append tweet to current push string
                pushString += tweetStr;
            }
            //If pushString has content after loop is done, push one last time
            if (pushString.Length > 0)
            { var finalPushResult = pusher.Trigger(_channel, "tweets-event", new { tweets = pushString }); }
        }

        private AuthToken GetBearerToken()
        {
            WebRequest request = WebRequest.Create(TwitterAuthUrl);
            string consumerKeyAndSecret = String.Format("{0}:{1}", TwitterKey, TwitterSecret);

            request.Method = "POST";
            request.Headers.Add("Authorization", String.Format("Basic {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(consumerKeyAndSecret))));

            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

            const string postData = "grant_type=client_credentials";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            var stream = request.GetResponse().GetResponseStream();
            if (stream != null)
            {
                var reader = new StreamReader(stream, Encoding.UTF8);
                var responseString = reader.ReadToEnd();
                return (AuthToken)SimpleJson.SimpleJson.DeserializeObject(responseString, typeof(AuthToken));
            }
            return null;
        }

       private TwitterObject SearchTweets(AuthToken token)
       {
           WebRequest request = (_lastTweetId > 0)
                                    ? WebRequest.Create(TwitterSearchUrl + "rpp=100&result_type=" + TwitterResultType +"&q=" + _searchString + "&since_id=" +
                                    _lastTweetId) : WebRequest.Create(TwitterSearchUrl + "rpp=100&result_type=" + TwitterResultType + "&q=" + _searchString);
          
           request.Method = "GET";
           request.Headers.Add("Authorization", String.Format("Bearer {0}", token.access_token));

           request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";

           var stream = request.GetResponse().GetResponseStream();
           if (stream != null)
            {
                var reader = new StreamReader(stream, Encoding.UTF8);
                var responseString = reader.ReadToEnd();
                var tweets = (TwitterObject)SimpleJson.SimpleJson.DeserializeObject(responseString, typeof(TwitterObject));
                if (!tweets.statuses.Any())
                    return null;
                _lastTweetId = tweets.statuses.First().id;
                return tweets;
            }
            return null;
       }

        private void StopSearch()
        {
            PauseSearch();
            _timer.Dispose();
        }

        public void Dispose()
        {
            StopSearch();
        }
        
        private class AuthToken
        {
            public string token_type { get; set; }
            public string access_token { get; set; }
        }


    }

    

}