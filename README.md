Mandrill-Webhook-Handler
========================

The purpose of this solution is to provide a listener for WebHook events from Mandrill.

I am specifically filtering for Hard Bounces from Mandrill. 
When a bounce occurs mandrill will fire a WebHook request to the server setup within the Mandrill portal.
This solution is running where I am pointing Mandrill to send the WebHook events to.
When a Hard Bounce is detected then I am sending an email notification to my internal users letting them know.
This allows them to update our system with the correct email address for any customers and be aware 
that the original email was not received.

Obviously this could be extened to any type of event, but this is what works for me in this limited use case.
