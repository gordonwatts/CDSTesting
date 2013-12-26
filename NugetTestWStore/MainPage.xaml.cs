using CERNSSO;
using HtmlAgilityPack;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NugetTestWStore
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// We are loading up a thing via the username and password.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadViaUserName(object sender, RoutedEventArgs e)
        {
            WebAccess.ResetCredentials();
            WebAccess.LoadUsernamePassword(Username.Text, Password.Text);
            TitleResult.Text = "running...";
            StartReadback();
        }

        private void LoadByCert(object sender, RoutedEventArgs e)
        {
            WebAccess.ResetCredentials();
            WebAccess.UseCertificateStore();
            TitleResult.Text = "running...";
            StartReadback();
        }

        /// <summary>
        /// Get the title back!
        /// </summary>
        /// <returns></returns>
        private async Task StartReadback()
        {
            var response = WebAccess.GetWebResponse(new Uri("https://cds.cern.ch/record/1512932?ln=en"));
            TitleResult.Text = await ExtractHTMLTitleInfo(await response);
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateSeenCerts();
        }

        /// <summary>
        /// Display to user how many of these certs we can find so they have some sort of a hint.
        /// </summary>
        /// <returns></returns>
        private async Task UpdateSeenCerts()
        {
            // Read them all back

            var all = await CertificateStores.FindAllAsync();
            StringBuilder bld = new StringBuilder();
            bool first = true;
            foreach (var c in all)
            {
                if (!first)
                    bld.Append(", ");
                first = false;
                bld.Append(c.FriendlyName);
            }

            CertsSeen.Text = bld.ToString();
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
                return "nothing found";
            return titleNode.InnerHtml;
        }

        /// <summary>
        /// User wants to add a cert to the WS cache!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCertFromFile(object sender, RoutedEventArgs e)
        {
            DoPicking(CertPass.Text);
        }

        /// <summary>
        /// Fetch the cert and load it into our internal store.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private async Task DoPicking(string password)
        {
            var picker = new FileOpenPicker();

            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".pfx");
            picker.ViewMode = PickerViewMode.List;

            var file = await picker.PickSingleFileAsync();

            IBuffer buffer = await FileIO.ReadBufferAsync(file);

            string certificateData = CryptographicBuffer.EncodeToBase64String(buffer);

            await CertificateEnrollmentManager.ImportPfxDataAsync(
                    certificateData,
                    password,
                    ExportOption.NotExportable,
                    KeyProtectionLevel.NoConsent,
                    InstallOptions.None,
                    file.DisplayName);

            await UpdateSeenCerts();
        }

    }
}
