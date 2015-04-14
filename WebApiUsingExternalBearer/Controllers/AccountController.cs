using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using WebApiUsingExternalBearer.Models;
using WebApiUsingExternalBearer.Providers;
using WebApiUsingExternalBearer.Results;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace WebApiUsingExternalBearer.Controllers {
	[Authorize]
	[RoutePrefix("api/Account")]
	public class AccountController : ApiController {
		private ApplicationUserManager _userManager;

		protected override void Dispose(bool disposing) {
			if (disposing && _userManager != null) {
				_userManager.Dispose();
				_userManager = null;
			}

			base.Dispose(disposing);
		}

		[HttpPost]
		[AllowAnonymous]
		[Route("GoogleTokenToExternalToken")]
		public async Task<IHttpActionResult> GoogleTokenToExternalToken(string googleToken) {
			var tokenInfo = await GoogleTokenInfo.Generate(googleToken);

			var user = await CreateOrGetUser(tokenInfo.user_id);


			//Identity data that will be associated with an access token
			var identity = new ClaimsIdentity(Startup.OAuthBearerOptions.AuthenticationType);
			identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, tokenInfo.user_id, null, "LOCAL_AUTHORITY"));
			identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, tokenInfo.user_id, null, "Google"));
			identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName, null, "LOCAL_AUTHORITY"));

			var properties = new AuthenticationProperties();
			properties.IssuedUtc = DateTime.UtcNow;
			properties.ExpiresUtc = DateTime.UtcNow.Add(TimeSpan.FromDays(14));

			AuthenticationTicket ticket = new AuthenticationTicket(identity, properties);

			Authentication.SignIn(identity);

			var serverToken = Startup.OAuthBearerOptions.AccessTokenFormat.Protect(ticket);

			JObject data = new JObject(
				new JProperty("serverToken", serverToken));
			return Ok(data);
		}

		private async Task<ApplicationUser> CreateOrGetUser(string userId) {
			var user = await UserManager.FindByIdAsync(userId);
			if (user != null)
				return user;

			user = new ApplicationUser {
				Id = userId,
				UserName = GetRandomLetters(15)
			};

			IdentityResult result = await UserManager.CreateAsync(user);
			if (!result.Succeeded)
				throw new HttpException("Could not create new user:" + String.Join(", ",result.Errors));


			return user;
		}

		private Random rng = new Random();
		private string GetRandomLetters(int numLetters) {
			string possibleLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			string returnString = "";
			for (int i = 0; i < numLetters; ++i)
				returnString += possibleLetters[rng.Next(0, possibleLetters.Length - 1)];
			return returnString;
		}

		public ApplicationUserManager UserManager {
			get {
				return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
			}
			private set {
				_userManager = value;
			}
		}
		private IAuthenticationManager Authentication {
			get { return Request.GetOwinContext().Authentication; }
		}
		#region Helpers


		private IHttpActionResult GetErrorResult(IdentityResult result) {
			if (result == null) {
				return InternalServerError();
			}

			if (!result.Succeeded) {
				if (result.Errors != null) {
					foreach (string error in result.Errors) {
						ModelState.AddModelError("", error);
					}
				}

				if (ModelState.IsValid) {
					// No ModelState errors are available to send, so just return an empty BadRequest.
					return BadRequest();
				}

				return BadRequest(ModelState);
			}

			return null;
		}

		private class ExternalLoginData {
			public string LoginProvider { get; set; }
			public string ProviderKey { get; set; }
			public string UserName { get; set; }

			public IList<Claim> GetClaims() {
				IList<Claim> claims = new List<Claim>();
				claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

				if (UserName != null) {
					claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
				}

				return claims;
			}

			public static ExternalLoginData FromIdentity(ClaimsIdentity identity) {
				if (identity == null) {
					return null;
				}

				Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

				if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
						|| String.IsNullOrEmpty(providerKeyClaim.Value)) {
					return null;
				}

				if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer) {
					return null;
				}

				return new ExternalLoginData {
					LoginProvider = providerKeyClaim.Issuer,
					ProviderKey = providerKeyClaim.Value,
					UserName = identity.FindFirstValue(ClaimTypes.Name)
				};
			}
		}

		private static class RandomOAuthStateGenerator {
			private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

			public static string Generate(int strengthInBits) {
				const int bitsPerByte = 8;

				if (strengthInBits % bitsPerByte != 0) {
					throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
				}

				int strengthInBytes = strengthInBits / bitsPerByte;

				byte[] data = new byte[strengthInBytes];
				_random.GetBytes(data);
				return HttpServerUtility.UrlTokenEncode(data);
			}
		}

		#endregion
	}
}
