mergeInto(LibraryManager.library, {
  GetUserAgent: function () {
    var ua = navigator.userAgent;
    var buffer = Module._malloc(ua.length + 1);
    stringToUTF8(ua, buffer, ua.length + 1);
    return buffer;
  }
});