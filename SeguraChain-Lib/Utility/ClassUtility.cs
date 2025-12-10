using LZ4;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.Network;
using SeguraChain_Lib.Other.Object.SHA3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SeguraChain_Lib.Utility
{
    public class ClassUtility
    {
        #region Constant objects.

        private const string Base64Regex = @"^[a-zA-Z0-9\+/]*={0,3}$";

        private static readonly List<string> ListOfCharacters = new List<string>
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u",
            "v", "w", "x", "y", "z"
        };

        private static readonly List<string> ListOfOtherCharacters = new List<string>
        {
            "&", "é", "\"", "'", "(", "[", "|", "è", "_", "\\", "-", "ç", "à", ")", "=", "~", "#", "{", "[", "|", "`", "^", "@", "]", "}", "¨", "$", "£", "¤", "%", "ù", "*", "µ", ",", ";", ":", "!", "?", ".", "/", "§", "€", "\t", "\b", "*", "+"
        };

        public static readonly HashSet<char> ListOfHexCharacters = new HashSet<char>
        {
            'a','b','c','d','e','f','0','1','2','3','4','5','6','7','8','9'
        };

        private static readonly HashSet<char> ListOfBase64Characters = new HashSet<char>
        {
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
            'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
            '0','1','2','3','4','5','6','7','8','9','+','/','='
        };

        private static readonly List<string> ListOfNumbers = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        // Hybrid mode - global secure RNG to avoid allocating per-call RNG objects.
        private static readonly RandomNumberGenerator SecureRng = RandomNumberGenerator.Create();

        #endregion

        #region Static functions about SHA hash

        /// <summary>
        /// Generate a SHA 512 hash from a string.
        /// </summary>
        public static string GenerateSha3512FromString(string sourceString)
        {
            using (var hash = new ClassSha3512DigestDisposable())
            {
                hash.Compute(sourceString.GetByteArray(), out byte[] hashedInputBytes);

                var hashedInputStringBuilder = new StringBuilder(BlockchainSetting.BlockchainSha512HexStringLength);

                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                string hashToReturn = hashedInputStringBuilder.ToString();
                // no need to Clear before returning
                return hashToReturn;
            }
        }

        /// <summary>
        /// Generate a SHA 256 hash from a string.
        /// </summary>
        public static string GenerateSha256FromString(string sourceString)
        {
            using (var hash = SHA256.Create())
            {
                byte[] hashedInputBytes = hash.ComputeHash(sourceString.GetByteArray());

                var hashedInputStringBuilder = new StringBuilder(BlockchainSetting.BlockchainSha256HexStringLength);

                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                return hashedInputStringBuilder.ToString();
            }
        }

        /// <summary>
        /// Generate a SHA 512 byte array from a byte array.
        /// </summary>
        public static byte[] GenerateSha512ByteArrayFromByteArray(byte[] source)
        {
            using (var hash = new ClassSha3512DigestDisposable())
                return hash.Compute(source);
        }

        #endregion

        #region Static functions about Random Secure RNG.

        /// <summary>
        /// Generate a random long number object between a range selected.
        /// Uses a static RandomNumberGenerator to avoid allocations.
        /// </summary>
        public static long GetRandomBetweenLong(long minimumValue, long maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentException("minimumValue must be <= maximumValue");

            ulong range = (ulong)(maximumValue - minimumValue + 1);
            // generate 8 random bytes
            var buffer = new byte[8];
            SecureRng.GetBytes(buffer);
            ulong rand = BitConverter.ToUInt64(buffer, 0);
            long result = (long)(minimumValue + (long)(rand % range));
            return result;
        }

        /// <summary>
        /// Generate a random BigInteger between a double range (kept signature for compatibility).
        /// </summary>
        public static BigInteger GetRandomBetweenBigInteger(double minimumValue, double maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentException("minimumValue must be <= maximumValue");

            // map to long range to be safe and fast
            long min = (long)Math.Floor(minimumValue);
            long max = (long)Math.Ceiling(maximumValue);
            long r = GetRandomBetweenLong(min, max);
            return new BigInteger(r);
        }

        /// <summary>
        /// Generate a random int number object between a range selected.
        /// Uses RandomNumberGenerator.GetInt32 when available.
        /// </summary>
        public static int GetRandomBetweenInt(int minimumValue, int maximumValue)
        {
            if (minimumValue > maximumValue)
                throw new ArgumentException("minimumValue must be <= maximumValue");

            // RandomNumberGenerator.GetInt32 is available in modern frameworks and is efficient.
            try
            {
#if NETSTANDARD2_0 || NETFRAMEWORK
                // Fallback for older frameworks
                var buf = new byte[4];
                SecureRng.GetBytes(buf);
                uint val = BitConverter.ToUInt32(buf, 0);
                return (int)(minimumValue + (int)(val % (uint)(maximumValue - minimumValue + 1)));
#else
                return RandomNumberGenerator.GetInt32(minimumValue, maximumValue + 1);
#endif
            }
            catch
            {
                // graceful fallback
                var buf = new byte[4];
                SecureRng.GetBytes(buf);
                uint val = BitConverter.ToUInt32(buf, 0);
                return (int)(minimumValue + (int)(val % (uint)(maximumValue - minimumValue + 1)));
            }
        }

        #endregion

        #region Static functions about content format. 

        /// <summary>
        /// Check if the string use only lowercase.
        /// </summary>
        public static bool CheckStringUseLowercaseOnly(string value)
        {
            return value.All(t => !char.IsUpper(t));
        }


        private static readonly uint[] Lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }

            return result;
        }

        /// <summary>
        /// Convert a byte array to hex string.
        /// </summary>
        public static string GetHexStringFromByteArray(byte[] bytes)
        {
            var lookup32 = Lookup32;
            var result = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }

            return new string(result, 0, result.Length);
        }

        /// <summary>
        /// Return a long value from a hex string.
        /// </summary>
        public static long GetLongFromHexString(string hexContent)
        {
            return Convert.ToInt64(hexContent, 16);
        }

        /// <summary>
        /// Remove decimal point from decimal value.
        /// </summary>
        public static decimal RemoveDecimalPoint(decimal value)
        {
            string stringValue = value.ToString(CultureInfo.InvariantCulture);

            if (stringValue.Contains("."))
            {
                string[] stringValueArray = stringValue.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (!decimal.TryParse(stringValueArray[0], out value))
                    value = 0;
            }
            else if (stringValue.Contains(","))
            {
                string[] stringValueArray = stringValue.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (!decimal.TryParse(stringValueArray[0], out value))
                    value = 0;
            }

            return value;
        }

        /// <summary>
        /// Convert a hex string into byte array. Uses Convert.FromHexString when available.
        /// </summary>
        public static byte[] GetByteArrayFromHexString(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return null;

            try
            {
#if NET5_0_OR_GREATER
                return Convert.FromHexString(hex);
#else
                byte[] ret = new byte[hex.Length / 2];
                for (int i = 0; i < ret.Length; i++)
                {
                    int high = hex[i * 2];
                    int low = hex[i * 2 + 1];
                    high = (high & 0xf) + ((high & 0x40) >> 6) * 9;
                    low = (low & 0xf) + ((low & 0x40) >> 6) * 9;

                    ret[i] = (byte)((high << 4) | low);
                }

                return ret;
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if the hex string contain only hex characters.
        /// </summary>
        public static bool CheckHexStringFormat(string hexString)
        {
            if (hexString.IsNullOrEmpty(false, out _))
                return false;

            foreach (var c in hexString)
            {
                if (!ListOfHexCharacters.Contains(char.ToLowerInvariant(c)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Remove every characters non-hex from a string.
        /// </summary>
        public static string RemoveNoHexCharacterFromString(string hexString)
        {
            if (hexString.IsNullOrEmpty(false, out _))
                return string.Empty;

            var sb = new StringBuilder(hexString.Length);
            foreach (var hexCharacter in hexString.ToLowerInvariant())
            {
                if (ListOfHexCharacters.Contains(hexCharacter))
                    sb.Append(hexCharacter);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Check if the string is a base64 string.
        /// </summary>
        public static bool CheckBase64String(string base64String)
        {
            return (base64String.Length % 4 == 0) && Regex.IsMatch(base64String.Trim(), Base64Regex, RegexOptions.Compiled);
        }

        public static bool CharIsABase64Character(char base64Character)
        {
            return ListOfBase64Characters.Contains(base64Character);
        }

        #endregion

        #region Static functions about random word.

        /// <summary>
        /// Random word.
        /// </summary>
        public static string GetRandomWord(int lengthTarget)
        {
            if (lengthTarget <= 0)
                return string.Empty;

            var sb = new StringBuilder(lengthTarget);
            var buffer = new byte[4];

            for (int i = 0; i < lengthTarget; i++)
            {
                SecureRng.GetBytes(buffer);
                int selector = BitConverter.ToInt32(buffer, 0);
                selector = Math.Abs(selector);

                int percent1 = selector % 100 + 1;

                if (percent1 <= 34) // numbers ~34%
                {
                    sb.Append(ListOfNumbers[selector % ListOfNumbers.Count]);
                    continue;
                }

                if (percent1 <= 67) // letters ~33%
                {
                    bool upper = (selector & 1) == 0;
                    var ch = ListOfCharacters[selector % ListOfCharacters.Count];
                    sb.Append(upper ? ch.ToUpperInvariant() : ch);
                    continue;
                }

                // else other characters
                sb.Append(ListOfOtherCharacters[selector % ListOfOtherCharacters.Count]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Random word into byte array object.
        /// </summary>
        public static byte[] GetRandomByteArrayWord(int lengthTarget)
        {
            var s = GetRandomWord(lengthTarget);
            return Encoding.UTF8.GetBytes(s);
        }

        #endregion

        #region Static functions about timestamp.

        public static long GetCurrentTimestampInSecond()
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public static long GetCurrentTimestampInMillisecond()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public static bool CheckPacketTimestamp(long timestampPacket, int maxDelay, int earlierDelay)
        {
            long currentTimestamp = GetCurrentTimestampInSecond();
            return timestampPacket + maxDelay >= currentTimestamp && (timestampPacket + maxDelay - currentTimestamp) <= earlierDelay ? true : false;
        }

        public static DateTime GetDatetimeFromTimestamp(long unixTimeStamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).ToLocalTime().DateTime;
        }

        #endregion

        #region Static functions about path.

        public static string ConvertPath(string path)
        {
            return Environment.OSVersion.Platform == PlatformID.Unix ? path.Replace("\\", "/") : path;
        }

        #endregion

        #region Static functions about the Blockchain.

        public static void InsertBlockchainVersionToByteArray(byte[] baseByteArray, out string result)
        {
            result = BlockchainSetting.BlockchainVersion + GetHexStringFromByteArray(baseByteArray);
        }

        #endregion

        #region Static function about Serialization/Deserialization.

        public static bool TryDeserialize<T>(string content, out T result, ObjectCreationHandling handling = ObjectCreationHandling.Auto)
        {
            result = default;

            if (content.IsNullOrEmpty(true, out string contentTrimmed) || contentTrimmed.Length == 0 || contentTrimmed.Contains("�"))
                return false;

            try
            {
                // single pass deserialize - avoids double parsing
                result = JsonConvert.DeserializeObject<T>(contentTrimmed, new JsonSerializerSettings() { ObjectCreationHandling = handling });
                return result != null;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        public static string SerializeData<T>(T content, Formatting formatting = Formatting.None)
        {
            return content != null ? JsonConvert.SerializeObject(content, formatting) : string.Empty;
        }

        #endregion

        #region Static functions about Compress/Decompress.

        public static byte[] WrapDataLz4(byte[] data)
        {
            return LZ4Codec.Wrap(data, 0, data.Length);
        }

        public static byte[] UnWrapDataLz4(byte[] compressed)
        {
            return LZ4Codec.Unwrap(compressed);
        }

        public static byte[] CompressLz4(byte[] data)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (LZ4Stream lz4 = new LZ4Stream(memory, LZ4StreamMode.Compress, LZ4StreamFlags.HighCompression))
                    lz4.Write(data, 0, data.Length);

                return memory.ToArray();
            }
        }

        public static byte[] DecompressLz4(byte[] data)
        {
            using (MemoryStream memory = new MemoryStream(data))
            using (MemoryStream memoryResult = new MemoryStream())
            using (LZ4Stream lz4 = new LZ4Stream(memory, LZ4StreamMode.Decompress, LZ4StreamFlags.HighCompression))
            {
                lz4.CopyTo(memoryResult);
                return memoryResult.ToArray();
            }
        }

        private static readonly Dictionary<string, string> ListHexCombinaison = new Dictionary<string, string>()
        {
            { "00", "g" },{ "01", "h" },{ "02", "j" },{ "03", "k" },{ "04", "l" },{ "05", "m" },{ "06", "w" },{ "07", "z" },{ "08", "t" },{ "09", "u" },{ "10", "i" },{ "11", "p" },{ "12", "x" },{ "13", "n" },{ "14", "!" },{ "15", "~" },{ "16", "&" },{ "17", "'" },{ "18", "(" },{ "19", "[" },{ "20", "-" },{ "21", "|" },{ "22", "è" },{ "23", "`" },{ "24", "_" },{ "25", "ç" },{ "26", "^" },{ "27", "à" },{ "28", "@" },{ "29", ")" },{ "30", "]" },{ "31", "°" },{ "32", "+" },{ "33", "=" },{ "34", "}" }
        };

        private static readonly Dictionary<char, string> ListHexCombinaisonReverted = new Dictionary<char, string>()
        {
            { 'g',  "00" },{ 'h',  "01" },{ 'j',  "02" },{ 'k',  "03" },{ 'l',  "04" },{ 'm',  "05" },{ 'w',  "06" },{ 'z',  "07" },{ 't',  "08" },{ 'u',  "09" },{ 'i',  "10" },{ 'p',  "11" },{ 'x',  "12" },{ 'n',  "13" },{ '!',  "14" },{ '~',  "15" },{ '&',  "16" },{ '\'',  "17" },{ '(',  "18" },{ '[',  "19" },{ '-',  "20" },{ '|',  "21" },{ 'è',  "22" },{ '`',  "23" },{ '_',  "24" },{ 'ç',  "25" },{ '^',  "26" },{ 'à',  "27" },{ '@',  "28" },{ ')',  "29" },{ ']',  "30" },{ '°',  "31" },{ '+',  "32" },{ '=',  "33" },{ '}',  "34" }
        };

        public static string CompressHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return string.Empty;

            var sb = new StringBuilder(hexString.Length / 2);

            for (int i = 0; i < hexString.Length / 2; i++)
            {
                string hexCombinaison = hexString.Substring(i * 2, 2);

                if (ListHexCombinaison.TryGetValue(hexCombinaison, out var replaced))
                    sb.Append(replaced);
                else
                    sb.Append(hexCombinaison);
            }

            return sb.ToString();
        }

        public static string DecompressHexString(string compressedHexString)
        {
            if (string.IsNullOrEmpty(compressedHexString))
                return string.Empty;

            var sb = new StringBuilder(compressedHexString.Length * 2);

            foreach (char character in compressedHexString)
            {
                if (ListHexCombinaisonReverted.TryGetValue(character, out var hex))
                    sb.Append(hex);
                else
                    sb.Append(character);
            }

            return sb.ToString();
        }

        #endregion

        #region Static functions about GC Collector

        public static void CleanGc()
        {
            GC.Collect(0, GCCollectionMode.Optimized, false, false);
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public static long GetMemoryAllocationFromProcess()
        {
            return Process.GetCurrentProcess().WorkingSet64;
        }

        #endregion

        #region Static functions about Socket/TCPClient/Packets.

        public static bool TcpClientIsConnected(TcpClient tcpClient)
        {
            try
            {
                if (tcpClient == null || tcpClient.Client == null || !tcpClient.Connected)
                    return false;

                return !((tcpClient.Client.Poll(10, SelectMode.SelectRead) && (tcpClient.Client.Available == 0)));
            }
            catch
            {
                return false;
            }
        }

        public static bool SocketIsConnected(Socket socket)
        {
            try
            {
                if (socket == null)
                    return false;

                if (!socket.Connected)
                    return false;

                return !((socket.Poll(10, SelectMode.SelectRead) && (socket.Available == 0)));
            }
            catch
            {
                return false;
            }
        }

        public static void CloseTcpClient(TcpClient tcpClient)
        {
            try
            {
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                    {
                        try
                        {
                            tcpClient.Client.Shutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            tcpClient?.Close();
                            tcpClient?.Dispose();
                        }
                    }
                    else
                    {
                        tcpClient?.Close();
                        tcpClient?.Dispose();
                    }
                }
            }
            catch
            {
            }
        }

        public static DisposableList<ClassReadPacketSplitted> GetEachPacketSplitted(byte[] packetBufferOnReceive, DisposableList<ClassReadPacketSplitted> listPacketReceived, CancellationTokenSource cancellation)
        {
            if (packetBufferOnReceive == null || packetBufferOnReceive.Length == 0)
                return listPacketReceived;

            string packetData = Encoding.UTF8.GetString(packetBufferOnReceive).Replace("\0", "");

            if (listPacketReceived == null || listPacketReceived.Disposed)
                listPacketReceived = new DisposableList<ClassReadPacketSplitted>();

            if (listPacketReceived.Count == 0)
                listPacketReceived.Add(new ClassReadPacketSplitted());

            if (packetData.Contains(ClassPeerPacketSetting.PacketPeerSplitSeperator))
            {
                int countSeperator = packetData.Count(x => x == ClassPeerPacketSetting.PacketPeerSplitSeperator);
                int completed = 0;
                int start = 0;

                for (int i = 0; i < packetData.Length; i++)
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    if (packetData[i] == ClassPeerPacketSetting.PacketPeerSplitSeperator)
                    {
                        int len = i - start;
                        if (len > 0)
                        {
                            string part = packetData.Substring(start, len);
                            var lastIndex = Math.Max(0, listPacketReceived.Count - 1);
                            listPacketReceived[lastIndex].Packet += part;
                        }

                        // mark complete and prepare new packet
                        var last = listPacketReceived[listPacketReceived.Count - 1];
                        last.Complete = true;
                        listPacketReceived.Add(new ClassReadPacketSplitted());

                        start = i + 1;
                        completed++;
                    }
                }

                // remainder
                if (start < packetData.Length)
                {
                    string part = packetData.Substring(start);
                    var lastIndex = Math.Max(0, listPacketReceived.Count - 1);
                    listPacketReceived[lastIndex].Packet += part;
                }
            }
            else
            {
                int index = Math.Max(0, listPacketReceived.Count - 1);
                listPacketReceived[index].Packet += packetData;
            }

            return listPacketReceived;
        }

        public static byte[] DoPadding(byte[] data)
        {
            int basePaddingSize = 16;
            int paddingSizeRequired = basePaddingSize - (data.Length % basePaddingSize);

            while (paddingSizeRequired == 0)
            {
                basePaddingSize++;
                paddingSizeRequired = basePaddingSize - (data.Length % basePaddingSize);
            }

            byte[] paddedBytes = new byte[data.Length + paddingSizeRequired];
            Array.Copy(data, 0, paddedBytes, 0, data.Length);

            for (int i = 0; i < paddingSizeRequired; i++)
                paddedBytes[data.Length + i] = (byte)paddingSizeRequired;

            return paddedBytes;
        }

        public static byte[] UndoPadding(byte[] paddedData)
        {
            if (paddedData == null || paddedData.Length == 0)
                return null;

            int size = paddedData.Length - paddedData[paddedData.Length - 1];

            if (size > 0)
            {
                byte[] packet = new byte[size];
                Array.Copy(paddedData, 0, packet, 0, packet.Length);
                return packet;
            }

            return null;
        }

        public static AddressFamily GetAddressFamily(string ip)
        {
            if (IPAddress.TryParse(ip, out IPAddress address))
                return address.AddressFamily == AddressFamily.InterNetworkV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

            return AddressFamily.Unknown;
        }

        #endregion

        #region Static functions about locking object.

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static T LockReturnObject<T>(T objectData)
        {
            bool locked = false;
            T returnData = default;

            try
            {
                if (!Monitor.IsEntered(objectData))
                    Monitor.TryEnter(objectData, ref locked);
                else
                    locked = true;

                if (locked)
                    returnData = objectData;

            }
            finally
            {
                if (locked)
                {
                    if (Monitor.IsEntered(objectData))
                        Monitor.Exit(objectData);
                }
            }

            return returnData;
        }

        #endregion

        #region Others static functions.

        public static int GetMaxAvailableProcessorCount()
        {
            return Environment.ProcessorCount;
        }

        private static readonly BigInteger KhStart = 1000;
        private static readonly BigInteger MhStart = BigInteger.Multiply(KhStart, 100);
        private static readonly BigInteger ThStart = BigInteger.Multiply(MhStart, 100);
        private static readonly BigInteger PhStart = BigInteger.Multiply(ThStart, 100);
        private static readonly BigInteger EhStart = BigInteger.Multiply(PhStart, 100);

        public static string GetFormattedHashrate(BigInteger hashrate)
        {
            if (hashrate < KhStart)
                return hashrate + " H/s";

            if (hashrate >= KhStart && hashrate < MhStart)
                return ((double)hashrate / (double)KhStart).ToString("N2", CultureInfo.InvariantCulture) + " KH/s";

            if (hashrate >= MhStart && hashrate < ThStart)
                return ((double)hashrate / (double)MhStart).ToString("N2", CultureInfo.InvariantCulture) + " MH/s";

            if (hashrate >= ThStart && hashrate < PhStart)
                return ((double)hashrate / (double)ThStart).ToString("N2", CultureInfo.InvariantCulture) + " TH/s";

            if (hashrate >= PhStart && hashrate < EhStart)
                return ((double)hashrate / (double)PhStart).ToString("N2", CultureInfo.InvariantCulture) + " PH/s";

            return ((double)hashrate / (double)EhStart).ToString("N2", CultureInfo.InvariantCulture) + " EH/s";
        }

        public static string ConvertBytesToMegabytes(long bytesCount)
        {
            return bytesCount > 0 ? Math.Round((((double)bytesCount / 1024) / 1024), 2) + " MB(s)" : "0 MB(s)";
        }

        #endregion
    }

    #region Extensions class's of generic objects.

    public static class ClassUtilityStringExtension
    {
        public static byte[] GetHexStringToByteArray(this string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

        private static byte[] GetByteArrayUtf8(this string content)
        {
            return new UTF8Encoding(true, true).GetBytes(content);
        }

        private static byte[] GetByteArrayAscii(this string content)
        {
            return Encoding.ASCII.GetBytes(content);
        }

        public static byte[] GetByteArray(this string src, bool ascii = false)
        {
            return ascii ? Encoding.ASCII.GetBytes(src) : Encoding.UTF8.GetBytes(src);
        }

        public static string CopyBase58String(this string base58String, bool isWalletAddress)
        {
            var sb = new StringBuilder(base58String.Length);
            bool isValid = false;

            foreach (var character in base58String)
            {
                if (ClassBase58.CharacterIsInsideBase58CharacterList(character))
                    sb.Append(character);

                if (isWalletAddress && sb.Length >= BlockchainSetting.WalletAddressWifLengthMin
                    && sb.Length <= BlockchainSetting.WalletAddressWifLengthMax)
                {
                    if (ClassBase58.DecodeWithCheckSum(sb.ToString(), true) != null)
                    {
                        isValid = true;
                        break;
                    }
                }
            }

            if (isValid)
                return sb.ToString();

            if (!isWalletAddress && ClassBase58.DecodeWithCheckSum(sb.ToString(), false) != null)
                return sb.ToString();

            return string.Empty;
        }

        public static string DeepCopy(this string srcString)
        {
            return new string(srcString.ToCharArray());
        }

        public static unsafe void Clear(this string s)
        {
            if (s != null)
            {
                fixed (char* ptr = s)
                {
                    for (int i = 0; i < s.Length; i++)
                        ptr[i] = '\0';
                }
            }
        }

        public static DisposableList<string> DisposableSplit(this string src, string seperatorStr, int countGetLimit = 0)
        {
            DisposableList<string> listSplitted = new DisposableList<string>();

            if (src.IsNullOrEmpty(true, out string srcTrimmed) || srcTrimmed.Length == 0)
                return listSplitted;

            foreach (var word in srcTrimmed.Split(new[] { seperatorStr }, StringSplitOptions.None))
            {
                if (!word.IsNullOrEmpty(true, out string wordTrimmed))
                    listSplitted.Add(wordTrimmed);
            }

            return listSplitted;
        }

        public static bool IsNullOrEmpty(this string str, bool useTrim, out string strTrimmed)
        {
            strTrimmed = null;

            if (str == null) return true;
            if (str.Length == 0) return true;
            if (str == string.Empty) return true;
            if (str == "") return true;

            strTrimmed = useTrim ? str.TrimFast() : null;

            if (useTrim)
            {
                if (strTrimmed.Length == 0) return true;
                if (string.IsNullOrEmpty(strTrimmed)) return true;
            }

            return false;
        }

        public static string TrimFast(this string str)
        {
            int startIndex = 0;
            int endIndex = str.Length - 1;

            while (startIndex < str.Length && char.IsWhiteSpace(str[startIndex]))
                startIndex += 1;

            while (endIndex >= 0 && char.IsWhiteSpace(str[endIndex]))
                endIndex -= 1;

            endIndex += 1;

            if (startIndex >= endIndex) return string.Empty;

            return str.Substring(startIndex, (endIndex - startIndex));
        }

        public static string GetStringBetweenTwoStrings(this string str, string firstString, string lastString)
        {
            int pos1 = str.IndexOf(firstString, 0, StringComparison.Ordinal) + firstString.Length;
            int pos2 = str.IndexOf(lastString, 0, StringComparison.Ordinal);

            return str.Substring(pos1, pos2 - pos1);
        }
    }

    public static class ClassUtilityByteArrayExtension
    {
        /// <summary>
        /// Get hex string from byte array.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string GetHexStringFromByteArray(this byte[] content)
        {
#if NET5_0_OR_GREATER
            return Convert.ToHexString(content);
#else
            return BitConverter.ToString(content).Replace("-", "");
#endif
        }

        public static string GetStringFromByteArrayAscii(this byte[] content)
        {
            return content != null && content.Length > 0 ? Encoding.ASCII.GetString(content) : null;
        }

        public static string GetStringFromByteArrayUtf8(this byte[] content)
        {
            return content != null && content.Length > 0 ? Encoding.UTF8.GetString(content) : null;
        }

        public static bool CompareArray(this byte[] source, byte[] compare)
        {
            if (source != null)
                return compare != null && source.Length == compare.Length && !source.Where((t, i) => t != compare[i]).Any();

            return compare == null;
        }
    }

    public static class ClassUtilityNetworkStreamExtension
    {
        public static async Task<bool> TrySendSplittedPacket(this NetworkStream networkStream, byte[] packetBytesToSend, CancellationTokenSource cancellation, int packetMaxSize, bool singleWrite = false)
        {
            try
            {
                if (networkStream == null || !networkStream.CanWrite || packetBytesToSend == null)
                    return false;

                if (packetBytesToSend.Length >= packetMaxSize && !singleWrite)
                {
                    int countPacketSendLength = 0;

                    while (countPacketSendLength < packetBytesToSend.Length && !cancellation.IsCancellationRequested)
                    {
                        int remaining = packetBytesToSend.Length - countPacketSendLength;
                        int packetSize = remaining > packetMaxSize ? packetMaxSize : remaining;

                        if (packetSize <= 0)
                            break;

                        await networkStream.WriteAsync(packetBytesToSend, countPacketSendLength, packetSize, cancellation.Token).ConfigureAwait(false);

                        countPacketSendLength += packetSize;
                    }

                    if (!cancellation.IsCancellationRequested)
                        await networkStream.FlushAsync(cancellation.Token).ConfigureAwait(false);
                }
                else
                {
                    await networkStream.WriteAsync(packetBytesToSend, 0, packetBytesToSend.Length, cancellation.Token).ConfigureAwait(false);
                    await networkStream.FlushAsync(cancellation.Token).ConfigureAwait(false);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }

    public static class ClassUtilitySemaphoreExtension
    {
        public static bool TryWait(this SemaphoreSlim semaphore, CancellationTokenSource cancellation)
        {
            try
            {
                semaphore.Wait(cancellation.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryWaitWithDelay(this SemaphoreSlim semaphore, int delay, CancellationTokenSource cancellation)
        {
            try
            {
                return semaphore.Wait(delay, cancellation.Token);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> TryWaitAsync(this SemaphoreSlim semaphore, CancellationTokenSource cancellation)
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    if (await semaphore.WaitAsync(1000, cancellation.Token).ConfigureAwait(false))
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public static async Task<bool> TryWaitAsync(this SemaphoreSlim semaphore, int delay, CancellationTokenSource cancellation)
        {
            try
            {
                return await semaphore.WaitAsync(delay, cancellation.Token).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> TryWaitExecuteActionAsync(this SemaphoreSlim semaphore, Action action, CancellationTokenSource cancellation)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = await semaphore.TryWaitAsync(cancellation).ConfigureAwait(false);

                if (!useSemaphore)
                    return false;

                try
                {
                    action.Invoke();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            finally
            {
                if (useSemaphore)
                    semaphore.Release();
            }
        }

        public static bool TryWaitExecuteAction(this SemaphoreSlim semaphore, Action action, CancellationTokenSource cancellation)
        {
            bool useSemaphore = false;

            try
            {
                useSemaphore = semaphore.TryWait(cancellation);

                if (!useSemaphore)
                    return false;
                try
                {
                    action.Invoke();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            finally
            {
                if (useSemaphore)
                    semaphore.Release();
            }
        }
    }
    #endregion
}
