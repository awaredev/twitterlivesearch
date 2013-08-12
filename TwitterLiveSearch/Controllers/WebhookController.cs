using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TwitterLiveSearch.Models;

namespace TwitterLiveSearch.Controllers
{
    public class WebhookController : Controller
    {
        //
        // POST: /Webhook/Subscribed

        //POST anchor for Pusher Webhook to start and stop search 
        //when Channel is occupied/vacated
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Subscribed()
        {
            //Get the WebHook data from
            var hook = GetWebHook();
            string description = "No Seeker Made";
            foreach (var ev in hook.events)
            {
                //Empty Seeker for Try methods
                TwitterSeeker seeker = null;
                switch (ev.name)
                {
                    case "channel_occupied":
                        //Trigger the Seeker to start searching Twitter
                        if (TwitterSeeker.Seekers.TryGetValue(new Guid(ev.channel), out seeker))
                        {
                            seeker.StartSearch();
                            description = "Created Seeker " + ev.channel;
                        }
                        else
                        {
                            //Alert Pusher of problem
                            return new HttpStatusCodeResult(406, "Could not create Seeker");
                        }
                        break;
                    case "channel_vacated":
                        //Remove Seeker from collection
                        if (TwitterSeeker.Seekers.TryRemove(new Guid(ev.channel), out seeker))
                        {
                            seeker.Dispose();
                        }
                        else
                        {
                            //Alert Pusher of problem
                            return new HttpStatusCodeResult(406, "Could not remove Seeker");
                        }
                        break;
                    default:
                        return new HttpStatusCodeResult(406, "Could not process");
                        break;
                }
            }
            TwitterSeeker.NotifyPusher(description);
            return new HttpStatusCodeResult(200, description);
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
