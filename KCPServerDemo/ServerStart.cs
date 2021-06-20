using System;
using KCPDemoProtocol;
using KCPNet;

namespace KCPServerDemo {
    class ServerStart {
        static void Main(string[] args) {
            string ip = "192.168.31.5";
            KCPNet<ServerSession, NetMsg> server = new KCPNet<ServerSession, NetMsg>();
            server.StartAsServer(ip, 10760);
            while (true) {
                var ipt = Console.ReadLine();
                if (ipt == "quit") {
                    server.CloseServer();
                    break;
                }
                else {
                    server.BroadCastMsg(new NetMsg {
                        Info = ipt
                    });
                }
            }

            Console.ReadKey();
        }
    }
}