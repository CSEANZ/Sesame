# Sesame 
A bolt-on voice identification and authentication service for any App or API.

## What is it?

This project allows you to add voice identification to any site compatible with OpenIdConnect based authentication flows. 

The core tenet of the approach taken in this solution is to ensure it doesn't tightly bind our solution to the client site's source code. The end result is an [OAuth2](https://oauth.net/2/)/[OpenID Connect](http://openid.net/connect/) compatible authentication web site that can be used to authenticate using voice from any website that supports an OpenID Connect based authentication flow. 

[Example](https://github.com/CSEANZ/Sesame/tree/master/Samples) client apps provided are for ASP.NET Core and ASP.NET Classic MVC. 

## How does it work?

Take the example of a bot website that is used in a situation where a user cannot interact with a keyboard to enter their authentication information - perhaps they are wearing safety equipment like gloves and goggles. 

![flow](https://user-images.githubusercontent.com/5225782/34549710-e37c1498-f15e-11e7-9b44-4a6d37c70310.jpg)

*Example flow for a bot client site*

There are two main stages to the process - enrolment and verification. 

### Enrolment

Before a user may authenticate with the system by using their voice they must enrol. Enrolment is performed from a PC in any environnement (i.e without personal protective equipment - like an office).

Enrolment involves logging in to [Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/active-directory-whatis) and saving the details of the login (refresh token, user claims) for later.  It does not save any further access tokens or otherwise that could be used to access downstream services such as the Microsoft Graph. 

Once the user has been logged in to AAD they are asked to enrol for voice identification. This process involves selecting a pre-generated phrase and repeating it a number of times.

Once successful the user is presented with a unique PIN which can be later used to help identify them. 

Enrolment can be done up to 90 days (by default) before the user would like to log in.  

### Verification

This system may be suited to a range of scenarios but in this case the example is a bot that is being used in an environment where the user cannot type or be identified using their face. The user must authenticate and commuicate with the bot using only their voice. In this example, the bot must be used in an authenticated state, it cannot be used by a single account that everyone uses. 

During the verification stage, the user approaches the "field station" and kicks off the login process (perhaps by pressing space bar - something that they can do whilst wearing protective equipment in this example). 

Upon activation the user is redirected from the client site to the authentication site (where they earlier enrolled) via OpenId Connect OAuth 2 flows.  Instead of being asked to enroll the user is asked to say their PIN (that was provided during enrolment) aloud. 

If the PIN is accepted then the user will be presented with the phrase they enrolled their voice with and asked the repeat it. If this is successful, the user will be redirected back to the original site and will be authenticated.  



### A note about security

This mode of authentication is weaker than regular 2FA. It is suggested that you limit the capabilities of the client app appropriately. 

