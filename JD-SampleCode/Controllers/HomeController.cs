using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace JD_SampleCode.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Connect()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://sandboxapi.deere.com/platform/oauth/request_token");

            var dateTime = DateTime.UtcNow;

            // ADD YOUR APP ID AND SHARED SECRET HERE
            var app_id = "your-app-id-goes-here";
            var shared_secret = "your-shared-secret-goes-here";

            // nonce and timestamp code are taken directly from John Deere sample code
            string digit = "1234567890";
            string lower = "abcdefghijklmnopqrstuvwxyz";
            string chars = (lower + digit);

            Random _random = new Random();
            var nonce = new char[16];
            for (var i = 0; i < nonce.Length; i++)
            {
                nonce[i] = chars[_random.Next(0, chars.Length)];
            }

            var timeSpan = (dateTime - new DateTime(1970, 1, 1));
            var timestamp = (long)timeSpan.TotalSeconds;

            List<string> oauthParams = new List<string>();
            // the order the parameters are added here matters
            oauthParams.Add($"oauth_callback={Uri.EscapeDataString("http://localhost:13822/Home/OAuthCallback")}");
            oauthParams.Add($"oauth_consumer_key={app_id}");
            oauthParams.Add($"oauth_nonce={new string(nonce)}");
            oauthParams.Add("oauth_signature_method=HMAC-SHA1");
            oauthParams.Add($"oauth_timestamp={timestamp}");
            oauthParams.Add("oauth_version=1.0");

            var requestMethod = "GET&";
            // the URI to John Deeres Request Token API is hard coded
            // per John Deere ALL URI's should be retrieved from the catalog API
            var requestUrl = Uri.EscapeDataString("https://sandboxapi.deere.com/platform/oauth/request_token");
            var requestString = string.Join("&", oauthParams);
            var requestParameters = Uri.EscapeDataString(requestString);

            var crypto = new HMACSHA1();
            var signatureKey = string.Format("{0}&{1}", shared_secret, "");
            crypto.Key = Encoding.UTF8.GetBytes(signatureKey);

            var dataString = requestMethod + requestUrl + "&" + requestParameters;
            var data = Encoding.UTF8.GetBytes(dataString);
            var hash = crypto.ComputeHash(data);
            var signature = Uri.EscapeDataString(Convert.ToBase64String(hash));

            // again, the order the parameters are added to the header matters
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", $"oauth_callback=\"{Uri.EscapeDataString("http://localhost:13822/Home/OAuthCallback")}\", oauth_consumer_key=\"{app_id}\", oauth_nonce=\"{new string(nonce)}\", oauth_signature=\"{signature}\", oauth_signature_method=\"HMAC-SHA1\", oauth_timestamp=\"{timestamp}\", oauth_version=\"1.0\"");

            HttpClient client = new HttpClient();
            var response = await client.SendAsync(request);
            
            var responseContent = await response.Content.ReadAsStringAsync();
            // if it fails here, chances are there was an error, review the response object, and debug the error
            var responseList = responseContent.Split('&');
            var oauthToken = responseList.First(s => s.Contains("oauth_token"));
            var tokenValue = oauthToken.Split('=')[1];

            return Redirect(string.Format("https://my.deere.com/consentToUseOfData?oauth_token={0}", tokenValue));
        }

        public ActionResult OAuthCallback()
        {
            // At this point you will get the token, and can use it to make John Deere API calls
            return RedirectToAction("Index", "Home");
        }
    }
}