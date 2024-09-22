using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ImageProcessingByOpenCV
{
    public partial class MainWindow : System.Windows.Window
    {
        private Mat _originalImage;
        private Mat _processedImage;
        private bool _isProcessing = false;
        private string _currentFolder;

        public enum ProcessingMode
        {
            None,
            Grayscale,
            GaussianBlur,
            EdgeDetection,
            ColorDetection
        }

        private ProcessingMode _currentMode = ProcessingMode.None;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "폴더 선택",
                Filter = "Folders|no_files",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (dialog.ShowDialog() == true)
            {
                _currentFolder = Path.GetDirectoryName(dialog.FileName);
                LoadImagesFromFolder(_currentFolder);
            }
        }

        private void LoadImagesFromFolder(string folderPath)
        {
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            var imageFiles = Directory.GetFiles(folderPath)
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .Select(Path.GetFileName)
                .ToList();

            ImageListBox.ItemsSource = imageFiles;
        }

        private async void ImageListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ImageListBox.SelectedItem is string selectedFileName)
            {
                string fullPath = Path.Combine(_currentFolder, selectedFileName);
                await LoadImageAsync(fullPath);
                await ApplySelectedFilterAsync();
            }
        }

        private async Task LoadImageAsync(string fileName)
        {
            await Task.Run(() =>
            {
                DisposeImages();
                _originalImage = Cv2.ImRead(fileName);
                _processedImage = _originalImage.Clone();
            });

            await UpdateDisplayImageAsync();
        }

        private async void ProcessingMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null || _isProcessing) return;

            if (sender is System.Windows.Controls.RadioButton radioButton)
            {
                _currentMode = (ProcessingMode)Enum.Parse(typeof(ProcessingMode), radioButton.Tag.ToString());
                await ApplySelectedFilterAsync();
            }
        }

        private async void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_originalImage == null || _isProcessing) return;
            await ApplySelectedFilterAsync();
        }

        private async void ColorDetectionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_originalImage == null || _isProcessing) return;
            await ApplySelectedFilterAsync();
        }

        private async Task ApplySelectedFilterAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                await Task.Run(() =>
                {
                    _processedImage = _originalImage.Clone();
                    switch (_currentMode)
                    {
                        case ProcessingMode.Grayscale:
                            ApplyGrayscale();
                            break;
                        case ProcessingMode.GaussianBlur:
                            ApplyGaussianBlur();
                            break;
                        case ProcessingMode.EdgeDetection:
                            ApplyEdgeDetection();
                            break;
                        case ProcessingMode.ColorDetection:
                            ApplyColorDetection();
                            break;
                        case ProcessingMode.None:
                        default:
                            // Do nothing, keep original image
                            break;
                    }
                });

                await UpdateDisplayImageAsync();
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show($"필터 적용 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void ApplyGrayscale()
        {
            double grayIntensity = 0;
            Dispatcher.Invoke(() => grayIntensity = GrayIntensitySlider.Value);

            using var gray = new Mat();
            Cv2.CvtColor(_originalImage, gray, ColorConversionCodes.BGR2GRAY);
            using var colorMat = new Mat();
            Cv2.CvtColor(gray, colorMat, ColorConversionCodes.GRAY2BGR);
            Cv2.AddWeighted(_originalImage, 1 - grayIntensity, colorMat, grayIntensity, 0, _processedImage);
        }

        private void ApplyGaussianBlur()
        {
            int blurIntensity = 0;
            Dispatcher.Invoke(() => blurIntensity = (int)BlurIntensitySlider.Value);

            int kernelSize = blurIntensity * 2 + 1;
            Cv2.GaussianBlur(_processedImage, _processedImage, new OpenCvSharp.Size(kernelSize, kernelSize), 0);
        }

        private void ApplyEdgeDetection()
        {
            int threshold = 0;
            Dispatcher.Invoke(() => threshold = (int)EdgeThresholdSlider.Value);

            using var edges = new Mat();
            Cv2.Canny(_processedImage, edges, threshold, threshold * 2);
            Cv2.CvtColor(edges, _processedImage, ColorConversionCodes.GRAY2BGR);
        }

        private void ApplyColorDetection()
        {
            string selectedColor = "";
            Dispatcher.Invoke(() => selectedColor = (ColorDetectionComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString());

            if (string.IsNullOrEmpty(selectedColor) || selectedColor == "없음")
            {
                return;
            }

            using var hsvImage = new Mat();
            Cv2.CvtColor(_originalImage, hsvImage, ColorConversionCodes.BGR2HSV);

            switch (selectedColor)
            {
                case "빨강":
                    {
                        var lowerRed1 = new Scalar(0, 70, 50);
                        var upperRed1 = new Scalar(10, 255, 255);
                        var lowerRed2 = new Scalar(170, 70, 50);
                        var upperRed2 = new Scalar(180, 255, 255);

                        using var mask1 = ApplyMask(_originalImage, hsvImage, lowerRed1, upperRed1, "red1");
                        using var mask2 = ApplyMask(_originalImage, hsvImage, lowerRed2, upperRed2, "red2");
                        _processedImage = new Mat();
                        Cv2.BitwiseOr(mask1, mask2, _processedImage);
                        break;
                    }
                case "초록":
                    {
                        var lowerGreen = new Scalar(35, 70, 50);
                        var upperGreen = new Scalar(85, 255, 255);
                        _processedImage = ApplyMask(_originalImage, hsvImage, lowerGreen, upperGreen, "green");
                        break;
                    }
                case "파랑":
                    {
                        var lowerBlue = new Scalar(90, 70, 50);
                        var upperBlue = new Scalar(130, 255, 255);
                        _processedImage = ApplyMask(_originalImage, hsvImage, lowerBlue, upperBlue, "blue");
                        break;
                    }
            }
        }

        private Mat ApplyMask(Mat sourceImage, Mat hsvImage, Scalar lowerBound, Scalar upperBound, string colorName)
        {
            using var mask = new Mat();
            Cv2.InRange(hsvImage, lowerBound, upperBound, mask);

            var resultImage = new Mat();
            sourceImage.CopyTo(resultImage, mask);

            return resultImage;
        }

        private async Task UpdateDisplayImageAsync()
        {
            if (_originalImage == null || _processedImage == null) return;

            BitmapSource originalBitmap = null;
            BitmapSource processedBitmap = null;
            await Task.Run(() =>
            {
                originalBitmap = ConvertMatToBitmapSource(_originalImage);
                processedBitmap = ConvertMatToBitmapSource(_processedImage);
            });

            await Dispatcher.InvokeAsync(() =>
            {
                OriginalImage.Source = originalBitmap;
                ProcessedImage.Source = processedBitmap;
            });
        }

        private BitmapSource ConvertMatToBitmapSource(Mat image)
        {
            using var stream = image.ToMemoryStream(".png");
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private void DisposeImages()
        {
            _originalImage?.Dispose();
            _processedImage?.Dispose();
        }

        protected override void OnClosed(EventArgs e)
        {
            DisposeImages();
            base.OnClosed(e);
        }
    }
}