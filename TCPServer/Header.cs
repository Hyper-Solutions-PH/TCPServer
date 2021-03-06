﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace TCPServer
{
    public enum PayloadType { Json = 0, Binary = 22 };

    public class Header
    {
        public int VersionNumber { get; set; }
        public bool Padding { get; set; }
        public bool ImportantEvent { get; set; }
        public int ContributorSourceCount { get; set; }
        public PayloadType PayloadType { get; set; }
        public int SynchronizationSourceIdentifier { get; set; }
        public int TcpPayloadLengthOrUdpSequenceNumber { get; set; }
        public int Reserved { get; set; }
        public int HeaderSize => RawHeader.Size + (this.ContributorSourceCount * sizeof(int));

        public Header(byte[] rawData)
        {
            GCHandle gcHandle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
            try
            {
                var rawHeader = (RawHeader)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(RawHeader));
                if (BitConverter.IsLittleEndian)
                {
                    //since network is big endian we have to correct for it
                    rawHeader.Packed = (uint)IPAddress.NetworkToHostOrder((int)rawHeader.Packed);
                    rawHeader.TcpPayloadLengthOrUdpSequenceNumber = IPAddress.NetworkToHostOrder(rawHeader.TcpPayloadLengthOrUdpSequenceNumber);
                    rawHeader.Reserved = IPAddress.NetworkToHostOrder(rawHeader.Reserved);
                }
                ReadFromRawHeader(rawHeader);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        private Header() { }
        
        public Header Clone()
        {
            return new Header
            {
                VersionNumber = VersionNumber,
                Padding = Padding,
                ImportantEvent = ImportantEvent,
                ContributorSourceCount = ContributorSourceCount,
                PayloadType = PayloadType,
                SynchronizationSourceIdentifier = SynchronizationSourceIdentifier,
                TcpPayloadLengthOrUdpSequenceNumber = TcpPayloadLengthOrUdpSequenceNumber,
                Reserved = Reserved
            };
        }
        public byte[] GetRawHeader()
        {
            var rawHeader = WriteToRawHeader();
            if (BitConverter.IsLittleEndian)
            {
                //since network is big endian we have to correct for it
                rawHeader.Packed = (uint)IPAddress.HostToNetworkOrder((int)rawHeader.Packed);
                rawHeader.TcpPayloadLengthOrUdpSequenceNumber = IPAddress.HostToNetworkOrder(rawHeader.TcpPayloadLengthOrUdpSequenceNumber);
                rawHeader.Reserved = IPAddress.HostToNetworkOrder(rawHeader.Reserved);
            }
            byte[] rawData = new byte[RawHeader.Size];
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms, Encoding.UTF8))
                {
                    bw.Write(rawHeader.Packed);
                    bw.Write(rawHeader.TcpPayloadLengthOrUdpSequenceNumber);
                    bw.Write(rawHeader.Reserved);
                }
                rawData = ms.ToArray();
            }
            return rawData;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = Size)]
        public unsafe struct RawHeader
        {
            public const int Size = 12;
            /* _________________________________________________________________
             * |    1-2 bit   | 3 bit |4 bit |  5-8 bit |   9-16bit  | 17-32 bit|
             * |______________|_______|______|__________|____________|__________|
             * |Version Number|Padding|Events|CSRC Count|PAYLOAD TYPE|   SSRC   |
             * |______________|_______|______|__________|____________|__________|
             */
            public uint Packed;
            public int TcpPayloadLengthOrUdpSequenceNumber;
            public int Reserved;
        }

        private void ReadFromRawHeader(RawHeader rawHeader)
        {
            TcpPayloadLengthOrUdpSequenceNumber = rawHeader.TcpPayloadLengthOrUdpSequenceNumber;
            Reserved = rawHeader.Reserved;
            SynchronizationSourceIdentifier = (int)(rawHeader.Packed & SsrcMask);
            PayloadType = (PayloadType)((rawHeader.Packed & PayloadTypeMask) >> PayloadTypeOffset);
            ContributorSourceCount = (int)((rawHeader.Packed & CsrcCountMask) >> CsrcCountOffset);
            ImportantEvent = Convert.ToBoolean((rawHeader.Packed & EventMask) >> EventOffset);
            Padding = Convert.ToBoolean((rawHeader.Packed & PaddingMask) >> PaddingOffset);
            VersionNumber = (int)((rawHeader.Packed & VersionNumberMask) >> VersionNumberOffset);
        }

        private RawHeader WriteToRawHeader()
        {
            var rawHeader = new RawHeader();
            rawHeader.TcpPayloadLengthOrUdpSequenceNumber = TcpPayloadLengthOrUdpSequenceNumber;
            rawHeader.Reserved = Reserved;
            rawHeader.Packed = (uint)SynchronizationSourceIdentifier;
            rawHeader.Packed |= ((uint)PayloadType << PayloadTypeOffset);
            rawHeader.Packed |= ((uint)ContributorSourceCount << CsrcCountOffset);
            rawHeader.Packed |= (Convert.ToUInt32(ImportantEvent) << EventOffset);
            rawHeader.Packed |= (Convert.ToUInt32(Padding) << PaddingOffset);
            rawHeader.Packed |= ((uint)VersionNumber << VersionNumberOffset);
            return rawHeader;
        }
        /* _________________________________________________________________
         * |    1-2 bit   | 3 bit |4 bit |  5-8 bit |   9-16bit  | 17-32 bit|
         * |______________|_______|______|__________|____________|__________|
         * |Version Number|Padding|Events|CSRC Count|PAYLOAD TYPE|   SSRC   |
         * |______________|_______|______|__________|____________|__________|
         */
        private const uint SsrcMask = 0x0000FFFF;
        private const uint PayloadTypeMask = 0x00FF0000;
        private const int PayloadTypeOffset = 16;
        private const uint CsrcCountMask = 0x0F000000;
        private const int CsrcCountOffset = 24;
        private const uint EventMask = 0x10000000;
        private const int EventOffset = 28;
        private const uint PaddingMask = 0x20000000;
        private const int PaddingOffset = 29;
        private const uint VersionNumberMask = 0xC0000000;
        private const int VersionNumberOffset = 30;
    }
}


