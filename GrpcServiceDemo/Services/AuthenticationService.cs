using Grpc.Core;
using GrpcServiceDemo.AuthenticationUtils;
using GrpcServiceDemo.Protos;

namespace GrpcServiceDemo.Services
{
    public class AuthenticationService:Authentication.AuthenticationBase
    {
        public override async Task<AuthenticationResponse> Authenticate(AuthenticationRequest request, ServerCallContext context)
        {
            var authenticationResponse = JwtAuthenticationManager.Authenticate(request);
            if (authenticationResponse == null)
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid user Credentials"));

            return authenticationResponse;
        }
    }
}
