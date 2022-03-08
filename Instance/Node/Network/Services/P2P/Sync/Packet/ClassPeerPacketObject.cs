using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Utility;
using System;
using System.Diagnostics;

namespace SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet
{
    public class ClassPeerPacketSendObject
    {
        public ClassPeerEnumPacketSend PacketOrder;
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
        public ClassPeerPacketSendObject(string packetPeerUniqueId, string publicKey, long lastTimestampSignatureWhitelist)
        {
            PacketContent = null;
            PacketHash = null;
            PacketSignature = null;
            PacketPeerUniqueId = packetPeerUniqueId;
            PublicKey = publicKey;
            PeerLastTimestampSignatureWhitelist = lastTimestampSignatureWhitelist;
        }

        public ClassPeerPacketSendObject(byte[] packetData, out bool status)
        {
            status = false;
            if (packetData?.Length > 0)
            {
                try
                {
                    string[] splitPacketData = packetData.GetStringFromByteArrayUtf8().Split(new[] { "#" }, StringSplitOptions.None);


                    if (int.TryParse(splitPacketData[0], out int packetOrder) && long.TryParse(splitPacketData[6], out long peerLastTimestampSignatureWhitelist))
                    {
                        PacketOrder = (ClassPeerEnumPacketSend)packetOrder;
                        PacketContent = splitPacketData[1];
                        PacketHash = splitPacketData[2];
                        PacketSignature = splitPacketData[3];
                        PacketPeerUniqueId = splitPacketData[4];
                        PublicKey = splitPacketData[5];
                        PeerLastTimestampSignatureWhitelist = peerLastTimestampSignatureWhitelist;
                        status = true;
                    }

                }
                catch
                {
                    // Ignored.
                }
            }


#if DEBUG
            if (!status)
                Debug.WriteLine("Invalid send packet format: " + packetData.GetStringFromByteArrayUtf8());
#endif
        }

        public byte[] GetPacketData()
        {
            return ClassUtility.GetByteArrayFromStringUtf8((int)PacketOrder + "#" +
                (PacketContent.IsNullOrEmpty(false, out _) ? "empty" : PacketContent) + "#" +
                (PacketHash.IsNullOrEmpty(false, out _) ? "empty" : PacketHash) + "#" +
                (PacketSignature.IsNullOrEmpty(false, out _) ? "empty" : PacketSignature) + "#" +
                (PacketPeerUniqueId.IsNullOrEmpty(false, out _) ? "empty" : PacketPeerUniqueId) + "#" +
                (PublicKey.IsNullOrEmpty(false, out _) ? "empty" : PublicKey) + "#" +
                PeerLastTimestampSignatureWhitelist);
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
            PacketContent = null;
            PacketHash = null;
            PacketSignature = null;
            PacketPeerUniqueId = packetPeerUniqueId;
            PublicKey = publicKey;
            PeerLastTimestampSignatureWhitelist = lastTimestampSignatureWhitelist;
         }

        public ClassPeerPacketRecvObject(byte[] packetData, out bool status)
        {
            status = false;

            if (packetData?.Length > 0)
            {
                try
                {
                    string[] splitPacketData = packetData.GetStringFromByteArrayUtf8().Split(new[] { "#" }, StringSplitOptions.None);

                    if (int.TryParse(splitPacketData[0], out int packetOrder) && long.TryParse(splitPacketData[6], out long peerLastTimestampSignatureWhitelist))
                    {
                        PacketOrder = (ClassPeerEnumPacketResponse)packetOrder;
                        PacketContent = splitPacketData[1];
                        PacketHash = splitPacketData[2];
                        PacketSignature = splitPacketData[3];
                        PacketPeerUniqueId = splitPacketData[4];
                        PublicKey = splitPacketData[5];
                        PeerLastTimestampSignatureWhitelist = peerLastTimestampSignatureWhitelist;
                        status = true;
                    }

                }
                catch
                {
                    // Ignored.
                }
            }

#if DEBUG
            if(!status)
                Debug.WriteLine("Invalid recv packet format: " + packetData.GetStringFromByteArrayUtf8());
#endif

        }

        public byte[] GetPacketData()
        {
            return ClassUtility.GetByteArrayFromStringUtf8((int)PacketOrder + "#" +
                (PacketContent.IsNullOrEmpty(false, out _) ? "empty" : PacketContent) + "#" +
                (PacketHash.IsNullOrEmpty(false, out _) ? "empty" : PacketHash) + "#" +
                (PacketSignature.IsNullOrEmpty(false, out _) ? "empty" : PacketSignature) + "#" +
                (PacketPeerUniqueId.IsNullOrEmpty(false, out _) ? "empty" : PacketPeerUniqueId) + "#" +
                (PublicKey.IsNullOrEmpty(false, out _) ? "empty" : PublicKey) + "#" +
                PeerLastTimestampSignatureWhitelist);
        }
    }
}
