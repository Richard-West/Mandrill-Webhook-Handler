﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Mandrill;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Text;
using MandrillWebhookHandler.Properties;

namespace MandrillWebhookHandler.Controllers
{
    public class TestController : ApiController
    {
        // POST api/event
        public HttpResponseMessage PostBounce(FormDataCollection value)
        {
            var events = Mandrill.JSON.Parse<List<Mandrill.WebHookEvent>>(value.Get("mandrill_events"));

            MandrillApi api = new MandrillApi(Settings.Default.MandrillApiKey);

            foreach (var evt in events)
            {
                if (evt.Event == WebHookEventType.Hard_bounce && evt.Msg.Sender.ToLower().Contains("peachtreedata.com"))
                {
                    var message = new Mandrill.EmailMessage();

                    message.to = new List<Mandrill.EmailAddress>()
                    {
                        new EmailAddress{
                            email = "rwest@peachtreedata.com"                            
                        }
                    };

                    //DateTime convertedDate = DateTime.SpecifyKind(evt.Msg.TimeStamp, DateTimeKind.UTC);
                    DateTime easternTime = TimeZoneInfo.ConvertTimeFromUtc(evt.Msg.TimeStamp, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

                    message.subject = String.Format("TEST Bounced email notification", evt.Event);
                    message.from_email = "notify@peachtreedata.com";

                    StringBuilder body = new StringBuilder();
                    body.AppendFormat("An email being sent to {0} has bounced.", evt.Msg.Email).AppendLine();
                    body.AppendFormat("Email sent from address: {0}", evt.Msg.Sender).AppendLine();
                    body.AppendFormat("Email subject line: {0}", evt.Msg.Subject).AppendLine();
                    body.AppendLine();
                    body.AppendLine("Please contact the customer and get an updated email address, or remove this email address from all systems.");
                    body.AppendLine("This includes: Goldmine, JobTracker & SecureFTP");
                    body.AppendFormat("Message sent at: {0}", easternTime);
                    body.AppendLine("----");
                    bool hardBounce = evt.Event == WebHookEventType.Hard_bounce;
                    bool containsPeachtreeData = evt.Msg.Sender.ToLower().Contains("peachtreedata.com");
                    bool fullTest = (evt.Event == WebHookEventType.Hard_bounce && evt.Msg.Sender.ToLower().Contains("peachtreedata.com"));
                    body.AppendLine(hardBounce.ToString());
                    body.AppendLine(containsPeachtreeData.ToString());
                    body.AppendLine(fullTest.ToString());



                    message.text = body.ToString();

                    api.SendMessage(message);

                    var myresponse = Request.CreateErrorResponse(HttpStatusCode.Accepted, "Received");
                    return myresponse;
                }
            }

            var response = Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Received");
            return response;
            
        }
    }
}
