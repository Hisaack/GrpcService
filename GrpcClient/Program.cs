using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Balancer;
using GrpcServiceDemo.Protos;
using System.Threading.Channels;

namespace GrpcClient
{

    public class Program
    {
        private static Random random;
        public static async Task Main(string[] args)
        {
            random = new Random();
            var channel = GrpcChannel.ForAddress("http://localhost:5000");

            //await ServerStreamingDemo(channel);
            //await ClientStreaming(channel);
            //await BidirectionalStreaming(channel);

            //await SecuredEndPoints(channel);
            await MultiPassRequests(channel);
            await channel.ShutdownAsync();
            Console.ReadLine();
        }

        //gRPC showing Unary style (One to one communication)
        private static async Task UnaryDemo(GrpcChannel channel)
        {
            var client1 = new Sample.SampleClient(channel);
            var response1 = await client1.GetFullNameAsync(new SampleRequest() { FirstName = "John", LastName = "Thomas" });
            Console.WriteLine(response1.FullName);


            var client2 = new Product.ProductClient(channel);

            var stockDate = DateTime.SpecifyKind(new DateTime(2023, 2, 1), DateTimeKind.Utc);
            var response2 = await client2.SaveProductAsync(new ProductModel()
            {
                ProductName = "Arimis",
                ProductCode = "Arimis 200g",
                Price = "90",
                StockDate = Timestamp.FromDateTime(stockDate)
            });
            Console.WriteLine($"{response2.StatusCode} | {response2.IsSuccessful}");


            var response3 = await client2.GetProductsAsync(new Google.Protobuf.WellKnownTypes.Empty());

            foreach (var product in response3.Products)
            {
                var _stockDate = product.StockDate.ToDateTime();
                Console.WriteLine($"{product.ProductName} | {product.ProductCode} | {product.Price} | {_stockDate.ToString("dd-MM-yyyy")}");

            }
        }

        //gRPC showing Server Streaming (Single Request from Client with Multiple Streaming from server)
        private static async Task ServerStreamingDemo(GrpcChannel channel)
        {
           
            var client = new StreamDemo.StreamDemoClient(channel);
            var response = client.ServerStreamingDemo(new SendTest { TestMessage = "Anza" });
            while(await response.ResponseStream.MoveNext(CancellationToken.None))
            {
                var value = response.ResponseStream.Current.TestMessage;
                Console.WriteLine(value);
            }

            Console.WriteLine("Server Streaming Completed");
        }

        //gRPC showing Client Streaming (Single Request from Server with Multiple Streaming from client)
        private static async Task ClientStreaming(GrpcChannel channel)
        {
            var client = new StreamDemo.StreamDemoClient(channel);
            var stream = client.ClientStreamingDemo();
            for (int i = 1; i <=10 ; i++)
            {
                await stream.RequestStream.WriteAsync(new SendTest() { TestMessage = $"Message {i}" });
            }

            await stream.RequestStream.CompleteAsync();
            Console.WriteLine("Client Streaming Completed");
        }
        //gRPC showing Bidirectional Streaming (Multiple Steaming from Client with Multiple Streaming from the Server)
        private static async Task BidirectionalStreaming(GrpcChannel channel)
        {
            var client = new StreamDemo.StreamDemoClient(channel);
            var stream = client.BidirectionalStreamingDemo();

            var requestedTask = Task.Run(async () =>
            {
                for (int i = 0; i <= 10; i++)
                {
                    var randomNumber = random.Next(1, 10);
                    await Task.Delay(randomNumber * 1000);
                    await stream.RequestStream.WriteAsync(new SendTest() { TestMessage = i.ToString() });
                    Console.WriteLine("Sent Request: " + i);
                }

                await stream.RequestStream.CompleteAsync();
            });

            var respondedTask = Task.Run(async () =>
            {
                while(await stream.ResponseStream.MoveNext(CancellationToken.None))
                {
                    Console.WriteLine("Received Response: " + stream.ResponseStream.Current.TestMessage);
                }

                Console.WriteLine("Response Stream Completed");
            });

            await Task.WhenAll(requestedTask, respondedTask);
        }

        //gRPC showing Authentication Services to secure the endpoints APIS
        private static async Task SecuredEndPoints(GrpcChannel channel)
        {
            var authenticationClient=new Authentication.AuthenticationClient(channel);
            //Here, you can get the User Name and Password from the UI or Database
            try
            {
                var authenticationResponse = authenticationClient.Authenticate(new AuthenticationRequest
                {
                    UserName = "admin",
                    Password = "admin"
                });
                Console.WriteLine($"Received Auth Response | Token: {authenticationResponse.AccessToken} | Expiry Time Seconds: {authenticationResponse.ExpiresIn}");
                var calculationClient = new Calculation.CalculationClient(channel);
                var header = new Metadata();
                header.Add("Authorization", $"Bearer {authenticationResponse.AccessToken}");

                //var calculationResults = await calculationClient.AddAsync(new InputNumbers() { Number1 = 5, Number2 = 10 }, header);
                //Console.WriteLine("Sum Results: " + calculationResults.Result);

                var calculationResults1 = await calculationClient.DivideAsync(new InputNumbers() { Number1 = 5, Number2 = 10 }, header);
                Console.WriteLine("Sum Results: " + calculationResults1.Result);
            }
            catch (RpcException ex)
            {

                Console.WriteLine($"Status Code: {ex.StatusCode} | Error: {ex.Message}");
                return;
            }

        
        }

        //gRPC showing request from CLIENT -> SERVER 1 -> SERVER 2 then response comes back from SERVER 2 -> SERVER 1 -> CLIENT
        //We are also showing gRPC deadline and deadline propagation: It means ---> Time Limit for an RPC call
        private static async Task MultiPassRequests(GrpcChannel channel)
        {
            var num1 = 100;
            var num2 = 140;

            Console.WriteLine($"Number1: {num1}");
            Console.WriteLine($"Number2: {num2}");
            Console.WriteLine("------------------");

            var calculationClient = new Calculation.CalculationClient(channel);

            await Sum(calculationClient, num1, num2);
            await Subtract(calculationClient, num1, num2);

            Console.WriteLine("------------------");
            Console.WriteLine("Channel Closed");

        }
        private static async Task Sum(Calculation.CalculationClient calculationClient, int num1, int num2)
        {
            try
            {
                var sumResult = await calculationClient.AddAsync(new InputNumbers() { Number1 = num1, Number2 = num2 }, deadline: DateTime.UtcNow.AddSeconds(5));
                Console.WriteLine($"Sum Result: {sumResult.Result}");
            }
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.DeadlineExceeded)
                    Console.WriteLine("Sum Result: Request Timeout");
                else
                    Console.WriteLine("Error: " + ex.Message);
            }
        }
        private static async Task Subtract(Calculation.CalculationClient calculationClient, int num1, int num2)
        {
            try
            {
                var sumResult = await calculationClient.SubtractAsync(new InputNumbers() { Number1 = num1, Number2 = num2 }, deadline: DateTime.UtcNow.AddSeconds(5));
                Console.WriteLine($"Subtract Result: {sumResult.Result}");
            }
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.DeadlineExceeded)
                    Console.WriteLine("Subtract Result: Request Timeout");
                else
                    Console.WriteLine("Error: " + ex.Message);
            }

           
        }
    }
}