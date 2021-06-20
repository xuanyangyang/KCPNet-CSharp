using System;
using KCPDemoProtocol;
using KCPNet;

namespace KCPDemo {
    /// <summary>
    /// 测试环境
    /// </summary>
    class ClientStart {
        private static KCPNet<ClientSession, NetMsg> client;

        static void Main(string[] args) {
            string ip = "192.168.1.100";
            client = new KCPNet<ClientSession, NetMsg>();
            client.StartAsClient(ip, 10760);
            client.ConnectServer();

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
        }
    }
}