using Grpc.Core;
using GrpcServiceDemo.Protos;

namespace GrpcServiceDemo.Services
{
    public class SampleService:Sample.SampleBase
    {
        public override Task<SampleResponse> GetFullName(SampleRequest request, ServerCallContext context)
        {
            var result = $"{request.FirstName} {request.LastName}";
            return Task.FromResult(new SampleResponse { FullName = result });
        }
    }
}
