namespace ConnectionFactoryTest
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Connections;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    internal sealed class SocketConnection : Connection, IConnectionProperties
    {
        private readonly Socket socket;

        private Stream stream;

        private InstanceMetadata metadata;

        public override EndPoint RemoteEndPoint => this.socket.RemoteEndPoint;

        public override EndPoint LocalEndPoint => this.socket.LocalEndPoint;

        public override IConnectionProperties ConnectionProperties => this;

        public SocketConnection(Socket socket, string metadata)
        {
            this.socket = socket;
            this.metadata = new InstanceMetadata()
            {
                Value = metadata
            };
        }

        protected override ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            try
            {
                if (method != ConnectionCloseMethod.GracefulShutdown)
                {
                    this.socket.Dispose();
                }

                this.stream?.Dispose();
            }
            catch (Exception ex)
            {
                return ValueTask.FromException(ex);
            }

            return default;
        }

        bool IConnectionProperties.TryGet(Type propertyKey, out object property)
        {
            if (propertyKey == typeof(Socket))
            {
                property = this.socket;
                return true;
            }

            if (propertyKey == typeof(InstanceMetadata))
            {
                property = this.metadata;
                return true;
            }

            property = null;
            return false;
        }

        protected override Stream CreateStream() => this.stream ??= new NetworkStream(this.socket, true);
    }
}