mergeInto(LibraryManager.library, {
    DetectPlatform: function () {
        var userAgent = navigator.userAgent || navigator.vendor || window.opera;

        // Check if the user is on Android
        if (/android/i.test(userAgent)) {
            return 1; // Android
        }

        // Check if the user is on iOS
        if (/iPad|iPhone|iPod/.test(userAgent) && !window.MSStream) {
            return 2; // iOS
        }

        // Default to desktop if neither Android nor iOS is detected
        return 0; // Desktop
    },
    GetSafeArea: function()
    {
        var div = document.createElement('div');
        div.style.padding = 'env(safe-area-inset-top) env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left)';
        document.body.appendChild(div);

        var style = window.getComputedStyle(div);
        var top = parseFloat(style.paddingTop) || 0;
        var right = parseFloat(style.paddingRight) || 0;
        var bottom = parseFloat(style.paddingBottom) || 0;
        var left = parseFloat(style.paddingLeft) || 0;

        document.body.removeChild(div);

        var result = top + "," + right + "," + bottom + "," + left;
        var bufferSize = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(result, buffer, bufferSize);
        return buffer;
    }

});