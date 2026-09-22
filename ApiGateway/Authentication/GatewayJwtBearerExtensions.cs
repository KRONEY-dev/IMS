using ApiGateway.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Kernel.Caching;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ApiGateway.Authentication
{
    public static class GatewayJwtBearerExtensions
    {
        public static IServiceCollection AddGatewayJwtBearerAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtValidationSettings>, IAccessTokenBlacklist, IUserAccessRevocation>(
                    (options, jwtSettings, accessTokenBlacklist, userAccessRevocation) =>
                {
                    var settings = jwtSettings.Value;

                    var rsa = RSA.Create();
                    rsa.ImportFromPem(File.ReadAllText(settings.PublicKeyPath));

                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = settings.Issuer,
                        ValidAudience = settings.Audience,
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

                            if (jti is null)
                            {
                                context.Fail("Access token is missing a jti claim.");
                                return;
                            }

                            if (await accessTokenBlacklist.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
                            {
                                context.Fail("Access token has been revoked.");
                                return;
                            }

                            var sub = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                            var iatClaim = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Iat);

                            if (sub is null || iatClaim is null)
                            {
                                context.Fail("Access token is missing required claims.");
                                return;
                            }

                            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(iatClaim));
                            var revokedAt = await userAccessRevocation.GetRevokedAtAsync(
                                Guid.Parse(sub), context.HttpContext.RequestAborted);

                            if (revokedAt is not null && issuedAt <= revokedAt)
                            {
                                context.Fail("User access has been revoked; refresh required.");
                            }
                        }
                    };
                });

            return services;
        }
    }
}