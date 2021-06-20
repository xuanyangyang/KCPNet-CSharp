using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace KCPNet {
    [Serializable]
    public abstract class KCPMsg {
    }

    /// <summary>
    /// kcp
    /// </summary>
    public class KCPNet<T, K>
        where T : KCPSession<K>, new()
        where K : KCPMsg, new() {
        private UdpClient udp;
        private IPEndPoint remotePoint;
        private T clientSession;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        public KCPNet() {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        #region Server

        private Dictionary<uint, T> sessionDic = null;

        public void StartAsServer(string ip, int port) {
            sessionDic = new Dictionary<uint, T>();
            udp = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), port));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                udp.Client.IOControl((IOControlCode) (-1744830452), new byte[] {0, 0, 0, 0}, null);
            }

            Log.ColorLog(KCPLogColor.Green, "服务器启动");
            Task.Run(ServerReceive, cancellationToken);
        }

        async private void ServerReceive() {
            while (true) {
                try {
                    if (cancellationToken.IsCancellationRequested) {
                        Log.ColorLog(KCPLogColor.Cyan, "服务器接收被取消");
                        break;
                    }

                    UdpReceiveResult result = await udp.ReceiveAsync();
                    uint sessionId = BitConverter.ToUInt32(result.Buffer, 0);
                    if (sessionId == 0) {
                        sessionId = GenerateUniqueSessionId();
                        byte[] sessionIdBytes = BitConverter.GetBytes(sessionId);
                        byte[] convBytes = new byte[8];
                        Array.Copy(sessionIdBytes, 0, convBytes, 4, 4);
                        SendUdpMsg(convBytes, result.RemoteEndPoint);
                    }
                    else {
                        T session;
                        if (!sessionDic.ContainsKey(sessionId)) {
                            session = new T();
                            session.InitSession(sessionId, SendUdpMsg, result.RemoteEndPoint);
                            session.onSessionClose = OnServerSessionClose;
                            lock (sessionDic) {
                                sessionDic.Add(sessionId, session);
                            }
                        }
                        else {
                            session = sessionDic[sessionId];
                        }

                        session.ReceiveData(result.Buffer);
                    }
                }
                catch (Exception e) {
                    Log.Warn($"接受数据异常:{e}");
                }
            }
        }

        private void OnServerSessionClose(uint sessionId) {
            if (sessionDic.ContainsKey(sessionId)) {
                lock (sessionDic) {
                    sessionDic.Remove(sessionId);
                    Log.Warn("移除sessionId:{0}", sessionId);
                }
            }
            else {
                Log.Error("找不到sessionId:{0}");
            }
        }

        public void CloseServer() {
            foreach (var item in sessionDic) {
                item.Value.CloseSession();
            }

            sessionDic = null;

            if (udp != null) {
                udp.Close();
                udp = null;
                cancellationTokenSource.Cancel();
            }
        }

        #endregion

        # region CLient

        public void StartAsClient(string ip, int port) {
            udp = new UdpClient();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                udp.Client.IOControl((IOControlCode) (-1744830452), new byte[] {0, 0, 0, 0}, null);
            }

            remotePoint = new IPEndPoint(IPAddress.Parse(ip), port);
            Log.ColorLog(KCPLogColor.Green, "客户端启动");

            Task.Run(ClientReceive, cancellationToken);
        }

        public Task<bool> ConnectServer(int interval, int maxIntervalSum = 5000) {
            SendUdpMsg(new byte[4], remotePoint);
            Task<bool> task = Task.Run(async () => {
                int checkTimes = 0;
                while (true) {
                    await Task.Delay(interval);
                    checkTimes += interval;
                    if (clientSession != null && clientSession.IsConnected()) {
                        return true;
                    }

                    if (checkTimes > maxIntervalSum) {
                        return false;
                    }
                }
            });
            return task;
        }

        public void CloseClient() {
            if (clientSession == null) {
                return;
            }

            clientSession.CloseSession();
        }


        public async void ClientReceive() {
            while (true) {
                try {
                    if (cancellationToken.IsCancellationRequested) {
                        Log.ColorLog(KCPLogColor.Cyan, "客户端接收被取消");
                        break;
                    }

                    UdpReceiveResult result = await udp.ReceiveAsync();

                    if (!Equals(remotePoint, result.RemoteEndPoint)) {
                        Log.Warn("收到非法数据");
                        continue;
                    }

                    uint sessionId = BitConverter.ToUInt32(result.Buffer, 0);
                    if (sessionId == 0) {
                        // sid 数据
                        if (clientSession != null && clientSession.IsConnected()) {
                            Log.Warn("已经建立连接，初始化完成了，收到了多的sessionId，直接丢弃");
                        }
                        else {
                            // 未初始化，收到服务器分配的sid数据，初始化一个客户端session
                            sessionId = BitConverter.ToUInt32(result.Buffer, 4);
                            Log.ColorLog(KCPLogColor.Green, "接收到的sessionId:{0}", sessionId);

                            // 会话处理
                            clientSession = new T();
                            clientSession.InitSession(sessionId, SendUdpMsg, remotePoint);
                            clientSession.onSessionClose = OnClientSessionClose;
                        }
                    }
                    else {
                        // 业务逻辑数据
                        if (clientSession != null || clientSession.IsConnected()) {
                            clientSession.ReceiveData(result.Buffer);
                        }
                        else {
                            Log.Warn("客户端未初始化！");
                        }
                    }
                }
                catch (Exception e) {
                    Log.Warn($"接受数据异常:{e}");
                }
            }
        }

        protected void OnClientSessionClose(uint sessionId) {
            cancellationTokenSource.Cancel();
            if (udp != null) {
                udp.Close();
                udp = null;
            }

            Log.Warn("客户端关闭，sessionId:{0}", sessionId);
        }

        public T ClientSession {
            get => clientSession;
        }

        # endregion

        private void SendUdpMsg(byte[] bytes, IPEndPoint remotePoint) {
            if (udp != null) {
                udp.SendAsync(bytes, bytes.Length, remotePoint);
            }
        }

        public void BroadCastMsg(K msg) {
            byte[] bytes = KCPTool.Serialize(msg);
            foreach (var item in sessionDic) {
                item.Value.SendMsg(bytes);
            }
        }

        private uint curSessionId;

        public uint GenerateUniqueSessionId() {
            lock (sessionDic) {
                while (true) {
                    ++curSessionId;
                    if (curSessionId == uint.MaxValue) {
                        curSessionId = 1;
                    }

                    if (!sessionDic.ContainsKey(curSessionId)) {
                        break;
                    }
                }
            }

            return curSessionId;
        }
    }
}