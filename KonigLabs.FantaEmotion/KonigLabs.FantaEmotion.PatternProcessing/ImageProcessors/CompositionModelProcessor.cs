﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Monads;
using System.Threading;
using System.Threading.Tasks;
using KonigLabs.FantaEmotion.Camera;
using KonigLabs.FantaEmotion.Entities;
using KonigLabs.FantaEmotion.PatternProcessing.Dto;
using KonigLabs.FantaEmotion.SDKData.Enums;
using KonigLabs.FantaEmotion.SDKData.Events;
using Image = System.Drawing.Image;

namespace KonigLabs.FantaEmotion.PatternProcessing.ImageProcessors
{
    public class CompositionModelProcessor
    {
        private readonly Template _pattern;
        private readonly ImageProcessor _imageProcessor;
        private readonly ImageUtils _imageUtils;

        public event EventHandler<ImageDto> ImageChanged;
        public event EventHandler<int> TimerElapsed;
        public event EventHandler<int> ImageNumberChanged;
        public event EventHandler<CameraEventBase> CameraErrorEvent;
        public event EventHandler CameraRemoveEvent;
        public event EventHandler CameraAddEvent;

        public CompositionModelProcessor(Template pattern, ImageProcessor imageProcessor, ImageUtils imageUtils)
        {
            _pattern = pattern;
            _imageProcessor = imageProcessor;
            _imageUtils = imageUtils;
        }

        private bool _initialized;

        public void InitializeProcessor()
        {
            if (_initialized)
                return;

            _imageProcessor.Initialize();
            _imageProcessor.CameraErrorEvent += ImageProcessorOnCameraErrorEvent;
            _imageProcessor.AddCamera += OnAddCamera;
            _imageProcessor.RemoveCamera += OnRemoveCamera;
            _initialized = true;
        }

        private void OnRemoveCamera(object sender, EventArgs e)
        {
            if (CameraRemoveEvent != null)
            {
                CameraRemoveEvent(sender, e);
            }
        }

        private void OnAddCamera(object sender, EventArgs e)
        {
            if (CameraAddEvent != null)
            {
                CameraAddEvent(sender, e);
            }
        }

        private void ImageProcessorOnCameraErrorEvent(object sender, CameraEventBase cameraError)
        {
            var handler = CameraErrorEvent;
            if (handler != null)
                handler(this, cameraError);
        }

        public virtual async Task<CompositionProcessingResult> TakePictureAsync(byte[] liveViewImageStream, AEMode selectedAeMode, ApertureValue selectedAvValue, CameraISOSensitivity selectedIsoSensitivity, ShutterSpeed selectedShutterSpeed, WhiteBalance selectedWhiteBalance, CancellationToken token)
        {
            Size liveViewImageStreamSize;
            using (var stream = new MemoryStream(liveViewImageStream))
            {
                    var img = Image.FromStream(stream);
                    liveViewImageStreamSize = img.Size;
            }

            return new CompositionProcessingResult(_pattern, await TakePictureInternal(liveViewImageStreamSize, selectedAeMode, selectedAvValue, selectedIsoSensitivity, selectedShutterSpeed, selectedWhiteBalance, token));
        }

        protected async Task<byte[]> TakePictureInternal(Size liveViewImageStreamSize, AEMode selectedAeMode, ApertureValue selectedAvValue, CameraISOSensitivity selectedIsoSensitivity, ShutterSpeed selectedShutterSpeed, WhiteBalance selectedWhiteBalance, CancellationToken token)
        {
            return await Task.Run(() => Run(liveViewImageStreamSize, selectedAeMode, selectedAvValue, selectedIsoSensitivity, selectedShutterSpeed, selectedWhiteBalance, token), token);
        }

        private async Task<byte[]> Run(Size liveViewImageStreamSize, AEMode selectedAeMode, ApertureValue selectedAvValue, CameraISOSensitivity selectedIsoSensitivity, ShutterSpeed selectedShutterSpeed, WhiteBalance selectedWhiteBalance, CancellationToken token)
        {

            var settings = GetCameraPhotoSettings();

            var pictures = new List<byte[]>();

            for (var i = 0; i < _pattern.Images.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                RaiseImageNumberChanged(i + 1);
                RaiseTimerElapsed(4);
                await Task.Delay(TimeSpan.FromSeconds(2), token);

                for (var j = 3; j >= 0; j--)
                {
                    RaiseTimerElapsed(j);
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }

                SetCameraSettings(Enum.Parse(typeof(AEMode), settings.SelectedAeMode),
                    Enum.Parse(typeof(WhiteBalance), settings.SelectedWhiteBalance),
                    Enum.Parse(typeof(ApertureValue), settings.SelectedAvValue),
                    Enum.Parse(typeof(CameraISOSensitivity), settings.SelectedIsoSensitivity),
                    Enum.Parse(typeof(ShutterSpeed), settings.SelectedShutterSpeed));
                //await Task.Delay(TimeSpan.FromSeconds(1));

                //RaiseImageNumberChanged(i + 1);
                //await Task.Delay(TimeSpan.FromSeconds(1), token);
                token.ThrowIfCancellationRequested();
                var picture = await _imageProcessor.DoTakePicture();
                pictures.Add(picture);

                token.ThrowIfCancellationRequested();
                //await Task.Delay(TimeSpan.FromSeconds(3), token); //todo

                SetCameraSettings(selectedAeMode, selectedWhiteBalance,
                    selectedAvValue, selectedIsoSensitivity,
                    selectedShutterSpeed);
                StopLiveView();
                StartLiveView();
            }

            var result = _imageUtils.ProcessImages(pictures, liveViewImageStreamSize, _pattern);
            return result;
        }

