using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace MSAD
{
    public static class MSAUtils
    {
        private static void output(params object[] info)
        {
            var mainWindows = Application.Current.MainWindow as MainWindow;

            mainWindows?.OutputText.AppendText($"[{DateTime.Now:T}] {string.Join("", info)}\r\n");
            //mainWindows?.OutputText.Focus();
            //mainWindows?.OutputText.Select((int) (mainWindows?.OutputText.Text.Length), 0);
            mainWindows?.OutputText.ScrollToEnd();
        }

        public static async Task<string> GetMSToken(string MScode)
        {
            var rest = new RestClient("https://login.live.com/oauth20_token.srf");
            rest.AddDefaultQueryParameter("client_id", "00000000402b5328");
            rest.AddDefaultQueryParameter("code", MScode);
            rest.AddDefaultQueryParameter("grant_type", "authorization_code");
            rest.AddDefaultQueryParameter("redirect_uri", "https://login.live.com/oauth20_desktop.srf");
            rest.AddDefaultQueryParameter("scope", "service::user.auth.xboxlive.com::MBI_SSL");

            rest.AddDefaultHeader("Content-Type", "application/x-www-form-urlencoded");

            var res = await rest.ExecuteGetAsync(new RestRequest());
            var json = JObject.Parse(res.Content);
            output("Response json is: ", json);

            var token = json["access_token"].ToString();
            output("Authorization token is: ", token);

            return token;
        }

        public static async Task<Dictionary<string, string>> AuthenticateXBL(string MSToken)
        {
            //https://user.auth.xboxlive.com/user/authenticate
            var re = new Dictionary<string, string>();

            var payload = JsonConvert.SerializeObject(new
            {
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = MSToken
                },
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT"
            });
            output("XBL payload json is: ", payload);

            var rest = new RestClient("https://user.auth.xboxlive.com/user/authenticate");
            var req = new RestRequest();
            req.AddJsonBody(payload);

            var res = (await rest.ExecutePostAsync(req)).Content;
            output("XBL response json is: ", res);

            var json = JObject.Parse(res);

            re.Add("token", json["Token"].ToString());
            output("XBL token is: ", re["token"]);

            re.Add("uhs", JArray.Parse(json["DisplayClaims"]["xui"].ToString()).First["uhs"].ToString());
            output("XBL uhs is: ", re["uhs"]);

            return re;
        }

        public static async Task<string> AuthenticateXSTS(Dictionary<string, string> XBLresult)
        {
            //https://xsts.auth.xboxlive.com/xsts/authorize

            var payload = JsonConvert.SerializeObject(new
            {
                Properties = new
                {
                    SandboxId = "RETAIL",
                    UserTokens = new[] { XBLresult["token"] }
                },
                RelyingParty = "rp://api.minecraftservices.com/",
                TokenType = "JWT"
            });
            output("XBTS payload json is: ", payload);

            var rest = new RestClient("https://xsts.auth.xboxlive.com/xsts/authorize");
            var req = new RestRequest();
            req.AddJsonBody(payload);

            var res = (await rest.ExecutePostAsync(req)).Content;
            output("XBTS response json is: ", res);

            var XSTStoken = JObject.Parse(res)["Token"].ToString();
            output("XSTS token is: ", XSTStoken);

            return XSTStoken;
        }

        public static async Task<string> AuthenticateMinecraft(Dictionary<string, string> XBLresult, string XSTStoken)
        {
            //https://api.minecraftservices.com/authentication/login_with_xbox

            var payload = JsonConvert.SerializeObject(new
            {
                identityToken = $"XBL3.0 x={XBLresult["uhs"]};{XSTStoken}"
            });
            output("Minecraft authenticate token is: ", payload);

            var rest = new RestClient("https://api.minecraftservices.com/authentication/login_with_xbox");
            var req = new RestRequest();
            req.AddJsonBody(payload);

            var res = (await rest.ExecutePostAsync(req)).Content;
            output("Minecraft authenticate json is: ", res);

            var MCtoken = JObject.Parse(res)["access_token"].ToString();
            output("Minecraft access token is: ", MCtoken);

            return MCtoken;
        }

        public static async Task<bool> CheckMCProperty(string MCtoken)
        {
            //https://api.minecraftservices.com/entitlements/mcstore

            var rest = new RestClient("https://api.minecraftservices.com/entitlements/mcstore");
            rest.AddDefaultHeader("Authorization", $"Bearer {MCtoken}");

            var res = (await rest.ExecuteGetAsync(new RestRequest())).Content;
            output("Minecraft property json is: ", res);

            var jsonArr = JArray.Parse(JObject.Parse(res)["items"].ToString());

            return jsonArr.Count > 0;
        }

        public static async Task<string> GetMcProfile(string MCtoken)
        {
            //https://api.minecraftservices.com/minecraft/profile
            var rest = new RestClient("https://api.minecraftservices.com/minecraft/profile");
            rest.AddDefaultHeader("Authorization", $"Bearer {MCtoken}");

            var res = (await rest.ExecuteGetAsync(new RestRequest())).Content;
            output("Minecraft profile json is: ", res);

            return res;
        }
    }
}