using System;
using System.Net;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

namespace KCPNet {
    public enum SessionState {
        None,
        Connected,
        DisConnected
    }

    public abstract class KCPSession<T> where T : KCPMsg, new() {
        /// <summary>
        /// 会话ID
        /// </summary>
        private uint sessionId;

        /// <summary>
        /// 远程地址
        /// </summary>
        private IPEndPoint remotePoint;

        /// <summary>
        /// 状态
        /// </summary>
        private SessionState state = SessionState.None;

        public Action<uint> onSessionClose;

        /// <summary>
        /// udp发送器
        /// </summary>
        private Action<byte[], IPEndPoint> udpSender;

        private KCPHandler handler;
        private Kcp kcp;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        public void InitSession(uint sessionId, Action<byte[], IPEndPoint> udpSender, IPEndPoint remotePoint) {
            this.sessionId = sessionId;
            this.udpSender = udpSender;
            this.remotePoint = remotePoint;
            this.state = SessionState.Connected;

            handler = new KCPHandler();
            kcp = new Kcp(sessionId, handler);
            kcp.NoDelay(1, 20, 2, 1);
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

            handler.Out = buffer => {
                var bytes = buffer.ToArray();
                udpSender(bytes, remotePoint);
            };

            handler.Recv = buffer => {
                buffer = KCPTool.DeCompress(buffer);
                T msg = KCPTool.DeSerialize<T>(buffer);
                if (msg != null) {
                    OnReceiveMsg(msg);
                }
            };

            OnConnected();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            Task.Run(Update, cancellationToken);
        }

        public bool IsConnected() {
            return state == SessionState.Connected;
        }

        public void CloseSession() {
            cancellationTokenSource.Cancel();
            OnDisConnected();
            onSessionClose?.Invoke(sessionId);
            onSessionClose = null;
            state = SessionState.DisConnected;
            remotePoint = null;
            udpSender = null;
            handler = null;
            kcp = null;
            sessionId = 0;
            cancellationTokenSource = null;
        }

        protected abstract void OnDisConnected();

        protected abstract void OnUpdate(DateTime now);

        protected abstract void OnReceiveMsg(T msg);

        protected abstract void OnConnected();

        public void ReceiveData(byte[] buffer) {
            kcp.Input(buffer.AsSpan());
        }

        async void Update() {
            try {
                while (true) {
                    var now = DateTime.UtcNow;
                    OnUpdate(now);
                    if (cancellationToken.IsCancellationRequested) {
                        Log.ColorLog(KCPLogColor.Cyan, "kcp update取消了");
                        break;
                    }
                    else {
                        kcp.Update(now);
                        int len;
                        while ((len = kcp.PeekSize()) > 0) {
                            var buffer = new byte[len];
                            if (kcp.Recv(buffer) >= 0) {
                                handler.Recive(buffer);
                            }
                        }

                        await Task.Delay(10);
                    }
                }
            }
            catch (Exception e) {
                Log.Error("kcp update异常:{0}", e.ToString());
            }
        }

        public void SendMsg(T msg) {
            byte[] bytes = KCPTool.Serialize(msg);
            if (bytes != null) {
                SendMsg(bytes);
            }
        }

        public void SendMsg(byte[] bytes) {
            if (IsConnected()) {
                bytes = KCPTool.Compress(bytes);
                kcp.Send(bytes);
            }
            else {
                Log.Warn("未连接，不能发送");
            }
        }

        protected bool Equals(KCPSession<T> other) {
            return sessionId == other.sessionId;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KCPSession<T>) obj);
        }

        public uint SessionId {
            get => sessionId;
            set => sessionId = value;
        }

        public override int GetHashCode() {
            return sessionId.GetHashCode();
        }
    }
}