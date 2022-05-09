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


                    if (int.TryParse(splitPacketData[0], out int packetOrder) &&
                        long.TryParse(splitPacketData[6], out long peerLastTimestampSignatureWhitelist))
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
                    else
                    {
                        if (int.TryParse(splitPacketData[0], out packetOrder) &&
                                                 long.TryParse(splitPacketData[7], out peerLastTimestampSignatureWhitelist))
                        {
                            PacketOrder = (ClassPeerEnumPacketSend)packetOrder;
                            PacketContent = splitPacketData[2];
                            PacketHash = splitPacketData[3];
                            PacketSignature = splitPacketData[4];
                            PacketPeerUniqueId = splitPacketData[5];
                            PublicKey = splitPacketData[6];
                            PeerLastTimestampSignatureWhitelist = peerLastTimestampSignatureWhitelist;
                            status = true;
                        }
                    }
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

        public byte[] GetPacketData()
        {
            try
            {
                return ClassUtility.GetByteArrayFromStringUtf8((int)PacketOrder + "#" +
                    (PacketContent.IsNullOrEmpty(false, out _) ? "empty" : PacketContent) + "#" +
                    (PacketHash.IsNullOrEmpty(false, out _) ? "empty" : PacketHash) + "#" +
                    (PacketSignature.IsNullOrEmpty(false, out _) ? "empty" : PacketSignature) + "#" +
                    (PacketPeerUniqueId.IsNullOrEmpty(false, out _) ? "empty" : PacketPeerUniqueId) + "#" +
                    (PublicKey.IsNullOrEmpty(false, out _) ? "empty" : PublicKey) + "#" +
                    PeerLastTimestampSignatureWhitelist);
            }
            catch (Exception error)
            {
#if DEBUG
                Debug.WriteLine("Error to get the packet data. Exception: " + error.Message);
#endif
            }
            return null;
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


                    if (int.TryParse(splitPacketData[0], out int packetOrder) &&
                        long.TryParse(splitPacketData[6], out long peerLastTimestampSignatureWhitelist))
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
                    else
                    {
                        if (int.TryParse(splitPacketData[1], out packetOrder) && long.TryParse(splitPacketData[7], out peerLastTimestampSignatureWhitelist))
                        {
                            PacketOrder = (ClassPeerEnumPacketResponse)packetOrder;
                            PacketContent = splitPacketData[2];
                            PacketHash = splitPacketData[3];
                            PacketSignature = splitPacketData[4];
                            PacketPeerUniqueId = splitPacketData[5];
                            PublicKey = splitPacketData[6];
                            PeerLastTimestampSignatureWhitelist = peerLastTimestampSignatureWhitelist;
                            status = true;
                        }
                    }
                }
                catch(Exception error)
                {
#if DEBUG
                    Debug.WriteLine("Error to build the packet data. Exception: "+error.Message);
#endif
                }
            }

#if DEBUG
            if(!status)
                Debug.WriteLine("Invalid recv packet format: " + packetData.GetStringFromByteArrayUtf8());
#endif

        }

        public byte[] GetPacketData()
        {
            try
            {
                return ClassUtility.GetByteArrayFromStringUtf8((int)PacketOrder + "#" +
                    (PacketContent.IsNullOrEmpty(false, out _) ? "empty" : PacketContent) + "#" +
                    (PacketHash.IsNullOrEmpty(false, out _) ? "empty" : PacketHash) + "#" +
                    (PacketSignature.IsNullOrEmpty(false, out _) ? "empty" : PacketSignature) + "#" +
                    (PacketPeerUniqueId.IsNullOrEmpty(false, out _) ? "empty" : PacketPeerUniqueId) + "#" +
                    (PublicKey.IsNullOrEmpty(false, out _) ? "empty" : PublicKey) + "#" +
                    PeerLastTimestampSignatureWhitelist);
            }
            catch(Exception error)
            {
#if DEBUG
                Debug.WriteLine("Error to get the packet data. Exception: " + error.Message);
#endif
            }
            return null;
        }
    }
}
