# ReactiveHUB

Showcasing reactive programming should contain a use-case where incoming data is not triggered by
a user action (e.g. invoking a search or submitting data), but by something beyond the control of
the showcase application itself. On the other hand the showcase should also contain typical user
interaction which may or may not lead to a response in form of additional data.

In my opinion these conditions are satisfied best by crating a message hub similar to [Hootsuite].
This message hub can aggregate messages from [Twitter], [Facebook], [Google+] and others, thus
enabling the user to view the content of all these sites in one central application. Therefore the
application would need to register to incoming messages from all sources in order to display them
and notify the user of new messages. The user should also be able to send a message to a subset of
registered sources.


Mandatory features:
-------------------
* Connection to at least 2 sources ([Twitter], [Facebook], [Google+], [Slack]...)
* Display all messages from all sources in one timeline
* Display new messages as soon as they arrive
* Enable the user to send a message and choose on which service(s) it should be sent.


Optional features:
------------------
/These features are not neccessarily implemented within the Zühlke Topic REACT/

* Source discovery ("Plugins")
* Source-Specific features
 * Like / Favourite / +1
 * Comment / Reply 
 * Share / Retweet
 * Cross-Share (Share a message from service X to service Y)
* Per-source blocklist for senders
* Setting up rules (keywords, users) to sort messages into different lists
* Sound effect on new message (configurable per list)
* Multiple accounts per service
* More Sources
 * Instagram
 * Flikr
* ...and many more


Possible synergies with other topics:
-------------------------------------
* Implementation as CrossPlatform-App (CrossPlatform-Topic)


Additional Links:
-----------------

* https://api.slack.com/
* https://apps.twitter.com/


[Hootsuite]: https://hootsuite.com/
[Twitter]: https://twitter.com/
[Facebook]: https://www.facebook.com/
[Google+]: https://plus.google.com/
[Slack]: https://zuehlke.slack.com