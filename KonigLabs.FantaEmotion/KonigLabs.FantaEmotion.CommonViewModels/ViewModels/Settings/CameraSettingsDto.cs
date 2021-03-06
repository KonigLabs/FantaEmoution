﻿using KonigLabs.FantaEmotion.SDKData.Enums;

namespace KonigLabs.FantaEmotion.CommonViewModels.ViewModels.Settings
{
    public class CameraSettingsDto
    {
        //NOT SUPPORTED on EOS 1100D
        //public AEMode SelectedAeMode { get; set; }

        public ExposureCompensation SelectedCompensation { get; set; }

        public WhiteBalance SelectedWhiteBalance { get; set; }

        public CameraISOSensitivity SelectedIsoSensitivity { get; set; }

        public ShutterSpeed SelectedShutterSpeed { get; set; }

        public ApertureValue SelectedAvValue { get; set; }
    }
}
