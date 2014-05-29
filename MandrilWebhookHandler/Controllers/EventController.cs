using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MailChimp;
using MailChimp.Helper;
using Mandrill;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Text;
using MandrillWebhookHandler.Properties;

namespace MandrillWebhookHandler.Controllers
{
    public class EventController : ApiController
    {
        private static MandrillApi _mandrillApi;
        private static MailChimpManager _mailChimpApi;

        public EventController()
        {
            _mandrillApi = new MandrillApi(Settings.Default.MandrillApiKey);
            _mailChimpApi = new MailChimpManager(Settings.Default.MailChimpApiKey);
        }

        // POST api/event
        public HttpResponseMessage PostEvent(FormDataCollection value)
        {
            var events = Mandrill.JSON.Parse<List<Mandrill.WebHookEvent>>(value.Get("mandrill_events"));

            MandrillApi api = new MandrillApi(Settings.Default.MandrillApiKey);

            foreach (var evt in events)
            {
                if (HardBounceOrUnsubscribeFromMailChimpList(evt))
                {
                    UnsubscribeEmailfromMailChimpList(evt);
                }
                else if (HardBounceSentFromPeachtreeDataDomain(evt))
                {
                    CreateAndSendEmailMessageFromTemplate(evt);
                }
            }

            return Request.CreateErrorResponse(HttpStatusCode.OK, "Received");
        }

        private static bool HardBounceOrUnsubscribeFromMailChimpList(WebHookEvent evt)
        {
            var metadata = ParseMetadataFromMandrill(evt);
            return metadata.ContainsKey("MCList") && (evt.Event == WebHookEventType.Hard_bounce || evt.Event == WebHookEventType.Unsub);
        }

        private void UnsubscribeEmailfromMailChimpList(WebHookEvent evt)
        {
            var metadata = ParseMetadataFromMandrill(evt);
            try
            {
                var result = _mailChimpApi.Unsubscribe(metadata["MCList"], new EmailParameter() { Email = evt.Msg.Email }, false, false, false);
            }
            catch (Exception)
            {
            }
            
        }

        private void CreateAndSendEmailMessageFromTemplate(WebHookEvent evt)
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


            if (metadata.ContainsKey("CustID"))
                message.AddGlobalVariable("customerID", metadata["CustID"].ToUpper());
            else
                message.AddGlobalVariable("customerID", "Unknown");
            message.AddGlobalVariable("bouncedEmailAddress", evt.Msg.Email);
            message.AddGlobalVariable("application", GetSendingApplicationName(evt));
            message.AddGlobalVariable("timesent", TimeZoneInfo.ConvertTimeFromUtc(evt.Msg.TimeStamp, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString());

            _mandrillApi.SendMessage(message, "mandrill-email-bounce", null);
        }

        private string GetSendingApplicationName(WebHookEvent evt)
        {
            if (evt.Msg.Tags.Contains("SecureFTP"))
                return "Secure FTP Site";
            if (evt.Msg.Subject.Contains("FAST"))
                return "JobTracker - FAST System";

            return string.Format("Unknown Application. Subject line was: {0}", evt.Msg.Subject);
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
