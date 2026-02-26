package com.greeneyes.auth

import android.app.Activity
import android.net.Uri
import android.os.Bundle
import android.view.KeyEvent
import android.webkit.WebResourceError
import android.webkit.WebResourceRequest
import android.webkit.WebView
import android.webkit.WebViewClient
import com.unity3d.player.UnityPlayer
import org.json.JSONException
import org.json.JSONObject
import java.net.URLDecoder
import java.util.Base64

class AppleAuthWebViewActivity : Activity() {

    companion object {
        const val KEY_AUTH_URL        = "GREENEYES_AUTH_URL"
        const val KEY_REDIRECT_URL    = "GREENEYES_REDIRECT_URL"
        const val KEY_CALLBACK_OBJECT = "GREENEYES_CALLBACK_OBJECT"
    }

    private var mRedirectUrl: String? = null
    private var mCallbackObject: String? = null
    private var mResultSent = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        mRedirectUrl    = intent.getStringExtra(KEY_REDIRECT_URL)
        mCallbackObject = intent.getStringExtra(KEY_CALLBACK_OBJECT)
        val authUrl     = intent.getStringExtra(KEY_AUTH_URL)

        val webView = WebView(this)
        webView.settings.apply {
            javaScriptEnabled = true
            domStorageEnabled = true
        }

        webView.webViewClient = object : WebViewClient() {
            override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean {
                return handleUrl(request.url.toString())
            }

            @Suppress("DEPRECATION")
            override fun shouldOverrideUrlLoading(view: WebView, url: String): Boolean {
                return handleUrl(url)
            }

            override fun onReceivedError(view: WebView, request: WebResourceRequest, error: WebResourceError) {
                if (request.isForMainFrame) sendFailure("network")
            }
        }

        webView.loadUrl(authUrl ?: "")
        setContentView(webView)
    }

    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        if (keyCode == KeyEvent.KEYCODE_BACK) {
            sendFailure("cancelled")
            return true
        }
        return super.onKeyDown(keyCode, event)
    }

    // Returns true if the URL is our redirect and was handled
    private fun handleUrl(url: String): Boolean {
        val redirectUrl = mRedirectUrl ?: return false
        if (!url.startsWith(redirectUrl)) return false

        // Apple returns tokens in the URL fragment: redirect_uri#code=...&id_token=...
        val fragment = Uri.parse(url).fragment
        if (fragment.isNullOrEmpty()) {
            sendFailure("invalidResponse")
            finish()
            return true
        }

        try {
            var code: String? = null
            var idToken: String? = null
            var email: String? = null
            var fullName: String? = null

            for (param in fragment.split("&")) {
                val kv = param.split("=", limit = 2)
                if (kv.size < 2) continue
                val value = URLDecoder.decode(kv[1], "UTF-8")

                when (kv[0]) {
                    "code"     -> code    = value
                    "id_token" -> idToken = value
                    "user"     -> {
                        // Only provided on first sign-in
                        val (parsedFullName, parsedEmail) = parseUserParam(value)
                        fullName = parsedFullName
                        email    = parsedEmail
                    }
                }
            }

            if (idToken == null || code == null) {
                sendFailure("invalidResponse")
                finish()
                return true
            }

            val payload = JSONObject().apply {
                put("identityToken",     idToken)
                put("authorizationCode", code)
                put("userId",            extractSubFromJwt(idToken))
                if (!email.isNullOrEmpty())    put("email",    email)
                if (!fullName.isNullOrEmpty()) put("fullName", fullName)
            }

            sendSuccess(payload.toString())

        } catch (e: Exception) {
            sendFailure("invalidResponse")
        }

        finish()
        return true
    }

    // Parses Apple's user JSON: {"name":{"firstName":"John","lastName":"Doe"},"email":"john@example.com"}
    // Returns Pair(fullName, email)
    private fun parseUserParam(userJson: String): Pair<String?, String?> {
        return try {
            val user  = JSONObject(userJson)
            val email = user.optString("email").ifEmpty { null }

            val name     = user.optJSONObject("name")
            val fullName = name?.let {
                "${it.optString("firstName", "")} ${it.optString("lastName", "")}".trim().ifEmpty { null }
            }

            Pair(fullName, email)
        } catch (ignored: JSONException) {
            Pair(null, null)
        }
    }

    // Decodes the JWT payload and extracts the "sub" claim as userId
    private fun extractSubFromJwt(jwt: String): String {
        return try {
            val parts = jwt.split(".")
            if (parts.size < 2) return ""

            val decoded = Base64.getUrlDecoder().decode(parts[1])
            JSONObject(String(decoded, Charsets.UTF_8)).optString("sub", "")
        } catch (e: Exception) {
            ""
        }
    }

    private fun sendSuccess(json: String) {
        if (mResultSent) return
        mResultSent = true
        UnityPlayer.UnitySendMessage(mCallbackObject, "OnSuccess", json)
    }

    private fun sendFailure(errorCode: String) {
        if (mResultSent) return
        mResultSent = true
        UnityPlayer.UnitySendMessage(mCallbackObject, "OnFailure", errorCode)
    }
}
