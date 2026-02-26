using System;
using System.Collections.Generic;

namespace GreenEyes.Auth
{
    public sealed class AuthManager
    {
        private static readonly Lazy<AuthManager> _instance =
            new Lazy<AuthManager>(() => new AuthManager());

        public static AuthManager Instance => _instance.Value;

        private readonly Dictionary<AuthProviderType, IAuthProvider> _providers =
            new Dictionary<AuthProviderType, IAuthProvider>();

        private AuthManager() { }

        public void RegisterProvider(AuthProviderType type, IAuthProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _providers[type] = provider;
        }

        public void SignIn(AuthProviderType type, Action<AuthResult, AuthError> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!_providers.TryGetValue(type, out var provider))
            {
                callback.Invoke(null, new AuthError(
                    AuthErrorCode.NotSupported,
                    $"Provider '{type}' is not registered."));
                return;
            }

            provider.SignIn(callback);
        }

        public void SignOut(AuthProviderType type, Action<AuthError> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!_providers.TryGetValue(type, out var provider))
            {
                callback.Invoke(new AuthError(
                    AuthErrorCode.NotSupported,
                    $"Provider '{type}' is not registered."));
                return;
            }

            provider.SignOut(callback);
        }

        public void GetCredentialState(AuthProviderType type, string userId, Action<CredentialState, AuthError> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!_providers.TryGetValue(type, out var provider))
            {
                callback.Invoke(default, new AuthError(
                    AuthErrorCode.NotSupported,
                    $"Provider '{type}' is not registered."));
                return;
            }

            provider.GetCredentialState(userId, callback);
        }
    }
}
