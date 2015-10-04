using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using System.ComponentModel;
using Windows.Storage.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using KinectFace;
using Microsoft.Kinect.Face;

using System.Net.Http;
using System.Collections.Specialized;

namespace Kinect2Sample
{
    public enum DisplayFrameType
    {
        FaceOnColor,
        FaceOnInfrared
    }

    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private Stopwatch eyesClosedCtr = new Stopwatch(); // how long eyes have been closed
        private int sleepThreshold = 3 * 1000; //amount of time til eyes clsoed is considered sleeping

        private Stopwatch sleepCtr = new Stopwatch(); //how long user is asleep
        private int napThreshold = 18 * 1000; // how long a sleep is allowed to last

        private Stopwatch restCtr = new Stopwatch(); //how long a rest/walk has been
        private int restThreshold = 10 * 1000; //how long a rest/walk is allowed to endure before a pebble notification

        private Stopwatch workCtr = new Stopwatch(); //how long a user has been working
        private int workThreshold = 15 * 1000; //how long a work session goes until rest is called for

        private Stopwatch sendSmooth = new Stopwatch();

        //private const string url = "http://localhost:3000/";
        private const string url = "http://10.10.10.194:3000/";
      //  private const string url = "http://192.168.1.14:1402/";
        private HttpClient wb = new HttpClient();

        private string serverStatus = "searching";

        private string userstate = "working";
        private string laststate = "";

        private int eyesOpenCounter = 0;
        private int eyesClosedCounter = 0;
        private string currentEyes;

        private const DisplayFrameType DEFAULT_DISPLAYFRAMETYPE = DisplayFrameType.FaceOnColor;

        /// <summary>
        /// The highest value that can be returned in the InfraredFrame.
        /// It is cast to a float for readability in the visualization code.
        /// </summary>
        private const float InfraredSourceValueMaximum = (float)ushort.MaxValue;

        /// <summary>
        /// Used to set the lower limit, post processing, of the
        /// infrared data that we will render.
        /// Increasing or decreasing this value sets a brightness 
        /// "wall" either closer or further away.
        /// </summary>
        private const float InfraredOutputValueMinimum = 0.01f;

        /// <summary>
        /// The upper limit, post processing, of the
        /// infrared data that will render.
        /// </summary>
        private const float InfraredOutputValueMaximum = 1.0f;

        /// <summary>
        /// The InfraredSceneValueAverage value specifies the average infrared 
        /// value of the scene. This value was selected by analyzing the average 
        /// pixel intensity for a given scene. 
        /// This could be calculated at runtime to handle different IR conditions
        /// of a scene (outside vs inside).
        /// </summary>
        private const float InfraredSceneValueAverage = 0.08f;

        /// <summary>
        /// The InfraredSceneStandardDeviations value specifies the number of 
        /// standard deviations to apply to InfraredSceneValueAverage. 
        /// This value was selected by analyzing data from a given scene.
        /// This could be calculated at runtime to handle different IR conditions
        /// of a scene (outside vs inside).
        /// </summary>
        private const float InfraredSceneStandardDeviations = 3.0f;

        // Size of the RGB pixel in the bitmap
        private const int BytesPerPixel = 4;

        private KinectSensor kinectSensor = null;
        private string statusText = null;
        private WriteableBitmap bitmap = null;
        private FrameDescription currentFrameDescription;
        private DisplayFrameType currentDisplayFrameType;
        private MultiSourceFrameReader multiSourceFrameReader = null;
        private CoordinateMapper coordinateMapper = null;
        private BodiesManager bodiesManager = null;

        //Infrared Frame 
        private ushort[] infraredFrameData = null;
        private byte[] infraredPixels = null;

        //FaceManager library
        private FaceManager faceManager;
        private FaceFrameFeatures faceFrameFeatures;

        //Cat assets
        private Image[] catEyeRightOpen, catEyeRightClosed, catEyeLeftOpen, catEyeLeftClosed, catNose;

