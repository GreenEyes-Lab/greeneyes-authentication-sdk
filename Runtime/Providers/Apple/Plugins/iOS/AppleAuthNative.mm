#import <Foundation/Foundation.h>
#import "AppleAuthNative.h"

// Unity runtime C function — available at link time in the generated Xcode project
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

// Swift-generated ObjC header.
// In Unity's Xcode project all plugin sources are compiled into the UnityFramework target,
// so the generated header is importable as below.
#if __has_include("UnityFramework-Swift.h")
    #import "UnityFramework-Swift.h"
#endif

extern "C" {

void _GreenEyes_Apple_SignIn(const char* callbackObjectName) {
    NSString* objName = [NSString stringWithUTF8String:callbackObjectName];

    [[AppleAuthManager shared]
        signInWithCallbackObjectName:objName
        onSuccess:^(NSString* json) {
            UnitySendMessage(callbackObjectName, "OnSuccess", [json UTF8String]);
        }
        onFailure:^(NSString* errorCode) {
            UnitySendMessage(callbackObjectName, "OnFailure", [errorCode UTF8String]);
        }
    ];
}

void _GreenEyes_Apple_GetCredentialState(const char* userId, const char* callbackObjectName) {
    NSString* userIdStr = [NSString stringWithUTF8String:userId];

    [[AppleAuthManager shared]
        getCredentialStateForUserId:userIdStr
        onResult:^(NSString* state) {
            UnitySendMessage(callbackObjectName, "OnCredentialState", [state UTF8String]);
        }
        onFailure:^(NSString* errorCode) {
            UnitySendMessage(callbackObjectName, "OnCredentialStateFailure", [errorCode UTF8String]);
        }
    ];
}

} // extern "C"
