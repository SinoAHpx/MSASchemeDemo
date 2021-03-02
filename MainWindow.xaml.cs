using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CefSharp.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace MSAD
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Browser.Address =
                @"https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328&response_type=code&scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf";
        }

        private async void Browser_OnAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Browser.Address.Contains("https://login.live.com/oauth20_desktop.srf?code="))
            {
                var MScode = Browser.Address.Replace(@"https://login.live.com/oauth20_desktop.srf?code=", "")
                    .Replace("&lc=1033", "");

                var MStoken = await MSAUtils.GetMSToken(MScode);
                var XBLresult = await MSAUtils.AuthenticateXBL(MStoken);
                var XSTStoken = await MSAUtils.AuthenticateXSTS(XBLresult);

                var accessToken = await MSAUtils.AuthenticateMinecraft(XBLresult, XSTStoken);
                if (await MSAUtils.CheckMCProperty(accessToken))
                    MessageBox.Show("This account has a Minecraft!");
                else
                    MessageBox.Show("This account has no Minecraft!");

                MessageBox.Show(await MSAUtils.GetMcProfile(accessToken));
            }
        }
    }
}
