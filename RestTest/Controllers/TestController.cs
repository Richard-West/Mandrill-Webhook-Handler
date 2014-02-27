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
    public class TestController : ApiController
    {
        private static MandrillApi _api;

        public TestController()
        {
            _api = new MandrillApi(Settings.Default.MandrillApiKey);
        }

        // POST api/event
        public HttpResponseMessage PostBounce(FormDataCollection value)
        {
            var events = Mandrill.JSON.Parse<List<Mandrill.WebHookEvent>>(value.Get("mandrill_events"));

            foreach (var evt in events)
            {
                if (HardBounceSentFromPeachtreeData(evt))
                {
                    //CreateAndSendEmailMessage(evt);
                    CreateAndSendEmailMessageFromTemplate(evt);
                    var myresponse = Request.CreateErrorResponse(HttpStatusCode.Accepted, "Received");
                    return myresponse;
                }
            }

            var response = Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Received");
            return response;
        }

        private void CreateAndSendEmailMessageFromTemplate(WebHookEvent evt)
        {
            var metadata = ParseMetadataFromMandrill(evt);
            var message = new Mandrill.EmailMessage();

            message.to = new List<Mandrill.EmailAddress>()
                    {
                        new EmailAddress{
                            email = "rwest@peachtreedata.com"                            
                        }
                    };

            message.subject = String.Format("TEST Bounced email notification", evt.Event);
            message.from_email = "notify@peachtreedata.com";


            if (metadata.ContainsKey("CustID"))
                message.AddGlobalVariable("customerID", metadata["CustID"].ToUpper());
            else
                message.AddGlobalVariable("customerID", "Unknown");
            message.AddGlobalVariable("bouncedEmailAddress", evt.Msg.Email);
            message.AddGlobalVariable("application", GetSendingApplicationName(evt));
            message.AddGlobalVariable("timesent", TimeZoneInfo.ConvertTimeFromUtc(evt.Msg.TimeStamp, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).ToString());

            var content = new List<TemplateContent>();
            var item = new TemplateContent();
            item.name = "body_content";
            item.content = "";

            content.Add(item);

            _api.SendMessage(message, "mandrill-email-bounce", null);
        }

        private string GetSendingApplicationName(WebHookEvent evt)
        {
            if (evt.Msg.Tags.Contains("SecureFTP"))
                return "Secure FTP Site";
            if (evt.Msg.Subject.Contains("FAST"))
                return "JobTracker - FAST System";

            return string.Format("Unknown Application. Subject line was: {0}", evt.Msg.Subject);
        }



        private static void CreateAndSendEmailMessage(WebHookEvent evt)
        {
            var metadata = ParseMetadataFromMandrill(evt);
            var message = new Mandrill.EmailMessage();

            message.to = new List<Mandrill.EmailAddress>()
                    {
                        new EmailAddress{
                            email = "rwest@peachtreedata.com"                            
                        }
                    };

            message.subject = String.Format("TEST Bounced email notification", evt.Event);
            message.from_email = "notify@peachtreedata.com";

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
                body.AppendLine();
            }

            if (evt.Msg.Tags != null)
            {
                body.Append("System Tags:");
                foreach (string tag in evt.Msg.Tags)
                {
                    body.Append(" "+tag);
                }
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

        private static bool HardBounceSentFromPeachtreeData(WebHookEvent evt)
        {
            return true;
            //return evt.Event == WebHookEventType.Hard_bounce && evt.Msg.Sender.ToLower().Contains("peachtreedata.com");
        }
    }
}
