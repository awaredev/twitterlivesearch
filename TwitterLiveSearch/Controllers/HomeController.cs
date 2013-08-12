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

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Index(string detail)
        {
            if(!String.IsNullOrWhiteSpace(detail))
            {
                //Create a GUID to identify this search- it will be used to track the channel.
                var seeker = Guid.NewGuid();
                //Create Seeker to search Twitter
                TwitterSeeker.Seekers.TryAdd(seeker, new TwitterSeeker(seeker, detail));
                //TwitterSeeker.Seekers[seeker].StartSearch(); //Local Testing only- online, Webhooks control search
                //Send GUID to View so Pusher JS can subscribe to channel
                ViewBag.Id = seeker.ToString();
                TwitterSeeker.NotifyPusher("New Channel ID " + seeker.ToString() + "Search: " + detail);
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
                TwitterSeeker.Seekers[new Guid(id)].ResumeSearch();
            }
            return new HttpStatusCodeResult(200);
        }

        

    }
}
