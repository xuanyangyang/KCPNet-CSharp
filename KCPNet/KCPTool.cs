using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace KCPNet {
    /// <summary>
    /// 日志
    /// </summary>
    public static class Log {
        public static Action<string> LOG_FUNC;
        public static Action<KCPLogColor, string> COLOR_LOG_FUNC;
        public static Action<string> WARN_LOG_FUNC;
        public static Action<string> ERROR_LOG_FUNC;

        public static void Info(string msg, params object[] args) {
            msg = string.Format(msg, args);
            if (LOG_FUNC == null) {
                ConsoleLog(msg, KCPLogColor.None);
            }
            else {
                LOG_FUNC(msg);
            }
        }

        public static void Warn(string msg, params object[] args) {
            msg = string.Format(msg, args);
            if (WARN_LOG_FUNC == null) {
                ConsoleLog(msg, KCPLogColor.Yellow);
            }
            else {
                WARN_LOG_FUNC(msg);
            }
        }

        public static void Error(string msg, params object[] args) {
            msg = string.Format(msg, args);
            if (ERROR_LOG_FUNC == null) {
                ConsoleLog(msg, KCPLogColor.Red);
            }
            else {
                ERROR_LOG_FUNC(msg);
            }
        }

        public static void ColorLog(KCPLogColor color, string msg, params object[] args) {
            msg = string.Format(msg, args);
            if (COLOR_LOG_FUNC == null) {
                ConsoleLog(msg, KCPLogColor.None);
            }
            else {
                COLOR_LOG_FUNC(color, msg);
            }
        }


        private static void ConsoleLog(string msg, KCPLogColor color) {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            msg = $"Thread:{threadId} {msg}";

            switch (color) {
                case KCPLogColor.Red:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case KCPLogColor.Green:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case KCPLogColor.Blue:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case KCPLogColor.Cyan:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case KCPLogColor.Yellow:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }

            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }

    public enum KCPLogColor {
        None,
        Red,
        Green,
        Blue,
        Cyan,
        Yellow,
    }

    public static class KCPTool {
        public static byte[] Serialize<T>(T msg) where T : KCPMsg {
            using (MemoryStream stream = new MemoryStream()) {
                try {
                    var binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Serialize(stream, msg);
                    stream.Seek(0, SeekOrigin.Begin);
                    return stream.ToArray();
                }
                catch (Exception e) {
                    Log.Error("序列化失败：{0}", e.Message);
                    throw;
                }
            }
        }

        public static T DeSerialize<T>(byte[] bytes) where T : KCPMsg {
            using (MemoryStream stream = new MemoryStream(bytes)) {
                try {
                    var binaryFormatter = new BinaryFormatter();
                    T msg = (T) binaryFormatter.Deserialize(stream);
                    return msg;
                }
                catch (Exception e) {
                    Log.Error("反序列化失败：{0}，消息长度：{1}", e.Message, bytes.Length);
                    throw;
                }
            }
        }

        public static byte[] Compress(byte[] input) {
            using (MemoryStream stream = new MemoryStream()) {
                using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress)) {
                    gZipStream.Write(input, 0, input.Length);
                }
                return stream.ToArray();
            }
        }

        public static byte[] DeCompress(byte[] input) {
            using (MemoryStream stream = new MemoryStream(input)) {
                using (MemoryStream outStream = new MemoryStream()) {
                    using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress)) {
                        byte[] bytes = new byte[1024];
                        int len;
                        while ((len = gZipStream.Read(bytes, 0, bytes.Length)) > 0) {
                            outStream.Write(bytes, 0, len);
                        }
                    }
                    return outStream.ToArray();
                }
            }
        }

        public static readonly DateTime UtcStart = new DateTime(1970, 1, 1);

        public static ulong GetUTCStartMilliseconds() {
            var timeSpan = DateTime.UtcNow - UtcStart;
            return (ulong)timeSpan.TotalMilliseconds;
        }
    }
}