using Microsoft.Live;
using Microsoft.Band;
using Microsoft.Band.Notifications;
using Microsoft.Band.Sensors;
using Microsoft.Band.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Text;
using BoxKite.Twitter.Authentication;
using BoxKite.Twitter.Models;
using BoxKite.Twitter;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace App12
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private Geolocator _geolocator = null;
        private IBandClient bandClient;

        private Geoposition pos;

        private Geoposition lap;
        private DateTime timeLastLap;
        private TimeSpan elapsedTimeLastLap;
        double minLapLat;
        double minLapLong;
        double maxLapLat;
        double maxLapLong;

        int _lapCount = 0;

        private int _heartRate;
        private double _skinTemperature;
        double _skinTemperatureMax;
        double _skinTemperatureSum;
        double _skinTemperatureReadCount;
        double _skinTemperatureAvg;

        private Boolean connected;

        DispatcherTimer _1secondTimer;

        int _heartRateMax;
        int _heartRateSum;
        int _heartRateReadCount;
        int _heartRateAvg;

        int countHeartRateTime;
        int countTempTime;

        String fileName;

        Guid _tileGuid;

        DisplayRequest dr;

        string _notifications = "";

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            bool fileCreated = await createGPXFile();

            if (fileCreated == false)
            {
                tbLive.Text = "file NOT created";
                return;
            }

            dr = new DisplayRequest();
            dr.RequestActive();

            _geolocator = new Geolocator();
            // Desired Accuracy needs to be set
            // before polling for desired accuracy.
            _geolocator.DesiredAccuracy = PositionAccuracy.High;
            _geolocator.DesiredAccuracyInMeters = 1;
            _geolocator.MovementThreshold = 1;
            try
            {
                _geolocator.PositionChanged += _geolocator_PositionChanged;
                pos = await _geolocator.GetGeopositionAsync();
            }
            catch (Exception ex)
            {
                tbLat.Text = ex.ToString();                
            }

            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.tbHeart.Text = "band nao detectado";
                    return;
                }

                // Connect to Microsoft Band.
                using (bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    connected = true;

                    await CreateOrGetATileGuid();

                    bandClient.SensorManager.HeartRate.ReadingChanged += HeartRate_ReadingChanged;
                    await bandClient.SensorManager.HeartRate.StartReadingsAsync();

                    bandClient.SensorManager.SkinTemperature.ReadingChanged += SkinTemperature_ReadingChanged;
                    await bandClient.SensorManager.SkinTemperature.StartReadingsAsync();

                    _1secondTimer = new DispatcherTimer();
                    _1secondTimer.Tick += _1secondTimer_Tick;
                    _1secondTimer.Interval = new TimeSpan(0, 0, 1);
                    _1secondTimer.Start();

                    while (connected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }

                    await bandClient.SensorManager.HeartRate.StopReadingsAsync();
                    await bandClient.SensorManager.SkinTemperature.StopReadingsAsync();
                    dr.RequestRelease();
                    _1secondTimer.Stop();

                    writeEndOfGPX();


                }
            }
            catch (Exception ex)
            {
                this.tbHeart.Text = ex.ToString();
            }
        }

        private String GPXheader = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>\n" +