        public DirectoryInfo GetVideoDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var info = !Directory.Exists(Path.Combine(baseDir, "Videos"))
                ? Directory.CreateDirectory(Path.Combine(baseDir, "Videos"))
                : new DirectoryInfo(Path.Combine(baseDir, "Videos"));
            return info;
        }

        public void StartRecordVideo()
        {
            _imageProcessor.StartRecordVideo(GetVideoDirectory().FullName);
        }

        public string StopRecordVideo(bool force = false)
        {
            var result = _imageProcessor.StopRecordVideo();

            if (!result || force)
                return null;

            var info = GetVideoDirectory();
            var lastVideo = info.EnumerateFiles("MVI*.mov").OrderByDescending(p => p.CreationTimeUtc).FirstOrDefault();

            var resultVideo = lastVideo?.FullName;
            if (lastVideo != null)
            {
                Process.Start("ffmpeg.exe",
                    $"-i \"{info.FullName}\\{lastVideo.Name}\" -vf scale=400x226 \"{info.FullName}\\Min{lastVideo.Name}\"");
                resultVideo = resultVideo.Replace(lastVideo.Name, "Min" + lastVideo.Name);
            }

            return resultVideo;
        }

        public bool IsRecordingVideo()
        {
            return _imageProcessor.IsFilming();
        }

        private void SetCameraSettings(AEMode aeMode, WhiteBalance balance, ApertureValue apertureValue,
            CameraISOSensitivity cameraIsoSensitivity, ShutterSpeed shutterSpeed)
        {
            //NOT SUPPORTED on EOS 1100D
            //_imageProcessor.SetSetting((uint)PropertyId.AEMode, (uint)aeMode);
            _imageProcessor.SetSetting((uint)PropertyId.WhiteBalance, (uint)balance);
            _imageProcessor.SetSetting((uint)PropertyId.Av, (uint)apertureValue);
            //_imageProcessor.SetSetting((uint)PropertyId.ExposureCompensation, (uint)));
            _imageProcessor.SetSetting((uint)PropertyId.ISOSpeed, (uint)cameraIsoSensitivity);
            _imageProcessor.SetSetting((uint)PropertyId.Tv, (uint)shutterSpeed);
        }

        private dynamic GetCameraPhotoSettings()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Path.Combine(baseDir, "CameraPhotoSettings.txt");
            if (!File.Exists(filePath)) return null;

            var lines = File.ReadAllLines(Path.Combine(baseDir, "CameraPhotoSettings.txt"));
            return new
            {
                SelectedAeMode = lines.SingleOrDefault(x => x.Contains("SelectedAeMode")).With(x => x.Replace("SelectedAeMode=", "")),
                SelectedAvValue = lines.SingleOrDefault(x => x.Contains("SelectedAvValue")).With(x => x.Replace("SelectedAvValue=", "")),
                SelectedIsoSensitivity = lines.SingleOrDefault(x => x.Contains("SelectedIsoSensitivity")).With(x => x.Replace("SelectedIsoSensitivity=", "")),
                SelectedWhiteBalance = lines.SingleOrDefault(x => x.Contains("SelectedWhiteBalance")).With(x => x.Replace("SelectedWhiteBalance=", "")),
                SelectedShutterSpeed = lines.SingleOrDefault(x => x.Contains("SelectedShutterSpeed")).With(x => x.Replace("SelectedShutterSpeed=", ""))
            };
        }

        private void RaiseImageNumberChanged(int newNumber)
        {
            ImageNumberChanged?.Invoke(this, newNumber);
        }


        private void RaiseTimerElapsed(int tick)
        {
            TimerElapsed?.Invoke(this, tick);
        }

        private void RaiseImageChanged(byte[] imgBuf)
        {
            ImageChanged?.Invoke(this, new ImageDto(imgBuf));
        }

        //private void RaiseImageChanged(byte[] imgBuf, int width, int height)
        //{
        //    ImageChanged?.Invoke(this, new ImageDto(imgBuf, width, height));
        //}

        private void ImageProcessorOnStreamChanged(object sender, byte[] bytes)
        {
            RaiseImageChanged(bytes);
        }

        public void Dispose()
        {
            _initialized = false;
            _imageProcessor.CameraErrorEvent -= ImageProcessorOnCameraErrorEvent;
            _imageProcessor.Dispose();
        }

        public virtual void SetSetting(uint settingId, uint settingValue)
        {
            _imageProcessor.SetSetting(settingId, settingValue);
        }

        public virtual void SetFocus(uint focus)
        {
            _imageProcessor.SetFocus(focus);
        }

        public virtual void StopLiveView()
        {
            _imageProcessor.StreamChanged -= ImageProcessorOnStreamChanged;
            //todo stop live view
            //_imageProcessor.StartLiveView();
        }

        public virtual void StartLiveView()
        {
            _imageProcessor.StreamChanged += ImageProcessorOnStreamChanged;
            _imageProcessor.StartLiveView();
        }

        public virtual void RefreshCamera()
        {
            _imageProcessor.DoRefresh();
        }

        public virtual void CloseSession()
        {
            _imageProcessor.CloseSession();
            _initialized = false;
            _imageProcessor.CameraErrorEvent -= ImageProcessorOnCameraErrorEvent;
        }

        public virtual bool OpenSession()
        {
            InitializeProcessor();

            return _imageProcessor.DoOpenSession();
        }
    }
}
