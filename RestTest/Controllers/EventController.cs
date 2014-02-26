using System;
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
    public class EventController : ApiController
    {
        private static MandrillApi _api;

        public EventController()
        {
            _api = new MandrillApi(Settings.Default.MandrillApiKey);
        }

        // POST api/event
        public HttpResponseMessage PostEvent(FormDataCollection value)
        {
            var events = Mandrill.JSON.Parse<List<Mandrill.WebHookEvent>>(value.Get("mandrill_events"));

            MandrillApi api = new MandrillApi(Settings.Default.MandrillApiKey);

            foreach (var evt in events)
            {
                if (HardBounceSentFromPeachtreeDataDomain(evt))
                {
                    CreateAndSendEmailMessage(evt);
                }
            }

            return Request.CreateErrorResponse(HttpStatusCode.OK, "Received");
        }

        private static void CreateAndSendEmailMessage(WebHookEvent evt)
        {
            var metadata = ParseMetadataFromMandrill(evt);
            var message = new Mandrill.EmailMessage();

            message.to = new List<Mandrill.EmailAddress>()
                    {
                        new EmailAddress{
                            email = "salesteam@peachtreedata.com"
                        },
                        new EmailAddress{
                            email = "rwest@peachtreedata.com"                            
                        }
                    };

            message.subject = String.Format("Bounced email notification", evt.Event);
            message.from_email = "bounce-notifier@peachtreedata.com";

            StringBuilder body = new StringBuilder();
            body.AppendFormat("An email being sent to {0} has bounced.", evt.Msg.Email).AppendLine();
            body.AppendFormat("Email sent from address: {0}", evt.Msg.Sender).AppendLine();
            body.AppendFormat("Email subject line: {0}", evt.Msg.Subject).AppendLine();
            body.AppendLine();
            body.AppendLine("Please contact the customer and get an updated email address, or remove this email address from all systems.");
            body.AppendLine("This includes: Goldmine, JobTracker & SecureFTP");
            body.AppendFormat("Message sent at: {0}", TimeZoneInfo.ConvertTimeFromUtc(evt.Msg.TimeStamp, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"))).AppendLine();
            body.AppendLine("----");

            if (metadata.ContainsKey("CustID"))
            {
                body.AppendFormat("Customer ID: {0}", metadata["CustID"]);
            }

            message.text = body.ToString();

            _api.SendMessage(message);
        }

        private static Dictionary<string, string> ParseMetadataFromMandrill(WebHookEvent evt)
        {
            var metadata = new Dictionary<string, string>();

            if (evt.Msg.Metadata != null)
            {
                metadata = evt.Msg.Metadata.ToDictionary(m => m.Key, m => m.Value);
            }
            return metadata;
        }

        private static bool HardBounceSentFromPeachtreeDataDomain(WebHookEvent evt)
        {
            return evt.Event == WebHookEventType.Hard_bounce && evt.Msg.Sender.ToLower().Contains("peachtreedata.com");
        }
    }
}
