using Microsoft.AspNetCore.Authentication;

namespace PraxisNote.Web.Auth;

public static class MockAuthenticationExtensions
{
    public static AuthenticationBuilder AddMockAuthentication(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<MockAuthenticationOptions, MockAuthenticationHandler>(
            MockAuthenticationOptions.SchemeName,
            options => { });
    }
}
