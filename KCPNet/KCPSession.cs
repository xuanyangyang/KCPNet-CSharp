using System;
using System.Net;
using System.Net.Sockets.Kcp;

namespace KCPNet_CSharp {
    public enum SessionState {
        None,
        Connected,
        DisConnected
    }

    public class KCPSession {
        /// <summary>
        /// 会话ID
        /// </summary>
        protected uint sessionId;

        /// <summary>
        /// 远程地址
        /// </summary>
        private IPEndPoint remotePoint;

        /// <summary>
        /// 状态
        /// </summary>
        protected SessionState state = SessionState.None;

        /// <summary>
        /// udp发送器
        /// </summary>
        protected Action<byte[], IPEndPoint> udpSender;

        public KCPHandler handler;
        public Kcp kcp;

        public void InitSession(uint sessionId, Action<byte[], IPEndPoint> udpSender, IPEndPoint remotePoint) {
            this.sessionId = sessionId;
            this.udpSender = udpSender;
            this.remotePoint = remotePoint;

            handler = new KCPHandler();
            kcp = new Kcp(sessionId, handler);
            kcp.NoDelay(1, 20, 2, 1);
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

            handler.Out = buffer => {
                var bytes = buffer.ToArray();
                udpSender(bytes, remotePoint);
            };
        }
    }
}