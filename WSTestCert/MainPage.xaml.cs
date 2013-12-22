using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WSTestCert
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
        /// do the loading of the thing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadPFX(object sender, RoutedEventArgs e)
        {
            DoPicking(pass.Text);
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateSeenCerts();
        }

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
        /// Get a list of all certs that are "good"
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<Certificate[]> GetAllCerts(string name)
        {
            var all = await CertificateStores.FindAllAsync();
            return all.Where(c => c.FriendlyName.Contains(name)).ToArray();
        }

        /// <summary>
        /// The user has a thing to use! Lets see how it goes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadMARC21XML(object sender, RoutedEventArgs e)
        {
            Result.Text = "Updating...";
            GetMetaDataTestHC(docid.Text);
        }

        /// <summary>
        /// Do the load via the http client
        /// </summary>
        /// <param name="docid"></param>
        /// <returns></returns>
        private async Task GetMetaDataTestHC(string docid)
        {
            var reqUri = new Uri(string.Format("https://cds.cern.ch/record/{0}/export/xm?ln=en", docid));

            HttpBaseProtocolFilter bpf = new HttpBaseProtocolFilter();
            //bpf.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            bpf.ClientCertificate = (await GetAllCerts("CERN")).First();

            var aClient = new HttpClient(bpf);
            var result = await aClient.GetStringAsync(reqUri);
            if (result.Contains(docid))
            {
                Result.Text = "Got back good XML";
            }
            else
            {
                Result.Text = "Failed to get good XML";
            }
        }
    }
}
