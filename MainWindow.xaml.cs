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

        /// <summary>
        /// 사용자가 이미지 파일이 들어있는 폴더를 선택할 수 있게 하는 메서드입니다.
        /// 선택된 폴더 내의 이미지 파일들을 로드하여 리스트에 표시합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">이벤트 관련 매개변수</param>
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

        /// <summary>
        /// 지정된 폴더에서 지원되는 이미지 파일들을 찾아 리스트에 로드하는 메서드입니다.
        /// </summary>
        /// <param name="folderPath">이미지 파일을 검색할 폴더의 경로 (string)</param>
        private void LoadImagesFromFolder(string folderPath)
        {
            string[] supportedExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            var imageFiles = Directory.GetFiles(folderPath)
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .Select(Path.GetFileName)
                .ToList();

            ImageListBox.ItemsSource = imageFiles;
        }

        /// <summary>
        /// 이미지 리스트에서 선택된 이미지를 로드하고 현재 선택된 필터를 적용하는 메서드입니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">마우스 이벤트 관련 매개변수</param>
        private async void ImageListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ImageListBox.SelectedItem is string selectedFileName)
            {
                string fullPath = Path.Combine(_currentFolder, selectedFileName);
                await LoadImageAsync(fullPath);
                await ApplySelectedFilterAsync();
            }
        }

        /// <summary>
        /// 지정된 파일 경로에서 이미지를 비동기적으로 로드하는 메서드입니다.
        /// </summary>
        /// <param name="fileName">로드할 이미지 파일의 전체 경로 (string)</param>
        /// <returns>비동기 작업을 나타내는 Task</returns>
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

        /// <summary>
        /// 이미지 처리 모드가 변경될 때 호출되는 메서드입니다.
        /// 새로 선택된 필터를 적용합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">이벤트 관련 매개변수</param>
        private async void ProcessingMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_originalImage == null || _isProcessing) return;

            if (sender is System.Windows.Controls.RadioButton radioButton)
            {
                _currentMode = (ProcessingMode)Enum.Parse(typeof(ProcessingMode), radioButton.Tag.ToString());
                await ApplySelectedFilterAsync();
            }
        }

        /// <summary>
        /// 필터 강도 슬라이더의 값이 변경될 때 호출되는 메서드입니다.
        /// 변경된 값으로 현재 선택된 필터를 다시 적용합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">값 변경 이벤트 관련 매개변수</param>
        private async void SliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_originalImage == null || _isProcessing) return;
            await ApplySelectedFilterAsync();
        }

        /// <summary>
        /// 색상 검출을 위한 컴보박스의 선택이 변경될 때 호출되는 메서드입니다.
        /// 선택된 색상에 대한 검출 필터를 적용합니다.
        /// </summary>
        /// <param name="sender">이벤트를 발생시킨 객체</param>
        /// <param name="e">선택 변경 이벤트 관련 매개변수</param>
        private async void ColorDetectionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_originalImage == null || _isProcessing) return;
            await ApplySelectedFilterAsync();
        }

        /// <summary>
        /// 현재 선택된 필터를 이미지에 비동기적으로 적용하는 메서드입니다.
        /// </summary>
        /// <returns>비동기 작업을 나타내는 Task</returns>
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

        /// <summary>
        /// 이미지에 그레이스케일 필터를 적용하는 메서드입니다.
        /// 슬라이더로 조절된 강도에 따라 원본 이미지와 그레이스케일 이미지를 혼합합니다.
        /// </summary>
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

        /// <summary>
        /// 이미지에 가우시안 블러 필터를 적용하는 메서드입니다.
        /// 슬라이더로 조절된 강도에 따라 블러의 정도가 결정됩니다.
        /// </summary>
        private void ApplyGaussianBlur()
        {
            int blurIntensity = 0;
            Dispatcher.Invoke(() => blurIntensity = (int)BlurIntensitySlider.Value);

            int kernelSize = blurIntensity * 2 + 1;
            Cv2.GaussianBlur(_processedImage, _processedImage, new OpenCvSharp.Size(kernelSize, kernelSize), 0);
        }

        /// <summary>
        /// 이미지에 엣지 검출 필터를 적용하는 메서드입니다.
        /// 슬라이더로 조절된 임계값에 따라 엣지 검출의 민감도가 결정됩니다.
        /// </summary>
        private void ApplyEdgeDetection()
        {
            int threshold = 0;
            Dispatcher.Invoke(() => threshold = (int)EdgeThresholdSlider.Value);

            using var edges = new Mat();
            Cv2.Canny(_processedImage, edges, threshold, threshold * 2);
            Cv2.CvtColor(edges, _processedImage, ColorConversionCodes.GRAY2BGR);
        }

        /// <summary>
        /// 이미지에서 특정 색상을 검출하는 메서드입니다.
        /// 컴보박스에서 선택된 색상(빨강, 초록, 파랑)에 따라 해당 색상 영역만 표시합니다.
        /// </summary>
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

        /// <summary>
        /// 이미지에 마스크를 적용하여 특정 색상 범위만 표시하는 메서드입니다.
        /// </summary>
        /// <param name="sourceImage">원본 이미지 (Mat)</param>
        /// <param name="hsvImage">HSV 색상 공간으로 변환된 이미지 (Mat)</param>
        /// <param name="lowerBound">검출할 색상의 하한 범위 (Scalar)</param>
        /// <param name="upperBound">검출할 색상의 상한 범위 (Scalar)</param>
        /// <param name="colorName">검출할 색상의 이름 (string)</param>
        /// <returns>마스크가 적용된 결과 이미지 (Mat)</returns>
        private Mat ApplyMask(Mat sourceImage, Mat hsvImage, Scalar lowerBound, Scalar upperBound, string colorName)
        {
            using var mask = new Mat();
            Cv2.InRange(hsvImage, lowerBound, upperBound, mask);

            var resultImage = new Mat();
            sourceImage.CopyTo(resultImage, mask);

            return resultImage;
        }

        /// <summary>
        /// 처리된 이미지를 UI에 표시하는 메서드입니다.
        /// 원본 이미지와 처리된 이미지를 비동기적으로 UI에 업데이트합니다.
        /// </summary>
        /// <returns>비동기 작업을 나타내는 Task</returns>
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

        /// <summary>
        /// OpenCV의 Mat 객체를 WPF에서 사용할 수 있는 BitmapSource로 변환하는 메서드입니다.
        /// </summary>
        /// <param name="image">변환할 Mat 객체</param>
        /// <returns>변환된 BitmapSource 객체</returns>
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

        /// <summary>
        /// 현재 사용 중인 이미지 객체들을 메모리에서 해제하는 메서드입니다.
        /// </summary>
        private void DisposeImages()
        {
            _originalImage?.Dispose();
            _processedImage?.Dispose();
        }

        /// <summary>
        /// 윈도우가 닫힐 때 호출되는 메서드입니다.
        /// 사용된 리소스를 정리합니다.
        /// </summary>
        /// <param name="e">이벤트 관련 매개변수</param>
        protected override void OnClosed(EventArgs e)
        {
            DisposeImages();
            base.OnClosed(e);
        }
    }
}