// Mobile Utilities for Pillar ERP
// Provides device detection, viewport fixes, touch gestures, and scroll locking

(function (window) {
    'use strict';

    // Create global erpResponsive object
    window.erpResponsive = window.erpResponsive || {};

    // ======================
    // Device Detection
    // ======================
    window.erpResponsive.getDeviceInfo = function () {
        const width = window.innerWidth;
        const height = window.innerHeight;
        const userAgent = navigator.userAgent || navigator.vendor || window.opera;

        // Detect device type
        const isIOS = /iPad|iPhone|iPod/.test(userAgent) && !window.MSStream;
        const isAndroid = /android/i.test(userAgent);
        const isMobile = width < 600;
        const isTablet = width >= 600 && width < 960;
        const isDesktop = width >= 960;

        // Detect touch capability
        const isTouchDevice = ('ontouchstart' in window) ||
            (navigator.maxTouchPoints > 0) ||
            (navigator.msMaxTouchPoints > 0);

        // Detect standalone mode (PWA)
        const isStandalone = window.matchMedia('(display-mode: standalone)').matches ||
            window.navigator.standalone === true;

        // Get breakpoint name
        let breakpoint = 'xs';
        if (width >= 1920) breakpoint = 'xl';
        else if (width >= 1280) breakpoint = 'lg';
        else if (width >= 960) breakpoint = 'md';
        else if (width >= 600) breakpoint = 'sm';

        return {
            width,
            height,
            isIOS,
            isAndroid,
            isMobile,
            isTablet,
            isDesktop,
            isTouchDevice,
            isStandalone,
            breakpoint,
            orientation: width > height ? 'landscape' : 'portrait'
        };
    };

    // ======================
    // Viewport Height Fix
    // ======================
    window.erpResponsive.fixViewportHeight = function () {
        // Fix for mobile browsers where viewport height changes with address bar
        const setVh = () => {
            const vh = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--vh', `${vh}px`);
        };

        setVh();
        window.addEventListener('resize', setVh);
        window.addEventListener('orientationchange', setVh);
    };

    // ======================
    // Scroll Locking
    // ======================
    let scrollY = 0;
    let scrollLocked = false;

    window.erpResponsive.lockScroll = function () {
        if (scrollLocked) return;

        scrollY = window.scrollY || window.pageYOffset;
        document.body.style.position = 'fixed';
        document.body.style.top = `-${scrollY}px`;
        document.body.style.width = '100%';
        document.body.style.overflow = 'hidden';
        scrollLocked = true;
    };

    window.erpResponsive.unlockScroll = function () {
        if (!scrollLocked) return;

        document.body.style.position = '';
        document.body.style.top = '';
        document.body.style.width = '';
        document.body.style.overflow = '';
        window.scrollTo(0, scrollY);
        scrollLocked = false;
    };

    // ======================
    // Touch Gesture Detection
    // ======================
    window.erpResponsive.setupSwipeGesture = function (elementId, callbacks) {
        const element = document.getElementById(elementId);
        if (!element) {
            console.warn(`Element with id '${elementId}' not found for swipe gesture`);
            return null;
        }

        let touchStartX = 0;
        let touchStartY = 0;
        let touchEndX = 0;
        let touchEndY = 0;

        const minSwipeDistance = 50; // pixels
        const maxVerticalDeviation = 100; // pixels

        const handleTouchStart = (e) => {
            touchStartX = e.changedTouches[0].screenX;
            touchStartY = e.changedTouches[0].screenY;
        };

        const handleTouchEnd = (e) => {
            touchEndX = e.changedTouches[0].screenX;
            touchEndY = e.changedTouches[0].screenY;
            handleGesture();
        };

        const handleGesture = () => {
            const deltaX = touchEndX - touchStartX;
            const deltaY = touchEndY - touchStartY;
            const absDeltaX = Math.abs(deltaX);
            const absDeltaY = Math.abs(deltaY);

            // Check if horizontal swipe
            if (absDeltaX > minSwipeDistance && absDeltaY < maxVerticalDeviation) {
                if (deltaX > 0 && callbacks.onSwipeRight) {
                    callbacks.onSwipeRight();
                } else if (deltaX < 0 && callbacks.onSwipeLeft) {
                    callbacks.onSwipeLeft();
                }
            }

            // Check if vertical swipe
            if (absDeltaY > minSwipeDistance && absDeltaX < maxVerticalDeviation) {
                if (deltaY > 0 && callbacks.onSwipeDown) {
                    callbacks.onSwipeDown();
                } else if (deltaY < 0 && callbacks.onSwipeUp) {
                    callbacks.onSwipeUp();
                }
            }
        };

        element.addEventListener('touchstart', handleTouchStart, false);
        element.addEventListener('touchend', handleTouchEnd, false);

        // Return cleanup function
        return () => {
            element.removeEventListener('touchstart', handleTouchStart);
            element.removeEventListener('touchend', handleTouchEnd);
        };
    };

    // ======================
    // Safe Area Detection
    // ======================
    window.erpResponsive.getSafeAreaInsets = function () {
        const computedStyle = getComputedStyle(document.documentElement);

        return {
            top: parseInt(computedStyle.getPropertyValue('--sai-top') || '0'),
            right: parseInt(computedStyle.getPropertyValue('--sai-right') || '0'),
            bottom: parseInt(computedStyle.getPropertyValue('--sai-bottom') || '0'),
            left: parseInt(computedStyle.getPropertyValue('--sai-left') || '0')
        };
    };

    // ======================
    // Vibration (if supported)
    // ======================
    window.erpResponsive.vibrate = function (pattern) {
        if ('vibrate' in navigator) {
            navigator.vibrate(pattern);
        }
    };

    // ======================
    // Breakpoint Change Observer
    // ======================
    window.erpResponsive.onBreakpointChange = function (callback) {
        let currentBreakpoint = window.erpResponsive.getDeviceInfo().breakpoint;

        const checkBreakpoint = () => {
            const newBreakpoint = window.erpResponsive.getDeviceInfo().breakpoint;
            if (newBreakpoint !== currentBreakpoint) {
                currentBreakpoint = newBreakpoint;
                callback(newBreakpoint);
            }
        };

        window.addEventListener('resize', checkBreakpoint);
        window.addEventListener('orientationchange', checkBreakpoint);

        // Return cleanup function
        return () => {
            window.removeEventListener('resize', checkBreakpoint);
            window.removeEventListener('orientationchange', checkBreakpoint);
        };
    };

    // ======================
    // Touch Feedback
    // ======================
    window.erpResponsive.addTouchFeedback = function (elementId) {
        const element = document.getElementById(elementId);
        if (!element) return null;

        const handleTouchStart = () => {
            element.classList.add('touch-active');
        };

        const handleTouchEnd = () => {
            element.classList.remove('touch-active');
        };

        element.addEventListener('touchstart', handleTouchStart, { passive: true });
        element.addEventListener('touchend', handleTouchEnd, { passive: true });
        element.addEventListener('touchcancel', handleTouchEnd, { passive: true });

        // Return cleanup function
        return () => {
            element.removeEventListener('touchstart', handleTouchStart);
            element.removeEventListener('touchend', handleTouchEnd);
            element.removeEventListener('touchcancel', handleTouchEnd);
        };
    };

    // ======================
    // Initialization
    // ======================
    window.erpResponsive.init = function () {
        // Fix viewport height on load
        window.erpResponsive.fixViewportHeight();

        // Set safe area insets as CSS variables
        const updateSafeAreaInsets = () => {
            document.documentElement.style.setProperty('--sai-top', 'env(safe-area-inset-top, 0px)');
            document.documentElement.style.setProperty('--sai-right', 'env(safe-area-inset-right, 0px)');
            document.documentElement.style.setProperty('--sai-bottom', 'env(safe-area-inset-bottom, 0px)');
            document.documentElement.style.setProperty('--sai-left', 'env(safe-area-inset-left, 0px)');
        };

        updateSafeAreaInsets();

        console.log('Pillar ERP Mobile Utils initialized', window.erpResponsive.getDeviceInfo());
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', window.erpResponsive.init);
    } else {
        window.erpResponsive.init();
    }

})(window);
