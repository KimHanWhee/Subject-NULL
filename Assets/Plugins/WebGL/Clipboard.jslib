// WebGL 클립보드 복사 — GUIUtility.systemCopyBuffer가 WebGL에서 동작하지 않아 브라우저 API 사용.
// 클릭 핸들러(사용자 제스처) 안에서 호출되므로 clipboard 권한 문제 없음.
mergeInto(LibraryManager.library, {
  JS_CopyToClipboard: function (ptr) {
    var text = UTF8ToString(ptr);
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).catch(function () {});
      return;
    }
    var ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    try { document.execCommand('copy'); } catch (e) {}
    document.body.removeChild(ta);
  }
});
