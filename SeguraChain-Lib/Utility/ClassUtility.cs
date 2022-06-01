using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LZ4;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeguraChain_Lib.Blockchain.Setting;
using SeguraChain_Lib.Instance.Node.Network.Enum.P2P.Packet;
using SeguraChain_Lib.Instance.Node.Network.Services.P2P.Sync.Packet.Model;
using SeguraChain_Lib.Other.Object.List;
using SeguraChain_Lib.Other.Object.SHA3;

namespace SeguraChain_Lib.Utility
{
    public class ClassUtility
    {
        #region Constant objects.

        private const string Base64Regex = @"^[a-zA-Z0-9\+/]*={0,3}$";

        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(true, false);

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
            'a',
            'b',
            'c',
            'd',
            'e',
            'f',
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9'
        };

        private static readonly HashSet<char> ListOfBase64Characters = new HashSet<char>
        {
            'A',
            'B',
            'C',
            'D',
            'E',
            'F',
            'G',
            'H',
            'I',
            'J',
            'K',
            'L',
            'M',
            'N',
            'O',
            'P',
            'Q',
            'R',
            'S',
            'T',
            'U',
            'V',
            'W',
            'X',
            'Y',
            'Z',
            'a',
            'b',
            'c',
            'd',
            'e',
            'f',
            'g',
            'h',
            'i',
            'j',
            'k',
            'l',
            'm',
            'n',
            'o',
            'p',
            'q',
            'r',
            's',
            't',
            'u',
            'v',
            'w',
            'x',
            'y',
            'z',
            '0',
            '1',
            '2',
            '3',
            '4',
            '5',
            '6',
            '7',
            '8',
            '9',
            '+',
            '/',
            '='
        };

        private static readonly List<string> ListOfNumbers = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        #endregion

        #region Static functions about SHA hash

        /// <summary>
        /// Generate a SHA 512 hash from a string.
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static string GenerateSha3512FromString(string sourceString)
        {
            using (var hash = new ClassSha3512DigestDisposable())
            {

                hash.Compute(GetByteArrayFromStringUtf8(sourceString), out byte[] hashedInputBytes);

                var hashedInputStringBuilder = new StringBuilder(BlockchainSetting.BlockchainSha512HexStringLength);

                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                string hashToReturn = hashedInputStringBuilder.ToString();

                hashedInputStringBuilder.Clear();

                return hashToReturn;
            }
        }

        /// <summary>
        /// Generate a SHA 256 hash from a string.
        /// </summary>
        /// <param name="sourceString"></param>
        /// <returns></returns>
        public static string GenerateSha256FromString(string sourceString)
        {
            using (var hash = SHA256.Create())
            {

                byte[] hashedInputBytes = hash.ComputeHash(GetByteArrayFromStringUtf8(sourceString));

                var hashedInputStringBuilder = new StringBuilder(BlockchainSetting.BlockchainSha256HexStringLength);

                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));

                string hashToReturn = hashedInputStringBuilder.ToString();

                hashedInputStringBuilder.Clear();