"<gpx xmlns=\"http://www.topografix.com/GPX/1/1\" xmlns:gpxtpx=\"http://www.garmin.com/xmlschemas/TrackPointExtension/v1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" creator=\"Sports Tracker\" version=\"1.1\" xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd http://www.garmin.com/xmlschemas/TrackPointExtension/v1 http://www.garmin.com/xmlschemas/TrackPointExtensionv1.xsd\">\n" +
"  <metadata>\n" +
"    <name>3/29/2015 19:23 PM</name>\n" +
"    <desc>kart band</desc>\n" +
"    <author>\n" +
"      <name>Marlon Luz</name>\n" +
"    </author>\n" +
"    <link href=\"www.sports-tracker.com\">\n" +
"      <text>Sports Tracker</text>\n" +
"    </link>\n" +
"  </metadata>\n" +
"  <trk>\n" +
"    <trkseg>\n";


        StorageFile file;
        private async Task<bool> createGPXFile()
        {
            try
            {
                SettingsManager sm = new SettingsManager();
                int index = sm.GetValue("index", 5);
                index++;
                StorageFolder folder = ApplicationData.Current.LocalFolder;

                fileName = "kart" + index + ".gpx";

                file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                await writeString(GPXheader);

                sm.SetValue("index", index);

            }
            catch (Exception ex)
            {
                tbLive.Text = ex.Message;
            }

            return true;
        }

        private async Task writeString(string data)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            Byte[] bytes = encoding.GetBytes(data);

            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                ulong x = fileStream.Size;
                using (IOutputStream outputStream = fileStream.GetOutputStreamAt(fileStream.Size))
                {
                    using (DataWriter dataWriter = new DataWriter(outputStream))
                    {
                        dataWriter.WriteBytes(bytes);
                        await dataWriter.StoreAsync();
                        dataWriter.DetachStream();
                    }

                    await outputStream.FlushAsync();
                }
            }
        }

        private async void writeEndOfGPX()
        {
            string endOfGPX = "    </trkseg>\n" + "  </trk>\n" + "</gpx>\n";

            await writeString(endOfGPX);

            await writeString(_notifications);

        }

        private async Task addTrackToGPX()
        {
            string dateNow = "";
            string timeNow = "";

            try
            {
                dateNow = String.Format("{0:yyyy-MM-dd}", DateTime.Now);//"2015-03-26T17:05:38";
                timeNow = String.Format("{0:HH:mm:ss}", DateTime.Now);

            }
            catch(Exception ex)
            {
                tbLive.Text = ex.Message;
            }

            String nodo = "<trkpt lat=\"" + pos.Coordinate.Latitude + "\" lon=\"" + pos.Coordinate.Longitude + "\" >\n" +
                "  <ele>"+pos.Coordinate.Altitude+"</ele>\n"+
                "  <time>" + dateNow + "T" + timeNow + "Z</time>\n"+
                "  <extensions>\n"+
                "    <gpxtpx:TrackPointExtension>\n"+
		        "      <gpxtpx:hr>"+ _heartRate + "</gpxtpx:hr>\n"+
		        "      <gpxtpx:atemp>"+ (int)_skinTemperature + "</gpxtpx:atemp>\n"+
		        "    </gpxtpx:TrackPointExtension>\n"+			  
		        "  </extensions>\n"+		
                "</trkpt>\n";

            await writeString(nodo);
        }

        private async Task CreateOrGetATileGuid()
        {
            SettingsManager sm = new SettingsManager();
            _tileGuid = sm.GetValue("GUID", Guid.Empty);
            if (_tileGuid != Guid.Empty)
            {
                return;
            }
            else
            {

                BandIcon smallIcon = await LoadIcon("ms-appx:///Assets/capacete24.png");

                BandIcon tileIcon = await LoadIcon("ms-appx:///Assets/capacete46.png");

                _tileGuid = Guid.NewGuid();

                BandTile tile = new BandTile(_tileGuid)
                {
                    // enable badging (the count of unread messages)
                    IsBadgingEnabled = true,
                    // set the name
                    Name = "kart",
                    // set the icons
                    SmallIcon = smallIcon,
                    TileIcon = tileIcon
                };

                try
                {
                     // add the tile to the Band
                     if (await bandClient.TileManager.AddTileAsync(tile))
                     {
                          // do work if the tile was successfully created
                         sm.SetValue("GUID", _tileGuid);
                     }
                }
                catch(BandException ex)
                {
                 // handle a Band connection excpetion
                }

            }

        }

        async void _1secondTimer_Tick(object sender, object e)
        {
            // temperature
            string text = "temperature: " + _skinTemperature;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.tbTemp.Text = text; }).AsTask();

            //heart rate
            string text2 = "heart rate: " + _heartRate;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.tbHeart.Text = text2; }).AsTask();

            //gps
            if (pos != null)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.tbLat.Text = "Lat: " + pos.Coordinate.Point.Position.Latitude; }).AsTask();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.tbLong.Text = "Long: " + pos.Coordinate.Point.Position.Longitude; }).AsTask();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.tbAlt.Text = "Altitude: " + pos.Coordinate.Point.Position.Altitude; }).AsTask();
            }

            await addTrackToGPX();

            countHeartRateTime++;
            if (countHeartRateTime >180 && _tileGuid != Guid.Empty)// heart rate notification each 3 minutes
            {
                countHeartRateTime = 0;
                try
                {
                    tbHeartRate.Text += "MAX:" + _heartRateMax + " AVG:" + _heartRateAvg + "/";

                    await sendBandNotification("HeartRate", "MAX:" + _heartRateMax + " AVG:" + _heartRateAvg);

                }
                catch (BandException ex)
                {
                    // handle a Band connection exceptio
                }
            }

            //temperature
            countTempTime++;
            if (countTempTime > 400 && _tileGuid != Guid.Empty) // temp notification each 400 seconds
            {
                countTempTime = 0;
                try
                {
                    tbTemperature.Text += "MAX:" + _skinTemperatureMax + " AVG:" + _skinTemperatureAvg + "/";
                    await sendBandNotification("TEMP", "MAX:" + _skinTemperatureMax + " AVG:" + _skinTemperatureAvg);

                }
                catch (BandException ex)
                {
                    // handle a Band connection exceptio
                }
            }

            int iLat = (int)(pos.Coordinate.Latitude * 100000);
            int iLong = (int)(pos.Coordinate.Longitude * 100000);

            double lapLat = ((double)iLat / 100000.0);
            double lapLong = ((double)iLong / 100000.0);


            if ((lapLat > minLapLat && lapLat < maxLapLat) && (lapLong > minLapLong && lapLong < maxLapLong) &&
                (DateTime.Now.Subtract(timeLastLap) > TimeSpan.FromSeconds(30)))
            {
                //LAP!!!!
                _lapCount++;
                DateTime lastLap = timeLastLap;
                timeLastLap = DateTime.Now;

                elapsedTimeLastLap = timeLastLap.Subtract(lastLap);

                string strLapTime = "" + elapsedTimeLastLap;
                strLapTime = strLapTime.Substring(3, strLapTime.Length-7);
                

                tbTimeLap.Text += "" + _lapCount +"-" + strLapTime + "/";

                await sendBandNotification("LAP:" + _lapCount, strLapTime);

            }


        }

        private async Task sendBandNotification(string title, string body)
        {
            try
            {
                _notifications = _notifications + "\n" + DateTime.Now + title + "-" + body;
                await bandClient.NotificationManager.SendMessageAsync(_tileGuid,
                        title, body, DateTimeOffset.Now,
                        MessageFlags.ShowDialog);

            }
            catch (BandException ex)
            {
                // handle a Band connection exceptio
            }
        }

        void _geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            pos = args.Position;
        }

        void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {

            IBandSkinTemperatureReading temperature = e.SensorReading;
            _skinTemperature = temperature.Temperature;

            if (_skinTemperature > _skinTemperatureMax)
                _skinTemperatureMax = (double)(((int)(_skinTemperature *100))/100);
            _skinTemperatureSum += _skinTemperature;
            _skinTemperatureReadCount++;
            _skinTemperatureAvg = (double)(((int)((_skinTemperatureSum / _skinTemperatureReadCount)*100))/100);

        }

        void HeartRate_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandHeartRateReading> e)
        {
            IBandHeartRateReading heartRate = e.SensorReading;
            _heartRate = heartRate.HeartRate;
            if (_heartRate > _heartRateMax)
                _heartRateMax = _heartRate;
            _heartRateSum += _heartRate;
            _heartRateReadCount++;
            _heartRateAvg = _heartRateSum / _heartRateReadCount;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            connected = false;
        }

        private async Task<BandIcon> LoadIcon(string uri)
        {
            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                return bitmap.ToBandIcon();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            lap = pos;
            timeLastLap = DateTime.Now;

            int iLat = (int)(lap.Coordinate.Latitude * 100000);
            int iLong = (int)(lap.Coordinate.Longitude * 100000);

            double iLapLat = ((double)iLat / 100000.0);
            double iLapLong = ((double)iLong / 100000.0);

            tbLapLat.Text = "" + iLapLat;
            tbLapLong.Text = "" + iLapLong;

            minLapLat = iLapLat - 0.00012;
            minLapLong = iLapLong - 0.00012;

            maxLapLat = iLapLat + 0.00010;
            maxLapLong = iLapLong + 0.00010;

        }

        private LiveConnectClient liveClient;
        private LiveLoginResult loginResult;

        private async void Button_SignIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LiveAuthClient auth = new LiveAuthClient();
                loginResult = await auth.LoginAsync(new string[] { "wl.signin", "wl.basic", "wl.skydrive", "wl.skydrive_update" });
                liveClient = new LiveConnectClient(loginResult.Session);
                if (loginResult.Status == LiveConnectSessionStatus.Connected)
                {
                    this.tbLive.Text = "Signed in.";
                }
            }
            catch (LiveAuthException exception)
            {
                this.tbLive.Text = "Error signing in: "
                    + exception.Message;
            }
        }

        private async void Button_UploadFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await liveClient.BackgroundUploadAsync("me/skydrive",
                               fileName, file,
                               OverwriteOption.Overwrite);
                this.tbLive.Text = "Upload completed.";
            }
            catch (Exception ex)
            {
                this.tbLive.Text = ex.Message;
            }
        }

        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {

            SettingsManager sm = new SettingsManager();
            int index = sm.GetValue("index", 5);
            StorageFolder folder = ApplicationData.Current.LocalFolder;

            fileName = "kart" + index + ".gpx";

            file = await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);

        }
    }
}
