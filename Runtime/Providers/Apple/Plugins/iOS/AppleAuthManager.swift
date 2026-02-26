import AuthenticationServices
import Foundation
import UIKit

@objc public class AppleAuthManager: NSObject {

    @objc public static let shared = AppleAuthManager()

    private var onSuccess: ((String) -> Void)?
    private var onFailure: ((String) -> Void)?

    private override init() { super.init() }

    @objc public func signIn(
        callbackObjectName: String,
        onSuccess: @escaping (String) -> Void,
        onFailure: @escaping (String) -> Void
    ) {
        self.onSuccess = onSuccess
        self.onFailure = onFailure

        DispatchQueue.main.async { [weak self] in
            self?.performSignIn()
        }
    }

    @objc public func getCredentialState(
        forUserId userId: String,
        onResult: @escaping (String) -> Void,
        onFailure: @escaping (String) -> Void
    ) {
        let provider = ASAuthorizationAppleIDProvider()
        provider.getCredentialState(forUserID: userId) { state, error in
            if let error = error {
                onFailure("\((error as NSError).code)")
                return
            }
            switch state {
            case .authorized:   onResult("authorized")
            case .revoked:      onResult("revoked")
            case .notFound:     onResult("notFound")
            case .transferred:  onResult("transferred")
            @unknown default:   onFailure("unknown")
            }
        }
    }

    private func performSignIn() {
        let provider = ASAuthorizationAppleIDProvider()
        let request = provider.createRequest()
        request.requestedScopes = [.fullName, .email]

        let controller = ASAuthorizationController(authorizationRequests: [request])
        controller.delegate = self
        controller.presentationContextProvider = self
        controller.performRequests()
    }
}

// MARK: - ASAuthorizationControllerDelegate

extension AppleAuthManager: ASAuthorizationControllerDelegate {

    public func authorizationController(
        controller: ASAuthorizationController,
        didCompleteWithAuthorization authorization: ASAuthorization
    ) {
        guard let credential = authorization.credential as? ASAuthorizationAppleIDCredential else {
            complete(withError: "1003")
            return
        }

        guard
            let tokenData = credential.identityToken,
            let identityToken = String(data: tokenData, encoding: .utf8),
            let codeData = credential.authorizationCode,
            let authorizationCode = String(data: codeData, encoding: .utf8)
        else {
            complete(withError: "1003")
            return
        }

        var payload: [String: String] = [
            "identityToken": identityToken,
            "authorizationCode": authorizationCode,
            "userId": credential.user
        ]

        if let email = credential.email {
            payload["email"] = email
        }

        if let fullName = credential.fullName {
            let formatter = PersonNameComponentsFormatter()
            formatter.style = .default
            let name = formatter.string(from: fullName)
            if !name.isEmpty {
                payload["fullName"] = name
            }
        }

        guard
            let jsonData = try? JSONSerialization.data(withJSONObject: payload),
            let jsonString = String(data: jsonData, encoding: .utf8)
        else {
            complete(withError: "1003")
            return
        }

        complete(withSuccess: jsonString)
    }

    public func authorizationController(
        controller: ASAuthorizationController,
        didCompleteWithError error: Error
    ) {
        // ASAuthorizationError codes:
        // 1001: canceled, 1002: failed, 1003: invalidResponse, 1004: notHandled
        let code = (error as NSError).code
        complete(withError: "\(code)")
    }
}

// MARK: - ASAuthorizationControllerPresentationContextProviding

extension AppleAuthManager: ASAuthorizationControllerPresentationContextProviding {

    public func presentationAnchor(for controller: ASAuthorizationController) -> ASPresentationAnchor {
        return UIApplication.shared.connectedScenes
            .compactMap { $0 as? UIWindowScene }
            .flatMap { $0.windows }
            .first(where: { $0.isKeyWindow }) ?? UIWindow()
    }
}

// MARK: - Private

private extension AppleAuthManager {

    func complete(withSuccess json: String) {
        let callback = onSuccess
        onSuccess = nil
        onFailure = nil
        callback?(json)
    }

    func complete(withError code: String) {
        let callback = onFailure
        onSuccess = nil
        onFailure = nil
        callback?(code)
    }
}
