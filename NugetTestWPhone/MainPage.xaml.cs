using CERNSSO;
using HtmlAgilityPack;
using Microsoft.Phone.Controls;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NugetTestWPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        /// <summary>
        /// Load the title!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadTitle(object sender, System.Windows.RoutedEventArgs e)
        {
            WebAccess.LoadUsernamePassword(Username.Text, Password.Text);
            LoadResponse();
        }

        /// <summary>
        /// Load up the title.
        /// </summary>
        /// <returns></returns>
        private async Task LoadResponse()
        {
            var response = WebAccess.GetWebResponse(new Uri("https://cds.cern.ch/record/1512932?ln=en"));
            TitleText.Text = await ExtractHTMLTitleInfo(await response);
        }

        /// <summary>
        /// Extract the title from the response.
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        public static async Task<string> ExtractHTMLTitleInfo(HttpResponseMessage resp)
        {
            var text = await resp.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(text);
            var titleNode = doc.DocumentNode.Descendants("title").FirstOrDefault();
            if (titleNode == null)
                throw new InvalidDataException("No title node found for the web page!");
            return titleNode.InnerHtml;
        }
    }
}