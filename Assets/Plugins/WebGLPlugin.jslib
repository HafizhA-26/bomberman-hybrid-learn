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
    }
});