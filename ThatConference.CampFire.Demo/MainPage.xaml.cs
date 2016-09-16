using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ThatConference.CampFire.Demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string FaceApiKey = ""; // Go to https://www.microsoft.com/cognitive-services/en-us/sign-up and generate your own key.
        IFaceServiceClient _faceServiceClient = new FaceServiceClient(FaceApiKey);
        IPhotoService _photoService = new PhotoService();
        MediaCapture _mediaManager = new MediaCapture();

        public MainPage()
        {
            InitializeComponent();
            DataContext = FaceViewModel.Empty;
        }

        async void OnLoaded(object sender, RoutedEventArgs e) => await InitializeCameraAsync();

        async Task InitializeCameraAsync()
        {
            try
            {
                _progressIndicator.IsActive = true;
                _preview.Visibility = Visibility.Collapsed;

                var cameraId = await GetNamedCameraOrDefault();
                await _mediaManager.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    VideoDeviceId = cameraId
                });

                _preview.Source = _mediaManager;
                await _mediaManager.StartPreviewAsync();
            }
            finally
            {
                _progressIndicator.IsActive = false;
                _preview.Visibility = Visibility.Visible;
            }
        }

        async Task<string> GetNamedCameraOrDefault(string cameraName = "Microsoft® LifeCam HD-3000")
        {
            var videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return videoDevices.Where(camera =>
                                      camera.IsEnabled &&
                                      camera.Name.Equals(cameraName, StringComparison.OrdinalIgnoreCase))
                               .Select(camera => camera.Id)
                               .FirstOrDefault()
                   ?? videoDevices.Select(camera => camera.Id)
                                  .SingleOrDefault();
        }

        async void SnapButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _snapButton.IsEnabled = false;
                _detectingFaces.IsActive = true;

                var photoFile = await _photoService.CreateAsync();
                var imageProperties = ImageEncodingProperties.CreateBmp();

                await _mediaManager.CapturePhotoToStorageFileAsync(imageProperties, photoFile);

                _sound.Play();
                _emulateCameraFlashAnimation.Begin();

                var faces = 
                    await _faceServiceClient.DetectAsync(await photoFile.OpenStreamForReadAsync(), 
                                                         false, 
                                                         true, 
                                                         new FaceAttributeType[] 
                                                         {
                                                             FaceAttributeType.Gender,
                                                             FaceAttributeType.Age,
                                                             FaceAttributeType.Smile,
                                                             FaceAttributeType.Glasses,
                                                             FaceAttributeType.FacialHair
                                                         });

                if (faces != null)
                {
                    var face = faces.FirstOrDefault();
                    if (face != null)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, 
                                                  () => DataContext = new FaceViewModel(face));
                    }
                }
            }
            finally
            {
                await _photoService.CleanupAsync();
                _snapButton.IsEnabled = true;
                _detectingFaces.IsActive = false;
            }
        }
    }

    public class FaceViewModel
    {
        internal static FaceViewModel Empty => new FaceViewModel(null);

        private readonly Face _face;

        public string Sex => _face?.FaceAttributes.Gender.ToTitleCase() ?? "Unknown";
        public double Age => _face?.FaceAttributes.Age ?? 0;
        public string Glasses => (_face?.FaceAttributes.Glasses ?? Microsoft.ProjectOxford.Face.Contract.Glasses.NoGlasses).ToString().SplitCamelCase();
        public bool IsSmiling => _face?.FaceAttributes.Smile >= .45;
        public string FacialHair => _face?.FaceAttributes.FacialHair.ToDescription();

        public FaceViewModel(Face face)
        {
            _face = face;
        }
    }

    static class FacialHairExtensions
    {
        internal static string ToDescription(this FacialHair facialHair)
        {
            const double threshold = .45;
            var list = new List<string>();

            if (facialHair.Beard >= threshold) list.Add(nameof(facialHair.Beard));
            if (facialHair.Moustache >= threshold) list.Add(nameof(facialHair.Moustache));
            if (facialHair.Sideburns >= threshold) list.Add(nameof(facialHair.Sideburns));

            return list.Any() ? string.Join(", ", list) : "None";
        }
    }
}