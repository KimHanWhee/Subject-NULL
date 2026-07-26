// PayPal 결제 브리지 — WebGL 전용.
// 흐름: C#이 서버에서 받은 orderId로 PP_Pay 호출 → PayPal JS SDK 결제창(팝업) →
//       사용자가 승인/취소 → SendMessage로 C#에 결과 통지(확정은 서버가 수행).
//
// 주의: SDK는 index.html에서 client-id와 함께 로드된다(템플릿). 여기서는 로드 여부만 확인한다.
mergeInto(LibraryManager.library, {
  // goName: SendMessage 대상 GameObject 이름
  PP_Setup: function (goNamePtr) {
    window.__ppGo = UTF8ToString(goNamePtr);
    window.__ppReady = !!(window.paypal && window.paypal.Buttons);
    if (!window.__ppReady) {
      // SDK가 늦게 로드될 수 있으므로 잠시 폴링
      var tries = 0;
      var wait = function () {
        tries++;
        if (window.paypal && window.paypal.Buttons) { window.__ppReady = true; return; }
        if (tries < 40) setTimeout(wait, 300);
      };
      wait();
    }
  },

  // 서버가 만든 orderId로 결제창을 띄운다.
  PP_Pay: function (orderIdPtr) {
    var orderId = UTF8ToString(orderIdPtr);
    var go = window.__ppGo;

    if (!(window.paypal && window.paypal.Buttons)) {
      SendMessage(go, "OnPaypalError", "sdk-not-loaded");
      return;
    }

    var old = document.getElementById("pp-overlay");
    if (old) old.remove();

    var ov = document.createElement("div");
    ov.id = "pp-overlay";
    ov.style.cssText = "position:fixed;inset:0;background:rgba(0,0,0,.72);display:flex;align-items:center;justify-content:center;z-index:10000;";

    var card = document.createElement("div");
    card.style.cssText = "background:#fff;border-radius:12px;padding:24px 28px;min-width:320px;display:flex;flex-direction:column;align-items:center;gap:14px;font-family:sans-serif;";

    var title = document.createElement("div");
    title.textContent = "GEM 충전";
    title.style.cssText = "color:#222;font-size:16px;font-weight:600;";

    var host = document.createElement("div");
    host.style.cssText = "width:280px;";

    var close = document.createElement("div");
    close.textContent = "취소";
    close.style.cssText = "color:#888;font-size:13px;cursor:pointer;text-decoration:underline;";
    close.onclick = function () {
      ov.remove();
      SendMessage(go, "OnPaypalError", "cancelled");
    };

    card.appendChild(title);
    card.appendChild(host);
    card.appendChild(close);
    ov.appendChild(card);
    document.body.appendChild(ov);

    window.paypal.Buttons({
      // 서버에서 이미 만든 주문을 그대로 사용(금액은 서버가 확정 — 클라 위조 불가)
      createOrder: function () { return orderId; },
      onApprove: function (data) {
        ov.remove();
        SendMessage(go, "OnPaypalApproved", data.orderID || orderId);
      },
      onCancel: function () {
        ov.remove();
        SendMessage(go, "OnPaypalError", "cancelled");
      },
      onError: function (err) {
        ov.remove();
        SendMessage(go, "OnPaypalError", String(err));
      }
    }).render(host).catch(function (e) {
      ov.remove();
      SendMessage(go, "OnPaypalError", "render-failed: " + e);
    });
  },

  PP_Hide: function () {
    var o = document.getElementById("pp-overlay");
    if (o) o.remove();
  }
});
