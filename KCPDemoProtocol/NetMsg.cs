using KCPNet;

namespace KCPDemoProtocol {
    public class NetMsg : KCPMsg {
        private string info;

        public string Info {
            get => info;
            set => info = value;
        }
    }
}