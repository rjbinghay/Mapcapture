using Mapcapture.data;
using Microsoft.Maps.MapControl.WPF;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Configuration;
using System.Drawing;
using Color = System.Drawing.Color;
using Mapcapture.extensions;
using System.Windows.Controls;
using System.Timers;
using System.ComponentModel;

namespace Mapcapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 




    public partial class MainWindow : Window
    {

        private  System.Timers.Timer aTimer;
        public LocationCollection TheLocation { get; set; }
        private List<csvdata> CSVData { get; set; }
        public MapPolyline polyline = new MapPolyline();
        public MapPolygon polygon = new MapPolygon();
        public LocationRect bounds = new LocationRect();
        public bool isAutocapture;
        public BackgroundWorker worker;


        private IEnumerable<csvdata> LoadCSV(string filename)
        {
            var reader = new StreamReader(filename);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            {
                csv.Configuration.HasHeaderRecord = false;
                reader.ReadLine(); // removing the header
                var records = csv.GetRecords<csvdata>();

                return records;
            }

        }

        private void myMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           /*
            e.Handled = true;

            Point mousePosition = e.GetPosition(this);
            Location pinLocation = myMap.ViewportPointToLocation(mousePosition);
            
            Pushpin pin = new Pushpin();
            pin.Location = pinLocation;
            MessageBox.Show(mousePosition.ToString());

    */
           
        }

        public MainWindow()
        {

           InitializeComponent();

          //  myBrowser.Navigate("http://gameincol.com/googlemap.html")
          //  myMap.MouseDoubleClick += new MouseButtonEventHandler(myMap_MouseDoubleClick);

            myMap.CredentialsProvider = new ApplicationIdCredentialsProvider(ConfigurationManager.AppSettings.Get("ApplicationIdCredentialsProvider"));

             isAutocapture = bool.Parse(ConfigurationManager.AppSettings.Get("AutoCapture"));
            string filePath = ConfigurationManager.AppSettings.Get("LoadPathCSV") + "capture.csv";
            if (File.Exists(filePath)){
                InitializeMap(filePath);

            }
 
        }

        public void InitializeMap(string filePath)
        {

            TheLocation = new LocationCollection();
            polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightSkyBlue);
            polyline.StrokeThickness = 3;
            polyline.Opacity = 1;

            CSVData = LoadCSV(filePath).ToList();

            foreach (var x in CSVData)
            {
                TheLocation.Add(
                  new Location()
                  {
                      
                      Latitude = x.Lat,
                      Longitude = x.Long,
                      Altitude = 0

                  });

            }

            polyline.Locations = TheLocation;
           
          

            myMap.Children.Add(polyline);
           
            bounds = new LocationRect(TheLocation);


            double[] arrlatx1 = CSVData.Select(x=>x.Lat).ToArray();
            double[] arrlongx1 = CSVData.Select(x => x.Long).ToArray();

            double[] arrlaty2 = CSVData.Select(x => x.Lat).ToArray();
            double[] arrlongy2 = CSVData.Select(x => x.Long).ToArray();


            myMap.Loaded += (s, e) => {


                myMap.SetView(bounds);
                resizewindow();

                if (isAutocapture)
                {
                    
                  
                    SetTimer();
                }

               

            };

            
            
            

        }

        private void resizewindow()
        {


            //MessageBox.Show(myMap.ZoomLevel.ToString());
            if (myMap.ZoomLevel > 19.57) {

              //  MessageBox.Show("hit");
                myMap.ZoomLevel = 19.57;
                    
            }

            double[] arrlatx1 = CSVData.Select(x => x.Lat).ToArray();
            double[] arrlongx1 = CSVData.Select(x => x.Long).ToArray();

            double[] arrlaty2 = CSVData.Select(x => x.Lat).ToArray();
            double[] arrlongy2 = CSVData.Select(x => x.Long).ToArray();

            var pinpintx1 = myMap.LocationToViewportPoint(new Location(arrlatx1[0], arrlongx1[0]));
            var pinpintx2 = myMap.LocationToViewportPoint(new Location(arrlatx1[2], arrlongx1[2]));


            var pinpinty1 = myMap.LocationToViewportPoint(new Location(arrlaty2[1], arrlongy2[1]));
            var pinpinty2 = myMap.LocationToViewportPoint(new Location(arrlaty2[3], arrlongy2[3]));



            var mapwidth = (pinpintx2.X - pinpintx1.X);
            var mapheight = (pinpinty1.Y - pinpinty2.Y) + 50;

            this.Width = mapwidth + 16;
            this.Height = mapheight + 28;
        }
    
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            SetTimer();
        }

        public  void TakeAScreenShot() {

          

            polyline.Opacity = 0.0;
            
            string filePath;



            RenderTargetBitmap renderTargetBitmap =
            new RenderTargetBitmap(Convert.ToInt32(myMap.ActualWidth), Convert.ToInt32(myMap.ActualHeight), 96, 96, PixelFormats.Pbgra32);

            renderTargetBitmap.Render(myMap);
            PngBitmapEncoder pngImage = new PngBitmapEncoder();
            pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));


            if ((bool)chkenablersharpen.IsChecked)
            {

                filePath = Path.GetTempFileName();
            }
            else
            {
                filePath = ConfigurationManager.AppSettings.Get("SaveImagePath") + ConfigurationManager.AppSettings.Get("SaveImageName") + "." + ConfigurationManager.AppSettings.Get("SaveImageExtention");
            }

            using (Stream fileStream = File.Create(filePath))
            {
                pngImage.Save(fileStream);
                fileStream.Close();
                //File.Delete(filePath);
            }



            if ((bool)chkenablersharpen.IsChecked)
            {
                Bitmap bm = new Bitmap(filePath);
                // bm = xsharpen(bm, (int)sharpenslider.Value);
                ApplySharpen(ref bm, 9);
                //ApplySharpen(ref bm, 7);

                bm.Save(ConfigurationManager.AppSettings.Get("SaveImagePath") + ConfigurationManager.AppSettings.Get("SaveImageName") + "." + ConfigurationManager.AppSettings.Get("SaveImageExtention"));


            }

            this.Close();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            long sum = 0;
            long total = 3000;
            for (long i = 1; i <= total; i++)
            {
                sum += i;
                int percentage = Convert.ToInt32(((double)i / total) * 100);

                Dispatcher.Invoke(new System.Action(() =>
                {
                    worker.ReportProgress(percentage);
                    

                }));
            }
            Dispatcher.Invoke(new System.Action(() =>
            {
                TakeAScreenShot();

            }));
           
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MyProgressBar.Visibility = Visibility.Collapsed;
           
         
        }
        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            MyProgressBar.Value = e.ProgressPercentage;
        }


        public static Bitmap xsharpen(Bitmap image, int weight)
        {

           
            
            Bitmap sharpenImage = new Bitmap(image.Width, image.Height);

            int filterWidth = weight; // original 3
            int filterHeight = weight; // original 3
            int w = image.Width;
            int h = image.Height;

            double[,] filter = new double[filterWidth, filterHeight];

            filter[0, 0] = filter[0, 1] = filter[0, 2] = filter[1, 0] = filter[1, 2] = filter[2, 0] = filter[2, 1] = filter[2, 2] = -1;
            filter[1, 1] = 9; // original 9

            double factor = 1.0;
            double bias = 0.0;

            Color[,] result = new Color[image.Width, image.Height];

            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
                {
                    double red = 0.0, green = 0.0, blue = 0.0;



                    for (int filterX = 0; filterX < filterWidth; filterX++)
                    {
                        for (int filterY = 0; filterY < filterHeight; filterY++)
                        {
                            int imageX = (x - filterWidth / 2 + filterX + w) % w;
                            int imageY = (y - filterHeight / 2 + filterY + h) % h;

                            //=====[INSERT LINES]========================================================
                            // Get the color here - once per fiter entry and image pixel.
                            Color imageColor = image.GetPixel(imageX, imageY);
                            //===========================================================================

                            red += imageColor.R * filter[filterX, filterY];
                            green += imageColor.G * filter[filterX, filterY];
                            blue += imageColor.B * filter[filterX, filterY];
                        }
                        int r = Math.Min(Math.Max((int)(factor * red + bias), 0), 255);
                        int g = Math.Min(Math.Max((int)(factor * green + bias), 0), 255);
                        int b = Math.Min(Math.Max((int)(factor * blue + bias), 0), 255);

                        result[x, y] = Color.FromArgb(r, g, b);
                    }
                }
            }
           ;
           for (int i = 0; i < w; ++i)
            {
                for (int j = 0; j < h; ++j)
                {
                    sharpenImage.SetPixel(i, j, result[i, j]);
                }
                
            }
            return sharpenImage;
        }


        private void Button_Center(object sender, RoutedEventArgs e)
        {

            // myMap.Mode = new AerialMode();
            resizewindow();
            myMap.SetView(bounds);
           // MessageBox.Show(myMap.ZoomLevel.ToString());

        }

        public static void ApplySharpen(ref Bitmap bmp, int weight)
        {
            ConvolutionMatrix m = new ConvolutionMatrix();
            m.Apply(0); // original 0
            m.Pixel = weight;
            m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = -2;
            m.Factor = weight - 8;

            Convolution C = new Convolution();
            C.Matrix = m;
            C.Convolution3x3(ref bmp);
        }

        private void Button_zoomout(object sender, RoutedEventArgs e)
        {
            var current_zoom = myMap.ZoomLevel;
            var deduction = current_zoom * 0.0005;
            current_zoom = current_zoom - deduction;
            myMap.ZoomLevel = current_zoom;
            //MessageBox.Show(myMap.ZoomLevel.ToString());
        }

        private void Button_zoomin(object sender, RoutedEventArgs e)
        {
            var current_zoom = myMap.ZoomLevel;
            var deduction = current_zoom * 0.0005;
            current_zoom = current_zoom + deduction;
            myMap.ZoomLevel = current_zoom;
            //MessageBox.Show(myMap.ZoomLevel.ToString());
        }

        private void chkenablersharpen_Checked(object sender, RoutedEventArgs e)
        {
            //sharpenslider.IsEnabled = true;
        }

        private void chkenablersharpen_Unchecked(object sender, RoutedEventArgs e)
        {
           // sharpenslider.IsEnabled = false;
        }

        private void SetTimer()
        {
            btncapture.Visibility = Visibility.Collapsed;
            MyProgressBar.Visibility = Visibility.Visible;
            // create a timer with interval
            aTimer = new System.Timers.Timer(double.Parse(ConfigurationManager.AppSettings.Get("CaptureLoadTimer")));
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
           // aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        public void OnTimedEvent(Object source, ElapsedEventArgs e)
        {

            this.Dispatcher.Invoke(() =>
            {

                aTimer.Stop(); // doing all house keeping
                aTimer.Dispose();
                // TakeAScreenShot();

                // purpose of this code is to give impression that it's loading and finishing - for visual queue purpose.
                worker = new BackgroundWorker(); //Initializing the worker object
                worker.ProgressChanged += Worker_ProgressChanged; //Binding Worker_ProgressChanged method
                worker.DoWork += Worker_DoWork; //Binding Worker_DoWork method
                worker.WorkerReportsProgress = true; //telling the worker that it supports reporting progress
                worker.RunWorkerCompleted += Worker_RunWorkerCompleted; //Binding worker_RunWorkerCompleted method
                worker.RunWorkerAsync(); //Executing the worker


            });
        
           

        }


 
      
    }

   

   
}

