package com.greeneyes.auth;

import android.app.Activity;
import android.net.Uri;
import android.os.Bundle;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.UnsupportedEncodingException;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.Base64;

public class AppleAuthWebViewActivity extends Activity {

    public static final String KEY_AUTH_URL       = "GREENEYES_AUTH_URL";
    public static final String KEY_REDIRECT_URL   = "GREENEYES_REDIRECT_URL";
    public static final String KEY_CALLBACK_OBJECT = "GREENEYES_CALLBACK_OBJECT";

    private String mRedirectUrl;
    private String mCallbackObject;
    private boolean mResultSent = false;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        mRedirectUrl    = getIntent().getStringExtra(KEY_REDIRECT_URL);
        mCallbackObject = getIntent().getStringExtra(KEY_CALLBACK_OBJECT);
        String authUrl  = getIntent().getStringExtra(KEY_AUTH_URL);

        WebView webView = new WebView(this);
        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);

        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                return handleUrl(request.getUrl().toString());
            }

            @Override
            @SuppressWarnings("deprecation")
            public boolean shouldOverrideUrlLoading(WebView view, String url) {
                return handleUrl(url);
            }

            @Override
            public void onReceivedError(WebView view, WebResourceRequest request, WebResourceError error) {
                if (request.isForMainFrame()) {
                    sendFailure("network");
                }
            }
        });

        webView.loadUrl(authUrl);
        setContentView(webView);
    }

    @Override
    public void onBackPressed() {
        sendFailure("cancelled");
        super.onBackPressed();
    }

    // Returns true if the URL is our redirect and was handled
    private boolean handleUrl(String url) {
        if (mRedirectUrl == null || !url.startsWith(mRedirectUrl)) {
            return false;
        }

        // Apple returns tokens in the URL fragment: redirect_uri#code=...&id_token=...
        Uri uri = Uri.parse(url);
        String fragment = uri.getFragment();

        if (fragment == null || fragment.isEmpty()) {
            sendFailure("invalidResponse");
            finish();
            return true;
        }

        try {
            // Parse fragment as key=value pairs
            String code = null, idToken = null, email = null, fullName = null;

            for (String param : fragment.split("&")) {
                String[] kv = param.split("=", 2);
                if (kv.length < 2) continue;
                String key = kv[0];
                String value = URLDecoder.decode(kv[1], "UTF-8");

                switch (key) {
                    case "code":     code    = value; break;
                    case "id_token": idToken = value; break;
                    case "user":
                        // Only provided on first sign-in
                        String[] nameEmail = parseUserParam(value);
                        fullName = nameEmail[0];
                        email    = nameEmail[1];
                        break;
                }
            }

            if (idToken == null || code == null) {
                sendFailure("invalidResponse");
                finish();
                return true;
            }

            String userId = extractSubFromJwt(idToken);

            JSONObject payload = new JSONObject();
            payload.put("identityToken",    idToken);
            payload.put("authorizationCode", code);
            payload.put("userId",           userId);
            if (email    != null && !email.isEmpty())    payload.put("email",    email);
            if (fullName != null && !fullName.isEmpty()) payload.put("fullName", fullName);

            sendSuccess(payload.toString());

        } catch (Exception e) {
            sendFailure("invalidResponse");
        }

        finish();
        return true;
    }

    // Parses Apple's user JSON: {"name":{"firstName":"John","lastName":"Doe"},"email":"john@example.com"}
    // Returns [fullName, email]
    private String[] parseUserParam(String userJson) {
        String fullName = null, email = null;
        try {
            JSONObject user = new JSONObject(userJson);
            email = user.optString("email", null);

            JSONObject name = user.optJSONObject("name");
            if (name != null) {
                String first = name.optString("firstName", "");
                String last  = name.optString("lastName", "");
                fullName = (first + " " + last).trim();
                if (fullName.isEmpty()) fullName = null;
            }
        } catch (JSONException ignored) { }
        return new String[]{fullName, email};
    }

    // Decodes the JWT payload and extracts the "sub" claim as userId
    private String extractSubFromJwt(String jwt) {
        try {
            String[] parts = jwt.split("\\.");
            if (parts.length < 2) return "";

            String payloadBase64 = parts[1];
            byte[] decodedBytes = Base64.getUrlDecoder().decode(payloadBase64);
            String payload = new String(decodedBytes, StandardCharsets.UTF_8);

            return new JSONObject(payload).optString("sub", "");
        } catch (Exception e) {
            return "";
        }
    }

    private void sendSuccess(String json) {
        if (mResultSent) return;
        mResultSent = true;
        UnityPlayer.UnitySendMessage(mCallbackObject, "OnSuccess", json);
    }

    private void sendFailure(String errorCode) {
        if (mResultSent) return;
        mResultSent = true;
        UnityPlayer.UnitySendMessage(mCallbackObject, "OnFailure", errorCode);
    }
}
