// Responsive utilities for Pillar ERP
(function (window) {
    'use strict';

    const responsive = window.erpResponsive = window.erpResponsive || {};

    const isTouchDevice = () => ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);

    const getBreakpoint = () => {
        const width = window.innerWidth;
        if (width < 600) return 'xs';
        if (width < 960) return 'sm';
        if (width < 1280) return 'md';
        if (width < 1920) return 'lg';
        return 'xl';
    };

    const getDeviceInfo = () => {
        const width = window.innerWidth;
        const height = window.innerHeight;
        const userAgent = navigator.userAgent || navigator.vendor || window.opera;

        return {
            width,
            height,
            isIOS: /iPad|iPhone|iPod/.test(userAgent) && !window.MSStream,
            isAndroid: /android/i.test(userAgent),
            isMobile: width < 960,
            isTablet: width >= 600 && width < 960,
            isDesktop: width >= 960,
            isTouchDevice: isTouchDevice(),
            isStandalone: window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true,
            breakpoint: getBreakpoint(),
            orientation: width > height ? 'landscape' : 'portrait'
        };
    };

    responsive.isMobile = responsive.isMobile || function () {
        return window.innerWidth < 960;
    };

    responsive.isTablet = responsive.isTablet || function () {
        return window.innerWidth >= 600 && window.innerWidth < 960;
    };

    responsive.isDesktop = responsive.isDesktop || function () {
        return window.innerWidth >= 960;
    };

    responsive.getBreakpoint = getBreakpoint;
    responsive.getDeviceInfo = getDeviceInfo;

    responsive.onResize = function (dotNetHelper, method, delay = 250) {
        let timeoutId;
        const resizeHandler = () => {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                dotNetHelper.invokeMethodAsync(method, getBreakpoint(), responsive.isMobile());
            }, delay);
        };

        window.addEventListener('resize', resizeHandler);

        return () => {
            window.removeEventListener('resize', resizeHandler);
        };
    };

    responsive.setupDrawerClickOutside = function (drawerId, closeCallback) {
        const drawer = document.getElementById(drawerId);
        if (!drawer) return null;

        const clickHandler = (event) => {
            if (!responsive.isMobile()) return;
            if (!drawer.contains(event.target) && drawer.classList.contains('mud-drawer--open')) {
                closeCallback();
            }
        };

        document.addEventListener('click', clickHandler);

        return () => {
            document.removeEventListener('click', clickHandler);
        };
    };

    responsive.enableSwipeToClose = function (drawerId, closeCallback) {
        if (!responsive.isMobile()) return () => {};

        const drawer = document.getElementById(drawerId);
        if (!drawer) return () => {};

        let touchStartX = 0;
        let touchStartY = 0;
        let isDragging = false;

        const touchStart = (event) => {
            touchStartX = event.changedTouches[0].screenX;
            touchStartY = event.changedTouches[0].screenY;
            isDragging = true;
        };

        const touchMove = (event) => {
            if (!isDragging) return;
            const currentX = event.changedTouches[0].screenX;
            const deltaX = touchStartX - currentX;

            if (deltaX > 0) {
                drawer.style.transform = `translateX(-${Math.min(deltaX, 280)}px)`;
            }
        };

        const touchEnd = (event) => {
            if (!isDragging) return;
            isDragging = false;

            const touchEndX = event.changedTouches[0].screenX;
            const touchEndY = event.changedTouches[0].screenY;
            const swipeDistance = touchStartX - touchEndX;
            const verticalDistance = Math.abs(touchStartY - touchEndY);

            drawer.style.transform = '';

            if (swipeDistance > 50 && verticalDistance < 50) {
                if (typeof closeCallback === 'function') {
                    closeCallback();
                }
            }
        };

        drawer.addEventListener('touchstart', touchStart, { passive: true });
        drawer.addEventListener('touchmove', touchMove, { passive: true });
        drawer.addEventListener('touchend', touchEnd, { passive: true });

        return () => {
            drawer.removeEventListener('touchstart', touchStart);
            drawer.removeEventListener('touchmove', touchMove);
            drawer.removeEventListener('touchend', touchEnd);
        };
    };

    responsive.setMobileViewportHeight = responsive.setMobileViewportHeight || function () {
        const setVH = () => {
            const vh = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--vh', `${vh}px`);
        };

        setVH();
        window.addEventListener('resize', setVH);
        window.addEventListener('orientationchange', setVH);
    };

    responsive.lockScroll = responsive.lockScroll || function () {
        document.body.style.overflow = 'hidden';
        document.body.style.position = 'fixed';
        document.body.style.width = '100%';
    };

    responsive.unlockScroll = responsive.unlockScroll || function () {
        document.body.style.overflow = '';
        document.body.style.position = '';
        document.body.style.width = '';
    };

    responsive.scrollToElement = function (elementId, offset = 0) {
        const element = document.getElementById(elementId);
        if (!element) return;

        const y = element.getBoundingClientRect().top + window.pageYOffset + offset;
        window.scrollTo({ top: y, behavior: 'smooth' });
    };

    responsive.isTouchDevice = responsive.isTouchDevice || isTouchDevice;

    responsive.getSafeAreaInsets = responsive.getSafeAreaInsets || function () {
        const style = getComputedStyle(document.documentElement);
        return {
            top: parseInt(style.getPropertyValue('--sai-top') || '0', 10),
            right: parseInt(style.getPropertyValue('--sai-right') || '0', 10),
            bottom: parseInt(style.getPropertyValue('--sai-bottom') || '0', 10),
            left: parseInt(style.getPropertyValue('--sai-left') || '0', 10)
        };
    };

    responsive.initHapticFeedback = responsive.initHapticFeedback || function () {
        if (!responsive.isTouchDevice() || !('vibrate' in navigator)) return;

        document.addEventListener('touchstart', (event) => {
            const target = event.target.closest('.mud-button-root, .mud-icon-button, .bottom-nav-item');
            if (target) {
                navigator.vibrate(10);
            }
        }, { passive: true });
    };

    responsive.preventOverscroll = function (element) {
        if (!element) return;

        let startY = 0;
        element.addEventListener('touchstart', (event) => {
            startY = event.touches[0].pageY;
        }, { passive: true });

        element.addEventListener('touchmove', (event) => {
            const y = event.touches[0].pageY;
            const scrollTop = element.scrollTop;
            const scrollHeight = element.scrollHeight;
            const offsetHeight = element.offsetHeight;

            const atTop = scrollTop <= 0;
            const atBottom = scrollTop + offsetHeight >= scrollHeight;

            if ((atTop && y > startY) || (atBottom && y < startY)) {
                event.preventDefault();
            }
        }, { passive: false });
    };

    responsive.setFontSizeClass = function (fontSize) {
        document.body.classList.remove('font-size-base', 'font-size-large', 'font-size-small');

        if (fontSize === 'large') {
            document.body.classList.add('font-size-large');
        } else if (fontSize === 'small') {
            document.body.classList.add('font-size-small');
        } else {
            document.body.classList.add('font-size-base');
        }

        document.body.offsetHeight;
    };

    responsive.initialize = responsive.initialize || function () {
        responsive.setMobileViewportHeight();

        if (responsive.isTouchDevice()) {
            document.body.classList.add('touch-device');
        }

        const updateBreakpointClass = () => {
            const classesToRemove = [];
            document.body.classList.forEach((className) => {
                if (className.startsWith('breakpoint-')) {
                    classesToRemove.push(className);
                }
            });

            classesToRemove.forEach((className) => document.body.classList.remove(className));
            document.body.classList.add(`breakpoint-${getBreakpoint()}`);

            if (responsive.isMobile()) {
                document.body.classList.add('is-mobile');
                document.body.classList.remove('is-desktop');
            } else {
                document.body.classList.add('is-desktop');
                document.body.classList.remove('is-mobile');
            }
        };

        updateBreakpointClass();
        window.addEventListener('resize', updateBreakpointClass);

        responsive.initHapticFeedback();
    };

    if (!responsive._responsiveInitialized) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => responsive.initialize());
        } else {
            responsive.initialize();
        }
        responsive._responsiveInitialized = true;
    }
})(window);
