using System;
using KCPNet;

namespace KCPDemoProtocol {
    [Serializable]
    public class NetMsg : KCPMsg {
        public CMD Cmd { get; set; }
        public NetPing NetPing { get; set; }
        private string info;

        public string Info {
            get => info;
            set => info = value;
        }
    }

    [Serializable]
    public class ReqLogin {
        public string Account { get; set; }

        public string Password { get; set; }
    }

    [Serializable]
    public class NetPing {
        public bool Over { get; set; }
    }

    public enum CMD {
        None,
        ReqLogin,
        NetPing,
    }
}