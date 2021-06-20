using System;
using System.Threading.Tasks;
using KCPDemoProtocol;
using KCPNet;

namespace KCPClientDemo {
    /// <summary>
    /// 测试环境
    /// </summary>
    class ClientStart {
        private static KCPNet<ClientSession, NetMsg> client;
        private static Task<bool> checkTask = null;

        static void Main(string[] args) {
            string ip = "192.168.31.5";
            client = new KCPNet<ClientSession, NetMsg>();
            client.StartAsClient(ip, 10760);
            checkTask = client.ConnectServer(200);
            Task.Run(ConnectCheck);
            
            while (true) {
                string inptu = Console.ReadLine();
                if (inptu == "quit") {
                    client.CloseClient();
                    break;
                }
                else {
                    client.ClientSession.SendMsg(new NetMsg {
                        Info = inptu
                    });
                }
            }
            // NetMsg msg =new NetMsg {
            //     Cmd = CMD.NetPing,
            //     NetPing = new NetPing {
            //         Over = false
            //     }
            // };
            // var bytes = KCPTool.Serialize(msg);
            // var compressBytes = KCPTool.Compress(bytes);
            // var deCompressBytes = KCPTool.DeCompress(compressBytes);
            // var msg2 = KCPTool.DeSerialize<NetMsg>(deCompressBytes);
        }

        private static int counter = 0;

        static async void ConnectCheck() {
            while (true) {
                await Task.Delay(3000);
                if (checkTask != null && checkTask.IsCompleted) {
                    if (checkTask.Result) {
                        Log.ColorLog(KCPLogColor.Green, "连接服务成功");
                        checkTask = null;
                        await Task.Run(SendPingMsg);
                    }
                    else {
                        counter++;
                        if (counter > 4) {
                            Log.Error("连接失败，请检查网络情况");
                            checkTask = null;
                            break;
                        }

                        Log.Warn("正在尝试连接，当前连接次数{0}", counter);
                        checkTask = client.ConnectServer(200);
                    }
                }
            }
        }

        static async void SendPingMsg() {
            while (true) {
                await Task.Delay(5000);
                if (client != null && client.ClientSession != null) {
                    client.ClientSession.SendMsg(new NetMsg {
                        Cmd = CMD.NetPing,
                        NetPing = new NetPing {
                            Over = false
                        }
                    });
                    Log.ColorLog(KCPLogColor.Green, "发送Ping消息");
                }
                else {
                    Log.ColorLog(KCPLogColor.Green, "取消ping任务");
                    break;
                }
            }
        }
    }
}