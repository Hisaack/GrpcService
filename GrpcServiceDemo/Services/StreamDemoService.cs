using Grpc.Core;
using GrpcServiceDemo.Protos;

namespace GrpcServiceDemo.Services
{

    public class StreamDemoService:StreamDemo.StreamDemoBase
    {
        private Random random;

        public StreamDemoService()
        {
            random = new Random();
        }
        public override async Task ServerStreamingDemo(SendTest request, IServerStreamWriter<SendTest> responseStream, ServerCallContext context)
        {
            for (int i = 0; i <=20; i++)
            {
                await responseStream.WriteAsync(new SendTest { TestMessage = $"Message {i}" });
                var randomNumber = random.Next(1, 10);
                await Task.Delay(randomNumber * 1000); //*1000 to convert it to seconds
            }
        }

        public override async Task<SendTest> ClientStreamingDemo(IAsyncStreamReader<SendTest> requestStream, ServerCallContext context)
        {
            while(await requestStream.MoveNext())
            {
                //Printing all messages streamed by client
                Console.WriteLine(requestStream.Current.TestMessage);
            }

            Console.WriteLine("Client Streaming Completed");
            return new SendTest() { TestMessage = "Notify Client Back" };
        }

        public override async Task BidirectionalStreamingDemo(IAsyncStreamReader<SendTest> requestStream, IServerStreamWriter<SendTest> responseStream, ServerCallContext context)
        {
           var tasks=new List<Task>();
            while(await requestStream.MoveNext())
            {
                Console.WriteLine($"Received Request: {requestStream.Current.TestMessage}");
                var task = Task.Run(async () =>
                {
                    var message = requestStream.Current.TestMessage;
                    var randomNumber = random.Next(1, 10);
                    await Task.Delay(randomNumber * 1000);
                    await responseStream.WriteAsync(new SendTest() { TestMessage = message });
                    Console.WriteLine("Sent Response: " + message);
                });
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("Bidirectional Streaming Completed");
        }
    }
}