                return hashToReturn;
            }
        }


        /// <summary>
        /// Generate a SHA 512 byte array from a byte array.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static byte[] GenerateSha512ByteArrayFromByteArray(byte[] source)
        {
            using (var hash = new ClassSha3512DigestDisposable())
                return hash.Compute(source);
        }

        #endregion

        #region Static functions about Random Secure RNG.

        /// <summary>
        ///  Generate a random long number object between a range selected.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static long GetRandomBetweenLong(long minimumValue, long maximumValue)
        {
            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(long)];

                generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (long)(minimumValue + randomValueInRange);
            }
        }

        /// <summary>
        ///  Generate a random long number object between a range selected.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static BigInteger GetRandomBetweenBigInteger(double minimumValue, double maximumValue)
        {
            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(double)];

                generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return new BigInteger(minimumValue + randomValueInRange);
            }
        }

        /// <summary>
        ///  Generate a random int number object between a range selected.
        /// </summary>
        /// <param name="minimumValue"></param>
        /// <param name="maximumValue"></param>
        /// <returns></returns>
        public static int GetRandomBetweenInt(int minimumValue, int maximumValue)
        {
            using (RNGCryptoServiceProvider generator = new RNGCryptoServiceProvider())
            {
                var randomNumber = new byte[sizeof(int)];

                generator.GetBytes(randomNumber);

                var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

                var multiplier = Math.Max(0, asciiValueOfRandomCharacter / 255d - 0.00000000001d);

                var range = maximumValue - minimumValue + 1;

                var randomValueInRange = Math.Floor(multiplier * range);

                return (int)(minimumValue + randomValueInRange);
            }
        }

        #endregion

        #region Static functions about content format. 

        /// <summary>
        /// Check if the string use only lowercase.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool CheckStringUseLowercaseOnly(string value)
        {
            return value.All(t => !char.IsUpper(t));
        }

        /// <summary>
        /// Convert a string into a byte array object.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static byte[] GetByteArrayFromStringAscii(string content)
        {
            return Encoding.ASCII.GetBytes(content);
        }

        /// <summary>
        /// Convert a string into a byte array object.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static byte[] GetByteArrayFromStringUtf8(string content)
        {
            return Utf8Encoding.GetBytes(content);
        }

        private static readonly uint[] Lookup32 = CreateLookup32();

        /// <summary>
        /// Create a lookup conversation for accelerate byte array conversion into hex string.
        /// </summary>
        /// <returns></returns>
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
        /// <param name="bytes"></param>
        /// <returns></returns>
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
        /// <param name="hexContent"></param>
        /// <returns></returns>
        public static long GetLongFromHexString(string hexContent)
        {
            return Convert.ToInt64(hexContent, 16);
        }

        /// <summary>
        /// RemoveFromCache decimal point from decimal value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
        /// Convert a hex string into byte array.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] GetByteArrayFromHexString(string hex)
        {
            try
            {
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
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if the hex string contain only hex characters.
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static bool CheckHexStringFormat(string hexString)
        {
            if (hexString.IsNullOrEmpty(false, out _))
                return false;

            return hexString.ToLower().Count(character => ListOfHexCharacters.Contains(character)) == hexString.Length;
        }

        /// <summary>
        /// Remove every characters no-hex from a string.
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static string RemoveNoHexCharacterFromString(string hexString)
        {
            string newHexString = string.Empty;

            if (!hexString.IsNullOrEmpty(false, out _))
            {
                foreach (var hexCharacter in hexString.ToLower())
                {
                    if (ListOfHexCharacters.Contains(hexCharacter))
                        newHexString += hexCharacter;
                }
            }

            return newHexString;
        }

        /// <summary>
        /// Check if the string is a base64 string.
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        public static bool CheckBase64String(string base64String)
        {
            return (base64String.Length % 4 == 0) && Regex.IsMatch(base64String.Trim(), Base64Regex, RegexOptions.None);
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
        /// <param name="lengthTarget"></param>
        /// <returns></returns>
        public static string GetRandomWord(int lengthTarget)
        {
            string word = string.Empty;

            for (int i = 0; i < lengthTarget; i++)
            {
                var percent1 = GetRandomBetweenLong(1, 100); // Numbers.
                var percent2 = GetRandomBetweenLong(1, 100); // Letters.
                var percent3 = GetRandomBetweenLong(1, 100); // Special characters.

                if (percent1 >= percent2 && percent1 >= percent3) // Use numbers.
                    word += ListOfNumbers[GetRandomBetweenInt(0, ListOfNumbers.Count - 1)];
                else
                {
                    if (percent2 >= percent3) // Use letters.
                    {
                        percent1 = GetRandomBetweenLong(1, 100);
                        percent2 = GetRandomBetweenLong(1, 100);

                        if (percent2 >= percent1) // use Uppercase.
                            word += ListOfCharacters[GetRandomBetweenInt(0, ListOfCharacters.Count - 1)].ToUpper();
                        else // use normal lowercase.
                            word += ListOfCharacters[GetRandomBetweenInt(0, ListOfCharacters.Count - 1)];
                    }
                    else
                    {
                        if (percent3 >= percent1) // Use special characters.
                            word += ListOfOtherCharacters[GetRandomBetweenInt(0, ListOfOtherCharacters.Count - 1)];
                        else // Use numbers.
                            word += ListOfNumbers[GetRandomBetweenInt(0, ListOfNumbers.Count - 1)];
                    }
                }
            }

            return word;
        }

        /// <summary>
        /// Random word into byte array object.
        /// </summary>
        /// <param name="lengthTarget"></param>
        /// <returns></returns>
        public static byte[] GetRandomByteArrayWord(int lengthTarget)
        {
            string word = string.Empty;

            for (int i = 0; i < lengthTarget; i++)
            {
                var percent1 = GetRandomBetweenLong(1, 100);
                var percent2 = GetRandomBetweenLong(1, 100);
                if (percent1 >= percent2) // Use numbers.
                    word += ListOfNumbers[GetRandomBetweenInt(0, ListOfNumbers.Count - 1)];
                else // Use letters
                {
                    percent1 = GetRandomBetweenLong(1, 100);
                    percent2 = GetRandomBetweenLong(1, 100);
                    if (percent2 >= percent1) // use uppercase.
                        word += ListOfCharacters[GetRandomBetweenInt(0, ListOfCharacters.Count - 1)].ToUpper();
                    else // use normal lowercase.
                        word += ListOfCharacters[GetRandomBetweenInt(0, ListOfCharacters.Count - 1)];
                }
            }

            return GetByteArrayFromStringAscii(word);
        }

        #endregion

        #region Static functions about timestamp.

        /// <summary>
        /// Return the current timestamp in seconds.
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentTimestampInSecond()
        {
            return DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Return the current timestamp in millisecond.
        /// </summary>
        /// <returns></returns>
        public static long GetCurrentTimestampInMillisecond()
        {
            return DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Check if the timestamp is not too late and not too early.
        /// </summary>
        /// <param name="timestampPacket"></param>
        /// <param name="maxDelay"></param>
        /// <param name="earlierDelay"></param>
        /// <returns></returns>
        public static bool CheckPacketTimestamp(long timestampPacket, int maxDelay, int earlierDelay)
        {
            long currentTimestamp = GetCurrentTimestampInSecond();


            return timestampPacket + maxDelay >= currentTimestamp && (timestampPacket + maxDelay - currentTimestamp) <= earlierDelay ? true : false;
        }

        /// <summary>
        /// Convert a timestamp of seconds into a date.
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime GetDatetimeFromTimestamp(long unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        #endregion

        #region Static functions about path.

        /// <summary>
        /// Convert the path for Linux/Unix system.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertPath(string path)
        {
            return Environment.OSVersion.Platform == PlatformID.Unix ? path.Replace("\\", "/") : path;
        }

        #endregion

        #region Static functions about the Blockchain.

        /// <summary>
        /// Insert a byte array on the first index
        /// </summary>
        /// <param name="baseByteArray"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static void InsertBlockchainVersionToByteArray(byte[] baseByteArray, out string result)
        {
            result = BlockchainSetting.BlockchainVersion + GetHexStringFromByteArray(baseByteArray);
        }

        #endregion

        #region Static function about Serialization/Deserialization.

        /// <summary>
        /// Try to deserialize an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <param name="result"></param>
        /// <param name="handling"></param>
        public static bool TryDeserialize<T>(string content, out T result, ObjectCreationHandling handling = ObjectCreationHandling.Auto)
        {
            if (content.IsNullOrEmpty(true, out string contentTrimmed) || contentTrimmed.Length == 0 || 
                contentTrimmed.Contains("�"))
            {
                result = default;
                return false;
            }

            bool isNull = false;
            try
            {

                result = JObject.Parse(contentTrimmed).ToObject<T>();

                if (result == null)
                    isNull = true;
                else
                    return true;
            }
            catch
            {
#if DEBUG
                Debug.WriteLine("Failed to parse: " + contentTrimmed);
#endif
            }

            if (isNull)
            {

                try
                {
                    result = JsonConvert.DeserializeObject<T>(contentTrimmed, new JsonSerializerSettings() { ObjectCreationHandling = handling });
                    return true;
                }
                catch
                {
                    // Ignored.
                }
            }



            result = default;
            return false;
        }

        /// <summary>
        /// Serialize data as string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <param name="formatting"></param>
        /// <returns></returns>
        public static string SerializeData<T>(T content, Formatting formatting = Formatting.None)
        {
            return content != null ? JsonConvert.SerializeObject(content, formatting) : string.Empty;
        }


        #endregion

        #region Static functions about Compress/Decompress.

        public static byte[] CompressDataLz4(byte[] data)
        {
            return LZ4Codec.Wrap(data, 0, data.Length);
        }

        public static byte[] DecompressDataLz4(byte[] compressed)
        {
            return LZ4Codec.Unwrap(compressed);
        }

        private static Dictionary<string, string> ListHexCombinaison = new Dictionary<string, string>()
        {
            { "00", "g" },
            { "01", "h" },
            { "02", "j" },
            { "03", "k" },
            { "04", "l" },
            { "05", "m" },
            { "06", "w" },
            { "07", "z" },
            { "08", "t" },
            { "09", "u" },
            { "10", "i" },
            { "11", "p" },
            { "12", "x" },
            { "13", "n" },
            { "14", "!" },
            { "15", "~" },
            { "16", "&" },
            { "17", "'" },
            { "18", "(" },
            { "19", "[" },
            { "20", "-" },
            { "21", "|" },
            { "22", "è" },
            { "23", "`" },
            { "24", "_" },
            { "25", "ç" },
            { "26", "^" },
            { "27", "à" },
            { "28", "@" },
            { "29", ")" },
            { "30", "]" },
            { "31", "°" },
            { "32", "+" },
            { "33", "=" },
            { "34", "}" },
        };

        private static Dictionary<char, string> ListHexCombinaisonReverted = new Dictionary<char, string>()
        {
            { 'g',  "00" },
            { 'h',  "01" },
            { 'j',  "02" },
            { 'k',  "03" },
            { 'l',  "04" },
            { 'm',  "05" },
            { 'w',  "06" },
            { 'z',  "07" },
            { 't',  "08" },
            { 'u',  "09" },
            { 'i',  "10" },
            { 'p',  "11" },
            { 'x',  "12" },
            { 'n',  "13" },
            { '!',  "14" },
            { '~',  "15" },
            { '&',  "16" },
            { '\'',  "17" },
            { '(',  "18" },
            { '[',  "19" },
            { '-',  "20" },
            { '|',  "21" },
            { 'è',  "22" },
            { '`',  "23" },
            { '_',  "24" },
            { 'ç',  "25" },
            { '^',  "26" },
            { 'à',  "27" },
            { '@',  "28" },
            { ')',  "29" },
            { ']',  "30" },
            { '°',  "31" },
            { '+',  "32" },
            { '=',  "33" },
            { '}',  "34" }
        };

        /// <summary>
        /// Compress hex string into a short hex string replaced by invalid hex characters.
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static string CompressHexString(string hexString)
        {
            string compressedHexString = string.Empty;

            for (int i = 0; i < hexString.Length / 2; i++)
            {
                string hexCombinaison = hexString.Substring(i * 2, 2);

                if (ListHexCombinaison.ContainsKey(hexCombinaison))
                    compressedHexString += ListHexCombinaison[hexCombinaison];
                else compressedHexString += hexCombinaison;
            }

            return compressedHexString;
        }

        /// <summary>
        /// Decompress hex string, replace invalid characters used by the compression into good hex combinaisons.
        /// </summary>
        /// <param name="compressedHexString"></param>
        /// <returns></returns>
        public static string DecompressHexString(string compressedHexString)
        {
            string decompressedHexString = string.Empty;

            foreach(char character in compressedHexString)
            {
                if (ListHexCombinaisonReverted.ContainsKey(character))
                    decompressedHexString += ListHexCombinaisonReverted[character];
                else decompressedHexString += character;
            }
           
            return decompressedHexString;
        }

        #endregion

        #region Static functions about GC Collector

        /// <summary>
        /// Clean Garbage collector if it's possible.
        /// </summary>
        public static void CleanGc()
        {
            GC.Collect(0, GCCollectionMode.Forced, false, true);
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Return the amount of ram allocated from the process.
        /// </summary>
        /// <returns></returns>
        public static long GetMemoryAllocationFromProcess()
        {
            return Process.GetCurrentProcess().WorkingSet64;
        }

        #endregion

        #region Static functions about Socket/TCPClient/Packets.

        /// <summary>
        /// Check if the tcp client socket is still connected.
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if the tcp client socket is still connected.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Close TCP Client.
        /// </summary>
        /// <param name="tcpClient"></param>
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
                            if (tcpClient != null)
                                tcpClient?.Close();
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
                // Ignored, the tcp client can be already closed, or disposed.
            }
        }

        /// <summary>
        /// Close Socket Client.
        /// </summary>
        /// <param name="socket"></param>
        public static void CloseSocket(Socket socket)
        {
            try
            {
                if (socket != null)
                {
                    if (socket.Connected)
                    {
                        try
                        {
                            socket.Shutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            if (socket != null)
                                socket?.Close();
                        }
                    }
                    else
                    {
                        socket?.Close();
                        socket?.Dispose();
                    }
                }
            }
            catch
            {
                // Ignored, the socket can be already closed, or disposed.
            }
        }

        /// <summary>
        /// Return each packet splitted received.
        /// </summary>
        /// <param name="packetDataToSplit"></param>
        /// <returns></returns>
        public static DisposableList<ClassReadPacketSplitted> GetEachPacketSplitted(byte[] packetBufferOnReceive, DisposableList<ClassReadPacketSplitted> listPacketReceived, CancellationTokenSource cancellation)
        {
            string packetData = packetBufferOnReceive.GetStringFromByteArrayUtf8().Replace("\0", "");

            if (packetData.Contains(ClassPeerPacketSetting.PacketPeerSplitSeperator))
            {
                int countSeperator = packetData.Count(x => x == ClassPeerPacketSetting.PacketPeerSplitSeperator);

                string[] splitPacketData = packetData.Split(new[] { ClassPeerPacketSetting.PacketPeerSplitSeperator }, StringSplitOptions.None);

                int completed = 0;
                foreach (string data in splitPacketData)
                {
                    if (cancellation.IsCancellationRequested)
                        break;

                    listPacketReceived[listPacketReceived.Count - 1].Packet += data.Replace(ClassPeerPacketSetting.PacketPeerSplitSeperator.ToString(), "");

                    if (completed < countSeperator)
                    {
                        listPacketReceived[listPacketReceived.Count - 1].Complete = true;
                        break;
                    }

                    completed++;
                }
            }
            else
                listPacketReceived[listPacketReceived.Count - 1].Packet += packetData.Replace(ClassPeerPacketSetting.PacketPeerSplitSeperator.ToString(), "");

            return listPacketReceived;
        }

        /// <summary>
        /// Do the padding of the packet content before encryption
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public static byte[] DoPacketPadding(byte[] packet)
        {
            int packetLength = packet.Length;
            int paddingSizeRequired = 16 - packetLength % 16;
            byte[] paddedBytes = new byte[packetLength + paddingSizeRequired];

            Buffer.BlockCopy(packet, 0, paddedBytes, 0, packetLength);

            for (int i = 0; i < paddingSizeRequired; i++)
                paddedBytes[packetLength + i] = (byte)paddingSizeRequired;

            return paddedBytes;
        }

        /// <summary>
        /// Remove the padding of the decrypted packet.
        /// </summary>
        /// <param name="decryptedPacket"></param>
        /// <returns></returns>
        public static byte[] UndoPacketPadding(byte[] decryptedPacket)
        {
            if (decryptedPacket == null || decryptedPacket?.Length == 0)
                return null;

            int size = decryptedPacket.Length - decryptedPacket[decryptedPacket.Length - 1];

            if (size > 0)
            {
                byte[] packet = new byte[size];
                Buffer.BlockCopy(decryptedPacket, 0, packet, 0, packet.Length);

                return packet;
            }

            return null;
        }


        #endregion

        #region Static functions about locking object.

        /// <summary>
        /// Lock and object to return
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectData"></param>
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

        /// <summary>
        /// Return the maximum of available processor count
        /// </summary>
        /// <returns></returns>
        public static int GetMaxAvailableProcessorCount()
        {
            return Environment.ProcessorCount;
        }


        private static readonly BigInteger KhStart = 1000;
        private static readonly BigInteger MhStart = BigInteger.Multiply(KhStart, 100);
        private static readonly BigInteger ThStart = BigInteger.Multiply(MhStart, 100);
        private static readonly BigInteger PhStart = BigInteger.Multiply(ThStart, 100);
        private static readonly BigInteger EhStart = BigInteger.Multiply(PhStart, 100);


        /// <summary>
        /// Return the hashrate formatted by his power.
        /// </summary>
        /// <param name="hashrate"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Convert bytes count into megabytes.
        /// </summary>
        /// <param name="bytesCount"></param>
        /// <returns></returns>
        public static string ConvertBytesToMegabytes(long bytesCount)
        {
            return bytesCount > 0 ? Math.Round((((double)bytesCount / 1024) / 1024), 2) + " MB(s)" : "0 MB(s)";
        }

        #endregion
    }

    #region Extensions class's of generic objects.

    /// <summary>
    /// String extension class.
    /// </summary>
    public static class ClassUtilityStringExtension
    {
        /// <summary>
        /// Copy a base58 string.
        /// </summary>
        /// <param name="base58String"></param>
        /// <param name="isWalletAddress">Use limits of wallet base58 string length limits stated.</param>
        /// <returns></returns>
        public static string CopyBase58String(this string base58String, bool isWalletAddress)
        {
            string base58StringCopy = string.Empty;
            bool isValid = false;

            foreach (var character in base58String)
            {
                if (ClassBase58.CharacterIsInsideBase58CharacterList(character))
                    base58StringCopy += character;

                if (isWalletAddress && base58StringCopy.Length >= BlockchainSetting.WalletAddressWifLengthMin
                    && base58StringCopy.Length <= BlockchainSetting.WalletAddressWifLengthMax)
                {
                    if (ClassBase58.DecodeWithCheckSum(base58StringCopy, true) != null)
                    {
                        isValid = true;
                        break;
                    }
                }
            }

            if (isValid)
                return base58StringCopy;

            if (!isWalletAddress && ClassBase58.DecodeWithCheckSum(base58StringCopy, false) != null)
                return base58StringCopy;

            return string.Empty;
        }

        /// <summary>
        /// Deep copy of string, to keep out the string copied linked to the original one.
        /// </summary>
        /// <param name="srcString"></param>
        /// <returns></returns>
        public static string DeepCopy(this string srcString)
        {
            string copiedString = string.Empty;

            foreach (char character in srcString)
                copiedString += character;

            return copiedString;
        }

        /// <summary>
        /// Extended unsafe function who permit to clear a string.
        /// </summary>
        /// <param name="s"></param>
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

        /// <summary>
        /// Attempt to split faster a string.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="seperatorStr"></param>
        /// <param name="countGetLimit"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Check if the string is empty.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
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

            while (char.IsWhiteSpace(str[startIndex]))
                startIndex += 1;

            while (char.IsWhiteSpace(str[endIndex]))
                endIndex -= 1;

            endIndex += 1;

            return str.Substring(startIndex, (endIndex - startIndex));
        }

        /// <summary>
        /// Get a specific string between two strings.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="firstString"></param>
        /// <param name="lastString"></param>
        /// <returns></returns>
        public static string GetStringBetweenTwoStrings(this string str, string firstString, string lastString)
        {
            int pos1 = str.IndexOf(firstString, 0, StringComparison.Ordinal) + firstString.Length;
            int pos2 = str.IndexOf(lastString, 0, StringComparison.Ordinal);

            return str.Substring(pos1, pos2 - pos1);
        }
    }

    /// <summary>
    /// Byte array extension class
    /// </summary>
    public static class ClassUtilityByteArrayExtension
    {
        private static readonly UTF8Encoding Utf8Encoding = new UTF8Encoding(true, false);

        /// <summary>
        /// Get a string from a byte array object.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string GetStringFromByteArrayAscii(this byte[] content)
        {
            return content != null && content?.Length > 0 ? new ASCIIEncoding().GetString(content) : null;
        }

        /// <summary>
        /// Get a string from a byte array object.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string GetStringFromByteArrayUtf8(this byte[] content)
        {
            try
            {
                return content != null && content?.Length > 0 ? Utf8Encoding.GetString(content) : null;
            }
            catch
            {
                return content != null && content?.Length > 0 ? Encoding.UTF8.GetString(content) : null;
            }
        }

        /// <summary>
        /// Compare the source array with another one.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public static bool CompareArray(this byte[] source, byte[] compare)
        {
            if (source != null)
                return compare == null || source.Length != compare.Length ? false : !source.Where((t, i) => t != compare[i]).Any();

            return compare != null ? true : false;
        }
    }

    public static class ClassUtilityNetworkStreamExtension
    {
        /// <summary>
        /// Try to split a packet data to send and try to send it.
        /// </summary>
        /// <param name="networkStream"></param>
        /// <param name="packetBytesToSend"></param>
        /// <param name="cancellation"></param>
        /// <param name="packetMaxSize"></param>
        /// <param name="singleWrite"></param>
        /// <returns></returns>
        public static async Task<bool> TrySendSplittedPacket(this NetworkStream networkStream, byte[] packetBytesToSend, CancellationTokenSource cancellation, int packetMaxSize, bool singleWrite = false)
        {

            try
            {

                if (packetBytesToSend.Length >= packetMaxSize && !singleWrite)
                {
                    int packetLength = packetBytesToSend.Length;
                    int countPacketSendLength = 0;

                    while (!cancellation.Token.IsCancellationRequested)
                    {
                        int packetSize = packetMaxSize;

                        if (countPacketSendLength + packetSize > packetLength)
                            packetSize = packetLength - countPacketSendLength;

                        if (packetSize <= 0)
                            break;

                        byte[] dataBytes = new byte[packetSize];

                        Array.Copy(packetBytesToSend, countPacketSendLength, dataBytes, 0, packetSize);

                        await networkStream.WriteAsync(dataBytes, 0, dataBytes.Length, cancellation.Token);
                        countPacketSendLength += packetSize;

                        if (countPacketSendLength >= packetLength)
                            break;

                    }

                    await networkStream.FlushAsync(cancellation.Token);
                }
                else
                {
                    await networkStream.WriteAsync(packetBytesToSend, 0, packetBytesToSend.Length, cancellation.Token);
                    await networkStream.FlushAsync(cancellation.Token);
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
        /// <summary>
        /// Try wait shortcut function.
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Try await async shortcut function.
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> TryWaitAsync(this SemaphoreSlim semaphore, CancellationTokenSource cancellation)
        {
            try
            {
                await semaphore.WaitAsync(cancellation.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try await async shortcut function.
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="delay"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> TryWaitWithDelayAsync(this SemaphoreSlim semaphore, int delay, CancellationTokenSource cancellation)
        {
            try
            {
                return await semaphore.WaitAsync(delay, cancellation.Token);
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Try to get a semaphore access and execute the action.
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="action"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task<bool> TryWaitExecuteActionAsync(this SemaphoreSlim semaphore, Action action, CancellationTokenSource cancellation)
        {
            if (await semaphore.TryWaitAsync(cancellation))
            {
                action.Invoke();

                semaphore.Release();
                return true;
            }

            return false;
        }



        /// <summary>
        /// Try to get a semaphore access and execute the action.
        /// </summary>
        /// <param name="semaphore"></param>
        /// <param name="action"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static bool TryWaitExecuteAction(this SemaphoreSlim semaphore, Action action, CancellationTokenSource cancellation)
        {
            if (semaphore.TryWait(cancellation))
            {
                action.Invoke();

                semaphore.Release();
                return true;
            }

            return false;
        }


    }
    #endregion
}
