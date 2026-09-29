using ApiGateway.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace ApiGateway.Tests.Authorization
{
    public class IpAllowlistAuthorizationHandlerTests
    {
        private readonly Mock<IDatabase> _databaseMock = new();
        private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();

        public IpAllowlistAuthorizationHandlerTests()
        {
            _connectionMultiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_databaseMock.Object);
        }

        private IpAllowlistAuthorizationHandler CreateHandler()
        {
            return new IpAllowlistAuthorizationHandler(
                _connectionMultiplexerMock.Object, _httpContextAccessorMock.Object,
                Mock.Of<ILogger<IpAllowlistAuthorizationHandler>>());
        }

        private void SetRemoteIp(string ip)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);

            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        }

        private void SetAllowedNetworks(params string[] networks)
        {
            _databaseMock.Setup(d => d.HashKeysAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(networks.Select(n => (RedisValue)n).ToArray());
        }

        private static async Task<AuthorizationHandlerContext> RunAsync(IpAllowlistAuthorizationHandler handler)
        {
            var context = new AuthorizationHandlerContext(
                [new IpAllowlistRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), resource: null);

            await handler.HandleAsync(context);

            return context;
        }

        [Fact]
        public async Task HandleRequirementAsync_IpInsideAllowedNetwork_Succeeds()
        {
            SetRemoteIp("192.168.1.42");
            SetAllowedNetworks("192.168.1.0/24");

            var context = await RunAsync(CreateHandler());

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_IpOutsideAllAllowedNetworks_DoesNotSucceed()
        {
            SetRemoteIp("10.0.0.5");
            SetAllowedNetworks("192.168.1.0/24");

            var context = await RunAsync(CreateHandler());

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_EmptyAllowlist_DoesNotSucceed()
        {
            SetRemoteIp("192.168.1.42");
            SetAllowedNetworks();

            var context = await RunAsync(CreateHandler());

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_MalformedCidrEntryPresent_IsIgnoredWithoutThrowing()
        {
            SetRemoteIp("192.168.1.42");
            SetAllowedNetworks("not-a-cidr", "192.168.1.0/24");

            var context = await RunAsync(CreateHandler());

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_Ipv4MappedToIpv6_IsNormalizedBeforeMatching()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.168.1.42");
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

            SetAllowedNetworks("192.168.1.0/24");

            var context = await RunAsync(CreateHandler());

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_NoRemoteIpAvailable_DoesNotSucceed()
        {
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            SetAllowedNetworks("192.168.1.0/24");

            var context = await RunAsync(CreateHandler());

            Assert.False(context.HasSucceeded);
        }
    }
}
