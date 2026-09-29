using ApiGateway.Exceptions;
using Xunit;

namespace ApiGateway.Tests.Exceptions
{
    public class GatewayExceptionMapperSingletonServiceTests
    {
        [Fact]
        public void Map_AnyException_ReturnsNull()
        {
            var mapper = new GatewayExceptionMapperSingletonService();

            Assert.Null(mapper.Map(new InvalidOperationException()));
        }
    }
}
