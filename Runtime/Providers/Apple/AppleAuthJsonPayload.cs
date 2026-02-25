using System;
using UnityEngine;

namespace GreenEyes.Auth
{
    [Serializable]
    internal class AppleAuthJsonPayload
    {
        public string identityToken;
        public string authorizationCode;
        public string userId;
        public string email;
        public string fullName;

        public static AppleAuthJsonPayload FromJson(string json)
        {
            return JsonUtility.FromJson<AppleAuthJsonPayload>(json);
        }
    }
}
