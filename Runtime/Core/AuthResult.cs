namespace GreenEyes.Auth
{
    public sealed class AuthResult
    {
        public string IdentityToken { get; }
        public string AuthorizationCode { get; }
        public string UserId { get; }
        public string Email { get; }     // null on subsequent sign-ins
        public string FullName { get; }  // null on subsequent sign-ins

        public AuthResult(
            string identityToken,
            string authorizationCode,
            string userId,
            string email = null,
            string fullName = null)
        {
            IdentityToken = identityToken;
            AuthorizationCode = authorizationCode;
            UserId = userId;
            Email = email;
            FullName = fullName;
        }
    }
}
