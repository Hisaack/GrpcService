using Grpc.Core;
using Grpc.Net.Client;
using GrpcServiceDemo.Protos;
using GrpcServiceDemo2;
using Microsoft.AspNetCore.Authorization;

namespace GrpcServiceDemo.Services
{
    public class CalculationService:Calculation.CalculationBase
    {
        //[Authorize(Roles = "Administrator")]
        [AllowAnonymous]
        public override async Task<CalculationResult> Add(InputNumbers request, ServerCallContext context)
        {
            //Let's simulate a task that can take more that 5 sec to Test Deadline events
            await Task.Delay(10 * 1000);
            return (new CalculationResult()
            {
                Result = request.Number1 + request.Number2
            });
        }
        //[Authorize(Roles = "Administrator,User")]
        [AllowAnonymous]
        public override async Task<CalculationResult> Subtract(InputNumbers request, ServerCallContext context)
        {
            //return Task.FromResult(new CalculationResult()
            //{
            //    Result = request.Number1 - request.Number2
            //});

            //Let's send another request to another server : as this server will act as a client!
            //THIS IS COMING FROM MULTIPASSREQUEST Method from Client project
            var channel = GrpcChannel.ForAddress("http://localhost:5152");
            var subtractClient = new Subtract.SubtractClient(channel);
            //We shall use deadline propagation technique here when we are forwarding the original deadline info from client
            //as shown ---deadline: context.Deadline---
            var subtractResponse = await subtractClient.SubractAsync(new SubtractRequest()
            {
                Number1 = request.Number1,
                Number2 = request.Number2
            }, deadline: context.Deadline);
            await channel.ShutdownAsync();
            return new CalculationResult() { Result=subtractResponse.Result };
        }
        [AllowAnonymous]
        public override Task<CalculationResult> Multiply(InputNumbers request, ServerCallContext context)
        {
            return Task.FromResult(new CalculationResult() { Result = request.Number1 * request.Number2 });
        }
        [Authorize(Roles = "Administrator")]
        public override Task<CalculationResult> Divide(InputNumbers request, ServerCallContext context)
        {
            return Task.FromResult(new CalculationResult() { Result = request.Number1 / request.Number2 });
        }
    }
}
