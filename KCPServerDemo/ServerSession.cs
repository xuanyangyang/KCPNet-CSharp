using System;
using KCPDemoProtocol;
using KCPNet;

namespace KCPServerDemo {
    /// <summary>
    /// 服务端session
    /// </summary>
    public class ServerSession : KCPSession<NetMsg> {
        private int checkCounter;
        DateTime checkTime = DateTime.UtcNow.AddSeconds(5);

        protected override void OnDisConnected() {
            Log.ColorLog(KCPLogColor.Green, "{0}客户端断开连接", SessionId);
        }

        protected override void OnUpdate(DateTime now) {
            if (now > checkTime) {
                checkTime = now.AddSeconds(5);
                checkCounter++;
                if (checkCounter > 3) {
                    NetMsg pingMsg = new NetMsg {
                        Cmd = CMD.NetPing,
                        NetPing = new NetPing {
                            Over = true
                        }
                    };
                    OnReceiveMsg(pingMsg);
                }
            }
        }

        protected override void OnReceiveMsg(NetMsg msg) {
            Log.ColorLog(KCPLogColor.Green, "{0}收到客户端数据，CMD:{1}, {2}", SessionId, msg.Cmd, msg.Info);
            if (msg.Cmd == CMD.NetPing) {
                if (msg.NetPing.Over) {
                    CloseSession();
                }
                else {
                    // 收到ping请求，重置检测计数，并回复ping消息
                    checkCounter = 0;
                    NetMsg pingMsg = new NetMsg {
                        Cmd = CMD.NetPing,
                        NetPing = new NetPing {
                            Over = false
                        }
                    };
                    SendMsg(pingMsg);
                }
            }
        }

        protected override void OnConnected() {
            Log.ColorLog(KCPLogColor.Green, "{0}客户端连接", SessionId);
        }
    }
}