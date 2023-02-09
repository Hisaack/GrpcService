using Grpc.Core;

namespace GrpcServiceDemo2.Services
{
    public class SubtractService:Subtract.SubtractBase
    {
        public override async Task<SubtractResponse> Subract(SubtractRequest request, ServerCallContext context)
        {
            var result = request.Number1 - request.Number2;
            await Task.Delay(10 * 1000); //Delay for 10 Seconds
            return (new SubtractResponse() { Result= result });
        }
    }
}
