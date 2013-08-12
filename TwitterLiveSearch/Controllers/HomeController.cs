using System;
using System.IO;
using System.Web.Mvc;
using TwitterLiveSearch.Models;


namespace TwitterLiveSearch.Controllers
{
    public class HomeController : Controller
    {
   
        //
        // GET: /Home/

        public ActionResult Index(string id)
        {
            if(!String.IsNullOrWhiteSpace(id))
            {
                //Create a GUID to identify this search- it will be used to track the channel.
                var seeker = Guid.NewGuid();
                //Create Seeker to search Twitter
                TwitterSeeker.Seekers.TryAdd(seeker, new TwitterSeeker(seeker, id));
                //TwitterSeeker.Seekers[seeker].StartSearch(); //Local Testing only- online, Webhooks control search
                //Send GUID to View so Pusher JS can subscribe to channel
                ViewBag.Id = seeker.ToString();
            }
            
            return View();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult ControlSearch(string id, bool pause)
        {
            if(pause)
            {
                TwitterSeeker.Seekers[new Guid(id)].PauseSearch();
            }
            else
            {
                TwitterSeeker.Seekers[new Guid(id)].StartSearch();
            }
            return null;
        }

        //POST anchor for Pusher Webhook to start and stop search 
        //when Channel is occupied/vacated
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Subscribed()
        {
            //Get the WebHook data from
            var hook = GetWebHook();
            foreach (var ev in hook.events)
            {
                //Empty Seeker for Try methods
                TwitterSeeker seeker = null;
                switch (ev.name)
                {
                    case "channel_occupied":
                        //Trigger the Seeker to start searching Twitter
                        if(TwitterSeeker.Seekers.TryGetValue(new Guid(ev.channel), out seeker))
                            seeker.StartSearch();
                        else
                        {
                            //Alert Pusher of problem
                            return new HttpStatusCodeResult(406, "Could not create Seeker");
                        }
                        break;
                    case "channel_vacated" :
                        //Remove Seeker from collection
                        if(TwitterSeeker.Seekers.TryRemove(new Guid(ev.channel), out seeker))
                        {
                            seeker.Dispose();
                        }
                        else
                        {
                            //Alert Pusher of problem
                            return new HttpStatusCodeResult(406, "Could not remove Seeker");
                        }
                        break;
                }
            }
            return new HttpStatusCodeResult(200);
        }

        private WebHook GetWebHook()
        {
            Request.InputStream.Seek(0, 0);
            var reader = new StreamReader(Request.InputStream);
            var inputString = reader.ReadToEnd();
            return (WebHook)SimpleJson.SimpleJson.DeserializeObject(inputString, typeof(WebHook)); 
        }

    }
}
