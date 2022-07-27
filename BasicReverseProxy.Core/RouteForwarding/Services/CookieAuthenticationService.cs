using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace BasicReverseProxy.Core.RouteForwarding.Services
{
    public class CookieAuthenticationService : IAuthenticationService
    {
        private class AuthorizationOptions : IAuthorizeData
        {
            public string Policy { get; set; }

            public string Roles { get; set; }

            public string AuthenticationSchemes { get; set; }
        }

        private readonly IPolicyEvaluator _policyEvaluator;
        private AuthorizationPolicy _authorizationPolicy;
        private readonly IAuthorizationPolicyProvider _policyProvider;
        private readonly IAuthorizeData[] _authorizeData;

        public CookieAuthenticationService(
            IAuthorizationPolicyProvider policyProvider,
            IPolicyEvaluator policyEvaluator)
        {
            _policyProvider = policyProvider;
            _policyEvaluator = policyEvaluator;
            _authorizeData = new IAuthorizeData[] { new AuthorizationOptions() { AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme } };
        }

        public async Task AuthenticateAsync(HttpContext context)
        {
            if (_authorizationPolicy is null)
            {
                _authorizationPolicy =
                    await AuthorizationPolicy.CombineAsync(_policyProvider, _authorizeData);
            }
            AuthenticateResult authenticateResult =
                await _policyEvaluator.AuthenticateAsync(_authorizationPolicy, context);
        }
    }
}
