namespace ConnectionFactoryTest
{
    using System;
    using System.Net.Http;

    class Program
    {
        static void Main(string[] args)
        {
            var socketsHttpHandler = new SocketsHttpHandler
            {
                ConnectionFactory = new InstanceMetadataConnectionFactory()
            };

            var httpclient = new HttpClient(socketsHttpHandler);
            var output = httpclient.GetStringAsync("http://localhost:8092/api/test").Result;

            Console.WriteLine(output);
        }
    }
}
