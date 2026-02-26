using System;

namespace GreenEyes.Auth
{
    public sealed class AppleAuthAndroidConfig
    {
        /// <summary>
        /// Apple Developer Console에서 생성한 Services ID (client_id).
        /// 예) com.example.service
        /// </summary>
        public string ServiceId { get; }

        /// <summary>
        /// Apple Developer Console에 등록한 Redirect URL.
        /// 반드시 HTTPS여야 하며 Apple에 사전 등록이 필요하다.
        /// 실제 서버 응답은 불필요하며, WebView가 URL 변경을 인터셉트해 토큰을 추출한다.
        /// 예) https://example.com/apple/callback
        /// </summary>
        public string RedirectUrl { get; }

        public AppleAuthAndroidConfig(string serviceId, string redirectUrl)
        {
            if (string.IsNullOrEmpty(serviceId))
                throw new ArgumentException("ServiceId must not be empty.", nameof(serviceId));
            if (string.IsNullOrEmpty(redirectUrl))
                throw new ArgumentException("RedirectUrl must not be empty.", nameof(redirectUrl));

            ServiceId = serviceId;
            RedirectUrl = redirectUrl;
        }
    }
}
