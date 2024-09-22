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

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 이미지 로드 버튼 클릭 이벤트 핸들러
        /// </summary>
        private async void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            /* openFileDialog를 사용하여 이미지 파일을 선택하고 로드한다.
             * 선택된 이미지를 로드하고 필터를 적용한다.
             * 오류 발생 시 메시지 박스로 사용자에게 알린다.
             */
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    await LoadImageAsync(openFileDialog.FileName);
                    await ApplyFiltersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 이미지를 비동기적으로 로드하는 메서드
        /// </summary>
        private async Task LoadImageAsync(string fileName)
        {
            /* fileName: 로드할 이미지 파일의 경로
             * 이미지를 비동기적으로 로드하고 _originalImage와 _processedImage에 저장한다.
             * 로드된 이미지를 화면에 표시한다.
             */
            await Task.Run(() =>
            {
                DisposeImages();
                _originalImage = Cv2.ImRead(fileName);
                _processedImage = _originalImage.Clone();
            });

            await UpdateDisplayImageAsync();
        }

        /// <summary>
        /// 그레이스케일 강도 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private async void GrayIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            /* e.NewValue: 새로 설정된 그레이스케일 강도 값 (0-1 사이의 double)
             * 슬라이더 값이 변경될 때마다 필터를 다시 적용한다.
             */
            if (_originalImage == null || _isProcessing) return;
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// 블러 강도 슬라이더 값 변경 이벤트 핸들러
        /// </summary>
        private async void BlurIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            /* e.NewValue: 새로 설정된 블러 강도 값 (0-50 사이의 double)
             * 슬라이더 값이 변경될 때마다 필터를 다시 적용한다.
             */
            if (_originalImage == null || _isProcessing) return;
            await ApplyFiltersAsync();
        }

        /// <summary>
        /// 필터를 비동기적으로 적용하는 메서드
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            /* 그레이스케일과 가우시안 블러 필터를 순차적으로 적용한다.
             * 필터 적용 중 오류 발생 시 메시지 박스로 사용자에게 알린다.
             */
            if (_isProcessing) return;
            _isProcessing = true;

            try
            {
                await Task.Run(() =>
                {
                    _processedImage = _originalImage.Clone();
                    ApplyGrayscale();
                    ApplyGaussianBlur();
                });

                await UpdateDisplayImageAsync();
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 그레이스케일 필터를 적용하는 메서드
        /// </summary>
        private void ApplyGrayscale()
        {
            /* grayIntensity: 0-1 사이의 double 값
             * 해당 값을 grayIntensity에 동적으로 받아와 흑백의 정도를 조절할 수 있다.
             * 0에 가까울수록 원본 이미지에 가깝고, 1에 가까울수록 완전한 흑백 이미지가 된다.
             */
            double grayIntensity = 0;
            Dispatcher.Invoke(() => grayIntensity = GrayIntensitySlider.Value);
            if (grayIntensity == 0) return;

            using var gray = new Mat();
            Cv2.CvtColor(_originalImage, gray, ColorConversionCodes.BGR2GRAY);
            using var colorMat = new Mat();
            Cv2.CvtColor(gray, colorMat, ColorConversionCodes.GRAY2BGR);
            Cv2.AddWeighted(_originalImage, 1 - grayIntensity, colorMat, grayIntensity, 0, _processedImage);
        }

        /// <summary>
        /// 가우시안 블러 필터를 적용하는 메서드
        /// </summary>
        private void ApplyGaussianBlur()
        {
            /* blurIntensity: 0-50 사이의 int 값
             * 해당 값을 blurIntensity에 동적으로 받아와 블러의 강도를 조절할 수 있다.
             * 값이 클수록 더 강한 블러 효과가 적용된다.
             */
            int blurIntensity = 0;
            Dispatcher.Invoke(() => blurIntensity = (int)BlurIntensitySlider.Value);
            if (blurIntensity == 0) return;

            int kernelSize = blurIntensity * 2 + 1;
            Cv2.GaussianBlur(_processedImage, _processedImage, new OpenCvSharp.Size(kernelSize, kernelSize), 0);
        }

        /// <summary>
        /// 처리된 이미지를 화면에 비동기적으로 표시하는 메서드
        /// </summary>
        private async Task UpdateDisplayImageAsync()
        {
            /* _processedImage를 BitmapSource로 변환하여 UI에 표시한다.
             * 이미지 변환 작업은 백그라운드 스레드에서 수행하고,
             * UI 업데이트는 UI 스레드에서 수행한다.
             */
            if (_processedImage == null) return;

            BitmapSource bitmap = null;
            await Task.Run(() =>
            {
                using var stream = _processedImage.ToMemoryStream(".png");
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                bitmap = bitmapImage;
            });

            await Dispatcher.InvokeAsync(() =>
            {
                DisplayImage.Source = bitmap;
            });
        }

        /// <summary>
        /// 이미지 리소스를 해제하는 메서드
        /// </summary>
        private void DisposeImages()
        {
            /* _originalImage와 _processedImage의 리소스를 해제한다.
             * 이 메서드는 새 이미지를 로드하기 전이나 애플리케이션이 종료될 때 호출된다.
             */
            _originalImage?.Dispose();
            _processedImage?.Dispose();
        }

        /// <summary>
        /// 윈도우가 닫힐 때 호출되는 메서드
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            /* 윈도우가 닫힐 때 이미지 리소스를 해제하고 기본 OnClosed 메서드를 호출한다.
             */
            DisposeImages();
            base.OnClosed(e);
        }
    }
}