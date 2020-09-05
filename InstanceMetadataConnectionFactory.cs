namespace ConnectionFactoryTest
{
    using System;
    using System.Buffers;
    using System.Net;
    using System.Net.Connections;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class InstanceMetadataConnectionFactory : ConnectionFactory
    {
        private const int MaxImdsResponseSize = 256;

        private readonly Memory<byte> getRequest = Encoding.UTF8.GetBytes("GET /imds HTTP/1.1\r\nHost: www.bing.com\r\nContent-Length: 0\r\n\r\n").AsMemory();

        public async override ValueTask<Connection> ConnectAsync(EndPoint endPoint, IConnectionProperties options = null, CancellationToken cancellationToken = default)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            await socket.ConnectAsync(endPoint).ConfigureAwait(false);
            await socket.SendAsync(this.getRequest, SocketFlags.None);

            byte[] memory = null;
            var arrayPool = ArrayPool<byte>.Shared;

            try
            {
                memory = arrayPool.Rent(MaxImdsResponseSize);

                int totalBytesReceived = 0;
                do
                {
                    int bytesReceived = await socket.ReceiveAsync(new ArraySegment<byte>(memory, totalBytesReceived, MaxImdsResponseSize - totalBytesReceived), SocketFlags.None);
                    if (bytesReceived <= 0)
                    {
                        break;
                    }

                    totalBytesReceived += bytesReceived;

                    if (memory[totalBytesReceived - 1] == '\n' && memory[totalBytesReceived - 2] == '\r' && memory[totalBytesReceived - 3] == '\n' && memory[totalBytesReceived - 4] == '\r')
                    {
                        break;
                    }

                } while (totalBytesReceived < MaxImdsResponseSize);

                if (totalBytesReceived == MaxImdsResponseSize)
                {
                    throw new Exception($"Imds Endpoint is invalid, size must be < {MaxImdsResponseSize}");
                }

                return new SocketConnection(socket, Encoding.ASCII.GetString(memory, 0, totalBytesReceived));
            }
            finally
            {
                if (memory != null)
                {
                    arrayPool.Return(memory);
                }
            }
        }
    }
}
