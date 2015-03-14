Mandrill-Webhook-Handler
========================

The purpose of this solution is to provide a listener for WebHook events from Mandrill.

I have Mandrill configured to fire a webhook when a hard bounce occurs. 

This C# web api application is designed to handle these webhooks from Mandrill.

When a hard bounce occurs, Mandrill will create a webhook request that is sent to this application.
This application will receive this webhook and create and send an email to our internal team alerting 
them about the email bounce.

Other Mandrill events, such as open events or click events, could be setup to be handled as well. 

Addtionally instead of sending an email to a set of users letting them know about the event, this
application could be easily modified to allow it to insert, or update, a record in a database.
