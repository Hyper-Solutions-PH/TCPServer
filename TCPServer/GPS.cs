using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace TCPServer
{
    class GPS
    {
        public GpsReliability Viled { get; private set; }

        public byte Expand { get; private set; } // indicates whether an extension has been made, 0 is not extended,
                             // and is grouped by existing field.
        public DataType Real { get; private set; }
        public byte[] Reserved;
        public LatitudeType Latitude { get; private set; }
        public LongitudeType Longitude { get; private set; }
        public decimal Speed { get; private set; }
        public decimal Direction { get; private set; }
        public int Height { get; private set; }
        public DateTime Time { get; private set; }
        public override string ToString()
        {
            return $"{Time}|| {Latitude}, {Longitude}";
        }
        public GPS(byte[] rawData)
        {
            GCHandle gcHandle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                var rawGPS = (GPS.RawGPS)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(RawGPS));
                if (BitConverter.IsLittleEndian)
                {
                    //since network is big endian we have to correct for it
                    rawGPS.ulongitude = IPAddress.NetworkToHostOrder(rawGPS.ulongitude);
                    rawGPS.ulatitude = IPAddress.NetworkToHostOrder(rawGPS.ulatitude);
                    rawGPS.uspeed = IPAddress.NetworkToHostOrder(rawGPS.uspeed);
                    rawGPS.udirect = IPAddress.NetworkToHostOrder(rawGPS.udirect);
                    rawGPS.uhigh = IPAddress.NetworkToHostOrder(rawGPS.uhigh);
                }
                ReadFromRawGPS(rawGPS);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        private void ReadFromRawGPS(RawGPS rawGPS)
        {

            const uint HemisphereMask = 0x80000000;
            const uint CoordinateMask = 0x1FFFFFFF;
            const decimal CoordinatesDivisor = 1000000;
            const decimal DirectionDivisor = 100;
            const decimal SpeedDivisor = 100;
            Viled = (GpsReliability)rawGPS.viled;
            Expand = rawGPS.uexpand;
            Real = (DataType)rawGPS.ureal;
            Reserved = rawGPS.reserver;
            Latitude = new LatitudeType { Hemisphere = (LatitudeType.LatHemisphere)(rawGPS.ulatitude & HemisphereMask), Latitude = (rawGPS.ulatitude & CoordinateMask) / CoordinatesDivisor };
            Longitude = new LongitudeType { Hemisphere = (LongitudeType.LonHemisphere)(rawGPS.ulatitude & HemisphereMask), Longitude = (rawGPS.ulongitude & CoordinateMask) / CoordinatesDivisor };
            Speed = rawGPS.uspeed / SpeedDivisor; 
            Height = rawGPS.uhigh;
            Direction = rawGPS.uhigh / DirectionDivisor;
            
            var dateString = Encoding.Default.GetString(rawGPS.utime).Trim('\0');
            try
            {
                Time = DateTime.ParseExact(dateString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                Console.WriteLine("[ERROR]DATE FORMAT IS INAPPROPRIATE", ConsoleColor.Red);
            }    
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = Size)]
        internal unsafe struct RawGPS
        {
            const int Size = 36;
            public byte viled; // 0 indicates that the subsequent GPS data is reliable and effective;
                        // 1 indicates that the data is not necessarily reliable, this situation usually occurs
                        //when the number of tracking satellites is insufficient;
                        // 2 indicates that the device has no GPS module.
            public byte uexpand; // indicates whether an extension has been made, 0 is not extended,
                           // and is grouped by existing field.
            public byte ureal; //data type, 0: real-time data, 1: fill data
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] reserver;
            public int ulongitude;
            public int ulatitude;
            public int uspeed;//not present in the doc, but there's left-over comment
                             //and struct sent over by network seems to be bigger by 4 bytes
            public int udirect;
            public int uhigh;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] utime;
        }

        public struct LatitudeType
        {
            public enum LatHemisphere { North = 0, South = 1}

            public LatHemisphere Hemisphere;
            public decimal Latitude;

            public override string ToString()
            {
                return $"{Latitude.ToString()}*{(Hemisphere == LatHemisphere.North ? "N" : "S")}";
            }
        }

        public struct LongitudeType
        {
            public enum LonHemisphere { East = 0, West = 1 }

            public LonHemisphere Hemisphere;
            public decimal Longitude;
            public override string ToString()
            {
                return $"{Longitude.ToString()}*{(Hemisphere == LonHemisphere.East ? "E" : "W")}";
            }
        }

        public enum GpsReliability
        {
            Reliable = 0,
            PartlyReliable = 1,
            NoGPSModule = 2
        }

        public enum DataType
        {
            RealTime = 0,
            Fill = 1
        }
    }

    

}
