﻿using System;
using System.Runtime.InteropServices;
using KonigLabs.FantaEmotion.SDKData.Miscellaneous;

namespace KonigLabs.FantaEmotion.SDKData.Structs
{
    /// <summary>
    /// TODO - document
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyDescription
    {
        public int Form;
        public UInt32 Access;
        public int NumberOfElements;

        // TODO - rename this member
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GlobalConstants.PROPERTY_DESCRIPTIONS_ARRAY_SIZE)]
        public int[] Elements;
    }
}
