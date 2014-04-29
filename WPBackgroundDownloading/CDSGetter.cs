
using System;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
namespace WPBackgroundDownloading
{
    /// <summary>
    /// a class to help with the stuff that goes after a backgroudn download.
    /// </summary>
    public class CDSGetter
    {
        public static async Task StartDownload(string CDSID, string filename, string version)
        {
            // Build URI for this guy
            var uri = new Uri(string.Format("http://cds.cern.ch/record/{0}/files/{2}?version={1}", CDSID, version, filename));

            // Where is this thing going to be stored? We'll make something up for now.

            var location = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);

            var downloader = new BackgroundDownloader();
            var download = downloader.CreateDownload(uri, location);

            var dwn = await download.StartAsync();
        }
    }
}
