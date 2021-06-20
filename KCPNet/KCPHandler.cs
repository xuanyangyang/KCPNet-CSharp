using System;
using System.Buffers;
using System.Net.Sockets.Kcp;

namespace KCPNet {
    /// <summary>
    /// kcp数据处理器
    /// </summary>
    public class KCPHandler : IKcpCallback {
        public Action<Memory<byte>> Out;

        public void Output(IMemoryOwner<byte> buffer, int avalidLength) {
            using (buffer) {
                Out(buffer.Memory.Slice(0, avalidLength));
            }
        }

        public Action<byte[]> Recv;

        public void Recive(byte[] buffer) {
            Recv(buffer);
        }
    }
}