        public event PropertyChangedEventHandler PropertyChanged;
        public string StatusText
        {
            get { return this.statusText; }
            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public FrameDescription CurrentFrameDescription
        {
            get { return this.currentFrameDescription; }
            set
            {
                if (this.currentFrameDescription != value)
                {
                    this.currentFrameDescription = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentFrameDescription"));
                    }
                }
            }
        }

        public DisplayFrameType CurrentDisplayFrameType
        {
            get { return this.currentDisplayFrameType; }
            set
            {
                if (this.currentDisplayFrameType != value)
                {
                    this.currentDisplayFrameType = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDisplayFrameType"));
                    }
                }
            }
        }

        public MainPage()
        {
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            this.multiSourceFrameReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Infrared | FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            // specify the required face frame results
            // init with all the features so they are accessible later.
            this.faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.BoundingBoxInInfraredSpace
                | FaceFrameFeatures.PointsInInfraredSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;

            this.faceManager = new FaceManager(this.kinectSensor, this.faceFrameFeatures);

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // open the sensor
            this.kinectSensor.Open();

            this.InitializeComponent();

            // new
            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DEFAULT_DISPLAYFRAMETYPE);

            SetupCatAssets();
        }

        private void SetupCatAssets()
        {
            ScaleTransform flipTransform = new ScaleTransform() { ScaleX = -1.0 };
            int bodyCount = kinectSensor.BodyFrameSource.BodyCount;
            catEyeRightOpen = new Image[bodyCount];
            catEyeRightClosed = new Image[bodyCount];
            catEyeLeftOpen = new Image[bodyCount];
            catEyeLeftClosed = new Image[bodyCount];
            catNose = new Image[bodyCount];

            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; i++)
            {
                catEyeRightOpen[i] = new Image()
                {
                    Source = new BitmapImage(new Uri(this.BaseUri, "Assets/CatEye_left_open.png")),
                    Width = 50,
                    Height = 40,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = flipTransform
                };
                catEyeRightClosed[i] = new Image()
                {
                    Source = new BitmapImage(new Uri(this.BaseUri, "Assets/CatEye_left_closed.png")),
                    Width = 30,
                    Height = 20,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = flipTransform
                };
                catEyeLeftOpen[i] = new Image()
                {
                    Source = new BitmapImage(new Uri(this.BaseUri, "Assets/CatEye_left_open.png")),
                    Width = 50,
                    Height = 40
                };
                catEyeLeftClosed[i] = new Image()
                {
                    Source = new BitmapImage(new Uri(this.BaseUri, "Assets/CatEye_left_closed.png")),
                    Width = 30,
                    Height = 20
                };
                catEyeLeftClosed[i].RenderTransformOrigin = new Point(0.5, 0.5);
                catNose[i] = new Image()
                {
                    Source = new BitmapImage(new Uri(this.BaseUri, "Assets/CatNose.png")),
                    Width = 40,
                    Height = 25
                };
            }
        }

        private void SetupCurrentDisplay(DisplayFrameType newDisplayFrameType)
        {
            CurrentDisplayFrameType = newDisplayFrameType;
            // Frames used by more than one type are declared outside the switch
            FrameDescription colorFrameDescription = null;
            FrameDescription infraredFrameDescription = null;
            // reset the display methods
            FacePointsCanvas.Children.Clear();
            if (this.BodyJointsGrid != null)
            {
                this.BodyJointsGrid.Visibility = Visibility.Collapsed;
            }
            if (this.FrameDisplayImage != null)
            {
                this.FrameDisplayImage.Source = null;
            }
            switch (CurrentDisplayFrameType)
            {
                case DisplayFrameType.FaceOnColor:
                    colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
                    this.CurrentFrameDescription = colorFrameDescription;
                    // create the bitmap to display
                    this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);
                    this.FacePointsCanvas.Width = colorFrameDescription.Width;
                    this.FacePointsCanvas.Height = colorFrameDescription.Height;
                    this.faceFrameFeatures =
                            FaceFrameFeatures.BoundingBoxInColorSpace
                            | FaceFrameFeatures.PointsInColorSpace
                            | FaceFrameFeatures.RotationOrientation
                            | FaceFrameFeatures.FaceEngagement
                            | FaceFrameFeatures.Glasses
                            | FaceFrameFeatures.Happy
                            | FaceFrameFeatures.LeftEyeClosed
                            | FaceFrameFeatures.RightEyeClosed
                            | FaceFrameFeatures.LookingAway
                            | FaceFrameFeatures.MouthMoved
                            | FaceFrameFeatures.MouthOpen;
                    break;

                case DisplayFrameType.FaceOnInfrared:
                    infraredFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;
                    this.CurrentFrameDescription = infraredFrameDescription;
                    // allocate space to put the pixels being received and converted
                    this.infraredFrameData = new ushort[infraredFrameDescription.Width * infraredFrameDescription.Height];
                    this.infraredPixels = new byte[infraredFrameDescription.Width * infraredFrameDescription.Height * BytesPerPixel];
                    this.bitmap = new WriteableBitmap(infraredFrameDescription.Width, infraredFrameDescription.Height);
                    this.FacePointsCanvas.Width = infraredFrameDescription.Width;
                    this.FacePointsCanvas.Height = infraredFrameDescription.Height;
                    break;

                default:
                    break;
            }
        }

        private void Sensor_IsAvailableChanged(KinectSensor sender, IsAvailableChangedEventArgs args)
        {
            this.StatusText = this.kinectSensor.IsAvailable ? "Running" : "Not Available";
        }

        private void Reader_MultiSourceFrameArrived(MultiSourceFrameReader sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }
            ColorFrame colorFrame = null;
            InfraredFrame infraredFrame = null;
            switch (CurrentDisplayFrameType)
            {
                case DisplayFrameType.FaceOnColor:
                    using (colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        ShowColorFrame(colorFrame);
                        this.faceManager.DrawLatestFaceResults(this.FacePointsCanvas, this.faceFrameFeatures);
                    }
                    break;
                case DisplayFrameType.FaceOnInfrared:
                    using (infraredFrame = multiSourceFrame.InfraredFrameReference.AcquireFrame())
                    {
                        ShowInfraredFrame(infraredFrame);
                        DrawFaceOnInfrared();
                    }
                    break;
                default:
                    break;
            }
        }

        private void DrawFaceOnInfrared()
        {
            laststate = userstate;
            FacePointsCanvas.Children.Clear();
            FaceFrameResult[] results = faceManager.GetLatestFaceFrameResults();
            bool onRest = true;
            for (int i = 0; i < results.Count(); i++ )
            {
                if (results[i] != null)
                {
                    onRest = false;
                    Point rightEyePoint = results[i].FacePointsInInfraredSpace[FacePointType.EyeRight];
                    Point leftEyePoint = results[i].FacePointsInInfraredSpace[FacePointType.EyeLeft];
                    Point nosePoint = results[i].FacePointsInInfraredSpace[FacePointType.Nose];
                    bool rightEyeIsClosed = results[i].FaceProperties[FaceProperty.RightEyeClosed] != DetectionResult.No;
                    bool leftEyeIsClosed = results[i].FaceProperties[FaceProperty.LeftEyeClosed] != DetectionResult.No;

                    #region Draw on Eyes
                    if (leftEyeIsClosed)
                    {
                        Canvas.SetLeft(catEyeLeftClosed[i], leftEyePoint.X - (catEyeLeftClosed[i].Width / 2));
                        Canvas.SetTop(catEyeLeftClosed[i], leftEyePoint.Y - (catEyeLeftClosed[i].Height / 2));
                        this.FacePointsCanvas.Children.Add(catEyeLeftClosed[i]);
                    }
                    else
                    {
                        Canvas.SetLeft(catEyeLeftOpen[i], leftEyePoint.X - (catEyeLeftOpen[i].Width / 2));
                        Canvas.SetTop(catEyeLeftOpen[i], leftEyePoint.Y - (catEyeLeftOpen[i].Height / 2));
                        this.FacePointsCanvas.Children.Add(catEyeLeftOpen[i]);
                    }

                    if (rightEyeIsClosed)
                    {
                        Canvas.SetLeft(catEyeRightClosed[i], rightEyePoint.X - (catEyeRightClosed[i].Width / 2));
                        Canvas.SetTop(catEyeRightClosed[i], rightEyePoint.Y - (catEyeRightClosed[i].Height / 2));
                        this.FacePointsCanvas.Children.Add(catEyeRightClosed[i]);
                    }
                    else
                    {
                        Canvas.SetLeft(catEyeRightOpen[i], rightEyePoint.X - (catEyeRightOpen[i].Width / 2));
                        Canvas.SetTop(catEyeRightOpen[i], rightEyePoint.Y - (catEyeRightOpen[i].Height / 2));
                        this.FacePointsCanvas.Children.Add(catEyeRightOpen[i]);
                    }

                    Canvas.SetLeft(catNose[i], nosePoint.X - (catNose[i].Width / 2));
                    Canvas.SetTop(catNose[i], nosePoint.Y);
                    this.FacePointsCanvas.Children.Add(catNose[i]);

                    #endregion

                    if (leftEyeIsClosed && rightEyeIsClosed)
                    {
                        eyesOpenCounter = 0;
                        eyesClosedCounter++;
                        if (eyesClosedCounter == 10)
                        {
                            currentEyes = "closed";
                            eyesClosedCounter = 0;
                        }
                        
                    }
                    else
                    {
                        eyesClosedCounter = 0;
                        eyesOpenCounter++;
                        if (eyesOpenCounter == 10)
                        {
                            currentEyes = "open";
                            eyesOpenCounter = 0;
                        }
                    }



                    if (currentEyes == "closed")
                    {
                        if (!eyesClosedCtr.IsRunning)
                        {
                            eyesClosedCtr.Start();
                        }
                        else
                        {
                            if (eyesClosedCtr.ElapsedMilliseconds >= sleepThreshold)
                            {
                                eyesClosedCtr.Reset();
                                workCtr.Reset();
                                sleepCtr.Start();
                                userstate = "sleep";
                            }
                            if (sleepCtr.ElapsedMilliseconds >= napThreshold)
                            {
                                userstate = "wakeup";
                            }
                        }
                    }
                    else
                    {
                        userstate = "working";
                        if (eyesClosedCtr.IsRunning)
                        {
                            eyesClosedCtr.Reset();
                        }
                        if (sleepCtr.IsRunning)
                        {
                            sleepCtr.Reset();
                        }
                        if (restCtr.IsRunning)
                        {
                            restCtr.Reset();
                        }
                        if (!workCtr.IsRunning)
                        {
                            workCtr.Start();
                        }
                        if (workCtr.ElapsedMilliseconds > workThreshold)
                        {
                            userstate = "takebreak";
                        }
                    } 

                }
            }
            if (onRest)
            {
                if (workCtr.IsRunning)
                {
                    workCtr.Reset();
                }
                if (!restCtr.IsRunning)
                {
                    restCtr.Start();
                }
                if (restCtr.ElapsedMilliseconds > restThreshold)
                {
                    userstate = "backtowork";
                }
            }
            SendData();
        }

        private void SendData()
        {
            //if (!sendSmooth.IsRunning)
            //{
            //    sendSmooth.Start();
            //}
            //if (sendSmooth.ElapsedMilliseconds >= 5000) 
            //{
            if (laststate != userstate)
            {
                HttpContent content = new FormUrlEncodedContent(new[]
                {
                        new KeyValuePair<string, string>("state", userstate),
                });

                wb.PostAsync(url, content).ContinueWith(
                (postTask) =>
                {
                    serverStatus = postTask.Result.EnsureSuccessStatusCode().ToString();
                });
            }
           // }
        }

        private void ShowColorFrame(ColorFrame colorFrame)
        {
            bool colorFrameProcessed = false;

            if (colorFrame != null)
            {
                FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                // verify data and write the new color frame data to the Writeable bitmap
                if ((colorFrameDescription.Width == this.bitmap.PixelWidth) && (colorFrameDescription.Height == this.bitmap.PixelHeight))
                {
                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        colorFrame.CopyRawFrameDataToBuffer(this.bitmap.PixelBuffer);
                    }
                    else
                    {
                        colorFrame.CopyConvertedFrameDataToBuffer(this.bitmap.PixelBuffer, ColorImageFormat.Bgra);
                    }

                    colorFrameProcessed = true;
                }
            }

            if (colorFrameProcessed)
            {
                this.bitmap.Invalidate();
                FrameDisplayImage.Source = this.bitmap;
            }
        }

        private void ShowInfraredFrame(InfraredFrame infraredFrame)
        {
            bool infraredFrameProcessed = false;

            if (infraredFrame != null)
            {
                FrameDescription infraredFrameDescription = infraredFrame.FrameDescription;

                // verify data and write the new infrared frame data to the display bitmap
                if (((infraredFrameDescription.Width * infraredFrameDescription.Height)
                    == this.infraredFrameData.Length) &&
                    (infraredFrameDescription.Width == this.bitmap.PixelWidth) &&
                    (infraredFrameDescription.Height == this.bitmap.PixelHeight))
                {
                    // Copy the pixel data from the image to a temporary array
                    infraredFrame.CopyFrameDataToArray(this.infraredFrameData);

                    infraredFrameProcessed = true;
                }
            }

            // we got a frame, convert and render
            if (infraredFrameProcessed)
            {
                this.ConvertInfraredDataToPixels();
                this.RenderPixelArray(this.infraredPixels);
            }
        }

        private void ConvertInfraredDataToPixels()
        {
            // Convert the infrared to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < this.infraredFrameData.Length; ++i)
            {
                // normalize the incoming infrared data (ushort) to a float ranging from 
                // [InfraredOutputValueMinimum, InfraredOutputValueMaximum] by
                // 1. dividing the incoming value by the source maximum value
                float intensityRatio = (float)this.infraredFrameData[i] / InfraredSourceValueMaximum;

                // 2. dividing by the (average scene value * standard deviations)
                intensityRatio /= InfraredSceneValueAverage * InfraredSceneStandardDeviations;

                // 3. limiting the value to InfraredOutputValueMaximum
                intensityRatio = Math.Min(InfraredOutputValueMaximum, intensityRatio);

                // 4. limiting the lower value InfraredOutputValueMinimum
                intensityRatio = Math.Max(InfraredOutputValueMinimum, intensityRatio);

                // 5. converting the normalized value to a byte and using the result
                // as the RGB components required by the image
                byte intensity = (byte)(intensityRatio * 255.0f);
                this.infraredPixels[colorPixelIndex++] = intensity; //Blue
                this.infraredPixels[colorPixelIndex++] = intensity; //Green
                this.infraredPixels[colorPixelIndex++] = intensity; //Red
                this.infraredPixels[colorPixelIndex++] = 255;       //Alpha
            }
        }

        private void RenderPixelArray(byte[] pixels)
        {
            pixels.CopyTo(this.bitmap.PixelBuffer);
            this.bitmap.Invalidate();
            this.FrameDisplayImage.Source = this.bitmap;
        }

        private void ColorFaceButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.FaceOnColor);
        }

        private void InfraredFaceButton_Click(object sender, RoutedEventArgs e)
        {
            SetupCurrentDisplay(DisplayFrameType.FaceOnInfrared);
        }

        [Guid("905a0fef-bc53-11df-8c49-001e4fc686da"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IBufferByteAccess
        {
            unsafe void Buffer(out byte* pByte);
        }
    }
}
