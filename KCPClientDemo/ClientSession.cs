using System;
using KCPDemoProtocol;
using KCPNet;

namespace KCPClientDemo {
    public class ClientSession : KCPSession<NetMsg> {
        protected override void OnDisConnected() {
        }

        protected override void OnUpdate(DateTime now) {
        }

        protected override void OnReceiveMsg(NetMsg msg) {
            Log.ColorLog(KCPLogColor.Blue, "sessionId:{0}，收到了{1}", SessionId, msg.Info);
        }

        protected override void OnConnected() {
        }
    }
}