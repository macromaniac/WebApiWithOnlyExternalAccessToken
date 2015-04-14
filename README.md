# WebApiWithOnlyExternalAccessToken
This is an example of using web api with just an external access token. Some platforms, like chrome extensions or some IOS projects give you an external access token on their own instead of the normal route of getting one through your server, in which case you need to use web api with only an external access token

This example uses google access tokens, but it should be easy to modify to use facebook and other access tokens

The api function in [api/Account](WebApiUsingExternalBearer/Controllers/AccountController.cs) known as GoogleTokenToExternalToken(string googleToken) is what you'd call to get back your access token in a JSON format.

The api call is: "/api/Account/GoogleTokenToExternalToken/?googleToken=YourGoogleTokenHere".

Calling that function will convert a google access token into a server access token which you can use to access controller actions with the [Authorize] tag assuming you have the "Authorization: bearer <your-server-key>" header in your request.

This function WILL NOT WORK until you go into your [GoogleTokenInfo.cs](WebApiUsingExternalBearer/Models/GoogleTokenInfo.cs) module and change expectedAudience to equal your expected audience. The audience is what your access token was generated for, and that part of your code makes sure that your server only works with access tokens generated from your program.

