using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Utility;
using System;
using System.Diagnostics;
using System.Linq;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet
{
    public class ClassPeerPacketSendObject
    {
        public ClassPeerEnumPacketSend PacketOrder;
        public string PacketContent; // The serialized packet encrypted.
        public string PacketHash; // Packet hash.
        public string PacketSignature; // The signature of the packet hash.
        public string PacketPeerUniqueId;
        public string PublicKey;
        public long PeerLastTimestampSignatureWhitelist;

  
        /// <summary>
        /// The peer unique id is mandatory.
        /// </summary>
        /// <param name="packetPeerUniqueId"></param>
        public ClassPeerPacketSendObject(string packetPeerUniqueId, string publicKey, long lastTimestampSignatureWhitelist)
        {
            PacketPeerUniqueId = packetPeerUniqueId;
            PublicKey = publicKey;
            PeerLastTimestampSignatureWhitelist = lastTimestampSignatureWhitelist;
        }

        /// <summary>
        /// Recompile the packet data.
        /// </summary>
        /// <param name="packetData"></param>
        /// <param name="status"></param>
        public ClassPeerPacketSendObject(byte[] packetData, out bool status)
        {
            status = false;
            if (packetData?.Length > 0)
            {
                try
                {
                    string[] splitPacketData = packetData.GetStringFromByteArrayUtf8().Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);


                    if (int.TryParse(splitPacketData[0], out int packetOrder) &&
                    long.TryParse(splitPacketData[6], out long peerLastTimestampSignatureWhitelist))
                    {
                        PacketOrder = (ClassPeerEnumPacketSend)packetOrder;
                        PacketContent = splitPacketData[1];
                        PacketHash = splitPacketData[2] != "empty" ? ClassUtility.DecompressHexString(splitPacketData[2]) : string.Empty;
                        PacketSignature = splitPacketData[3];
                        PacketPeerUniqueId = splitPacketData[4];
                        PublicKey = splitPacketData[5];
                        PeerLastTimestampSignatureWhitelist = peerLastTimestampSignatureWhitelist;
                        status = true;
                    }
#if DEBUG
                    else
                    {
                        Debug.WriteLine("Can't convert packet data of the " + typeof(ClassPeerPacketSendObject).Name + " | length expected: 7/" + splitPacketData.Length);
                        Debug.WriteLine("Content: " + packetData.GetStringFromByteArrayUtf8());
                    }
#endif

                }
                catch (Exception error)
                {
#if DEBUG
                    Debug.WriteLine("Error to build the packet data. Exception: " + error.Message);
#endif
                    // Ignored.
                }
            }


#if DEBUG
            if (!status)
                Debug.WriteLine("Invalid send packet format: " + packetData.GetStringFromByteArrayUtf8());
#endif
        }

        /// <summary>
        /// Generate the packet data to send.
        /// FASTER than JSON. If you accumulate many requests and make a sum of timespend to handle them. A benchmark.
        /// That's not constitute a fail of security at all.
        /// </summary>
        /// <returns></returns>
        public byte[] GetPacketData()
        {

            string packetData = (int)PacketOrder + "#" +
                (PacketContent.IsNullOrEmpty(false, out _) ? "empty" : PacketContent) + "#" +
                (PacketHash.IsNullOrEmpty(false, out _) ? "empty" : ClassUtility.CompressHexString(PacketHash)) + "#" +
                (PacketSignature.IsNullOrEmpty(false, out _) ? "empty" : PacketSignature) + "#" +
                (PacketPeerUniqueId.IsNullOrEmpty(false, out _) ? "empty" : PacketPeerUniqueId) + "#" +
                (PublicKey.IsNullOrEmpty(false, out _) ? "empty" : PublicKey) + "#" +
                PeerLastTimestampSignatureWhitelist;
            return packetData.GetByteArray();

        }

        /// <summary>
        /// Clean up packet data.
        /// </summary>
        public void ClearPacketData()
        {
            PacketContent?.Clear();
            PacketHash?.Clear();
            PacketSignature?.Clear();
            PacketPeerUniqueId?.Clear();
            PublicKey?.Clear();
        }
    }


    public class ClassPeerPacketRecvObject
    {
        public ClassPeerEnumPacketResponse PacketOrder;
        public string PacketContent; // The serialized packet encrypted.
        public string PacketHash;
        public string PacketSignature; // The signature of the packet hash.
        public string PacketPeerUniqueId;
        public string PublicKey;
        public long PeerLastTimestampSignatureWhitelist;

        /// <summary>
        /// The peer unique id is mandatory.
        /// </summary>
        /// <param name="packetPeerUniqueId"></param>
        public ClassPeerPacketRecvObject(string packetPeerUniqueId, string publicKey, long lastTimestampSignatureWhitelist)
        {
            PacketPeerUniqueId = packetPeerUniqueId;
            PublicKey = publicKey;
            PeerLastTimestampSignatureWhitelist = lastTimestampSignatureWhitelist;
        }

        /// <summary>
        /// Recompile the packet data.
        /// </summary>
        /// <param name="packetData"></param>
        /// <param name="status"></param>
        public ClassPeerPacketRecvObject(byte[] packetData, out bool status)
        {
            status = false;
            string[] splitPacketData = null;

            try
            {
                if (packetData?.Length > 0)
                {
                    splitPacketData = packetData.GetStringFromByteArrayUtf8().Split(new[] { "#" }, StringSplitOptions.RemoveEmptyEntries);


                    if (int.TryParse(splitPacketData[0], out int packetOrder) &&
                        long.TryParse(splitPacketData[6], out long peerLastTimestampSignatureWhitelist))
                    {
                        PacketOrder = (ClassPeerEnumPacketResponse)packetOrder;
                        PacketContent = splitPacketData[1];
                        PacketHash = splitPacketData[2] != "empty" ? ClassUtility.DecompressHexString(splitPacketData[2]) : string.Empty;
                        PacketSignature = splitPacketData[3];
                        PacketPeerUniqueId = splitPacketData[4];
                        PublicKey = splitPacketData[5];
                        PeerLastTimestampSignatureWhitelist = peerLastTimestampSignatureWhitelist;
                        status = true;
                    }


                }

#if DEBUG
                if (!status)
                    Debug.WriteLine("Invalid recv packet format, length: " + splitPacketData != null ? splitPacketData.Length : 0);
#endif
            }
            catch(Exception error)
            {
#if DEBUG
                Debug.WriteLine("Can't compile the packet data received. Exception: " + error.Message);
#endif
            }

        }

        /// <summary>
        /// Generate the packet data to send.
        /// FASTER than JSON. If you accumulate many requests and make a sum of timespend to handle them. A benchmark.
        /// That's not constitute a fail of security at all.
        /// </summary>
        /// <returns></returns>
        public byte[] GetPacketData()
        {

            string packetData = (int)PacketOrder + "#" +
                (PacketContent.IsNullOrEmpty(false, out _) ? "empty" : PacketContent) + "#" +
                (PacketHash.IsNullOrEmpty(false, out _) ? "empty" : ClassUtility.CompressHexString(PacketHash)) + "#" +
                (PacketSignature.IsNullOrEmpty(false, out _) ? "empty" : PacketSignature) + "#" +
                (PacketPeerUniqueId.IsNullOrEmpty(false, out _) ? "empty" : PacketPeerUniqueId) + "#" +
                (PublicKey.IsNullOrEmpty(false, out _) ? "empty" : PublicKey) + "#" +
                PeerLastTimestampSignatureWhitelist;
            return packetData.GetByteArray();

        }

        /// <summary>
        /// Clean up packet data.
        /// </summary>
        public void ClearPacketData()
        {
            PacketContent?.Clear();
            PacketHash?.Clear();
            PacketSignature?.Clear();
            PacketPeerUniqueId?.Clear();
            PublicKey?.Clear();
        }
    }
}
