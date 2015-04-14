using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace WebApiUsingExternalBearer.Models {
	public class GoogleTokenInfo {
		private GoogleTokenInfo() { }
		// URL: 
		private const string tokenInfoUrl = @"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=";
		//Expected audience:
		private const string expectedAudience = @"156684964143-3v04qssfo6ftgt7m6ni7tf0buq27hqvt.apps.googleusercontent.com";

		// FORMAT: 
		// {
		//	"audience":"8819981768.apps.googleusercontent.com",
		//	"user_id":"123456789",
		//	"scope":"profile email",
		//	"expires_in":436
		// }

		public string audience;
		public string user_id;
		public string scope;
		public string expires_in;

		public async static Task<GoogleTokenInfo> Generate(string googleToken){
			HttpWebRequest wr = WebRequest.CreateHttp(tokenInfoUrl + googleToken);
			WebResponse response;
			try {
				response = await wr.GetResponseAsync();
			}catch(Exception ex){
				throw new HttpException(400, @"Could not retrieve tokeninfo from google, check if token is valid / token has not expired :" + ex.Message);
			}

			var data = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();

			var tokenInfo = JsonConvert.DeserializeObject<GoogleTokenInfo>(data);

			if (tokenInfo.audience != expectedAudience)
				throw new HttpException(400, @"Tokeninfo audiences did not match");

			return tokenInfo;
		}
	}
}