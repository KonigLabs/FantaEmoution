﻿using System.Runtime.InteropServices;

namespace KonigLabs.FantaEmotion.SDKData.Structs
{
    /// <summary>
    /// TODO - document
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Time
    {
        public int Year;
        public int Month;
        public int Day;
        public int Hour;
        public int Minute;
        public int Second;
        public int Milliseconds;
    }
}
