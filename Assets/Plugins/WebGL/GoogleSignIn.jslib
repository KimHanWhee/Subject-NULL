// Google Identity Services(GIS) 브리지 — WebGL 전용.
// 템플릿에서 https://accounts.google.com/gsi/client 를 로드해야 동작한다.
// 흐름: GSI_Setup(초기화) → GSI_Show(오버레이+공식 버튼) → 사용자가 버튼 클릭
//       → GIS 콜백의 credential(JWT id_token)을 SendMessage로 C#에 전달.
mergeInto(LibraryManager.library, {
  GSI_Setup: function (clientIdPtr, goNamePtr) {
    var clientId = UTF8ToString(clientIdPtr);
    window.__gsiGo = UTF8ToString(goNamePtr);
    var tryInit = function () {
      if (!(window.google && google.accounts && google.accounts.id)) { setTimeout(tryInit, 300); return; }
      google.accounts.id.initialize({
        client_id: clientId,
        callback: function (resp) {
          var host = document.getElementById("gsi-overlay");
          if (host) host.remove();
          if (resp && resp.credential) SendMessage(window.__gsiGo, "OnGoogleIdToken", resp.credential);
          else SendMessage(window.__gsiGo, "OnGoogleError", "empty-credential");
        }
      });
      window.__gsiReady = true;
    };
    tryInit();
  },

  GSI_Show: function () {
    if (!window.__gsiReady) { SendMessage(window.__gsiGo, "OnGoogleError", "not-ready"); return; }
    var old = document.getElementById("gsi-overlay");
    if (old) old.remove();
    var ov = document.createElement("div");
    ov.id = "gsi-overlay";
    ov.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,.65);display:flex;align-items:center;justify-content:center;z-index:10000;";
    var card = document.createElement("div");
    card.style.cssText = "background:#fff;border-radius:12px;padding:28px 32px;display:flex;flex-direction:column;align-items:center;gap:16px;font-family:sans-serif;";
    var title = document.createElement("div");
    title.textContent = "Google 계정으로 로그인"; // "Google 계정으로 로그인"
    title.style.cssText = "color:#333;font-size:15px;";
    var btnHost = document.createElement("div");
    var close = document.createElement("div");
    close.textContent = "닫기"; // "닫기"
    close.style.cssText = "color:#888;font-size:13px;cursor:pointer;margin-top:4px;text-decoration:underline;";
    close.onclick = function () {
      ov.remove();
      SendMessage(window.__gsiGo, "OnGoogleError", "cancelled");
    };
    card.appendChild(title);
    card.appendChild(btnHost);
    card.appendChild(close);
    ov.appendChild(card);
    document.body.appendChild(ov);
    google.accounts.id.renderButton(btnHost, { theme: "outline", size: "large", text: "signin_with", width: 280 });
  },

  GSI_Hide: function () {
    var o = document.getElementById("gsi-overlay");
    if (o) o.remove();
  }
});
