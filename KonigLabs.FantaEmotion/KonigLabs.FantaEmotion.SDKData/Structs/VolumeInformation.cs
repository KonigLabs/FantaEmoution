﻿using System;
using System.Runtime.InteropServices;
using KonigLabs.FantaEmotion.SDKData.Miscellaneous;

namespace KonigLabs.FantaEmotion.SDKData.Structs
{
    /// <summary>
    /// TODO - document
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VolumeInformation
    {
        public UInt32 StorageType;
        public UInt32 Access;
        public ulong MaxCapacity;
        public ulong FreeSpaceInBytes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = GlobalConstants.MAX_NAME)]
        public string VolumeLabel;
    }
}
