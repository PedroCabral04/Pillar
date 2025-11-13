// Responsive utilities for Pillar ERP
window.erpResponsive = {
    // Check if current viewport is mobile
    isMobile: function() {
        return window.innerWidth < 960;
    },
    
    // Check if current viewport is tablet
    isTablet: function() {
        return window.innerWidth >= 600 && window.innerWidth < 960;
    },
    
    // Check if current viewport is desktop
    isDesktop: function() {
        return window.innerWidth >= 960;
    },
    
    // Get current breakpoint
    getBreakpoint: function() {
        const width = window.innerWidth;
        if (width < 600) return 'xs';
        if (width < 960) return 'sm';
        if (width < 1280) return 'md';
        if (width < 1920) return 'lg';
        return 'xl';
    },
    
    // Setup resize listener with debounce
    onResize: function(dotNetHelper, method, delay = 250) {
        let timeoutId;
        const resizeHandler = () => {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                const breakpoint = this.getBreakpoint();
                const isMobile = this.isMobile();
                dotNetHelper.invokeMethodAsync(method, breakpoint, isMobile);
            }, delay);
        };
        
        window.addEventListener('resize', resizeHandler);
        
        // Return cleanup function
        return () => {
            window.removeEventListener('resize', resizeHandler);
        };
    },
    
    // Close drawer on mobile when clicking outside
    setupDrawerClickOutside: function(drawerId, closeCallback) {
        const drawer = document.getElementById(drawerId);
        if (!drawer) return;
        
        const clickHandler = (e) => {
            if (!this.isMobile()) return;
            
            // Check if click is outside drawer
            if (!drawer.contains(e.target) && drawer.classList.contains('mud-drawer--open')) {
                closeCallback();
            }
        };
        
        document.addEventListener('click', clickHandler);
        
        return () => {
            document.removeEventListener('click', clickHandler);
        };
    },
    
    // Enable swipe to close drawer on mobile
    enableSwipeToClose: function(drawerId, closeCallback) {
        if (!this.isMobile()) return;
        
        const drawer = document.getElementById(drawerId);
        if (!drawer) return;
        
        let touchStartX = 0;
        let touchEndX = 0;
        
        const handleSwipe = () => {
            const swipeDistance = touchEndX - touchStartX;
            // Swipe left to close (threshold: 50px)
            if (swipeDistance < -50) {
                closeCallback();
            }
        };
        
        drawer.addEventListener('touchstart', (e) => {
            touchStartX = e.changedTouches[0].screenX;
        });
        
        drawer.addEventListener('touchend', (e) => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        });
    },
    
    // Adjust viewport height for mobile browsers (addresses URL bar)
    setMobileViewportHeight: function() {
        if (!this.isMobile()) return;
        
        const setVH = () => {
            const vh = window.innerHeight * 0.01;
            document.documentElement.style.setProperty('--vh', `${vh}px`);
        };
        
        setVH();
        window.addEventListener('resize', setVH);
        window.addEventListener('orientationchange', setVH);
    },
    
    // Lock body scroll (useful when drawer is open on mobile)
    lockScroll: function() {
        document.body.style.overflow = 'hidden';
        document.body.style.position = 'fixed';
        document.body.style.width = '100%';
    },
    
    // Unlock body scroll
    unlockScroll: function() {
        document.body.style.overflow = '';
        document.body.style.position = '';
        document.body.style.width = '';
    },
    
    // Smooth scroll to element
    scrollToElement: function(elementId, offset = 0) {
        const element = document.getElementById(elementId);
        if (!element) return;
        
        const y = element.getBoundingClientRect().top + window.pageYOffset + offset;
        window.scrollTo({ top: y, behavior: 'smooth' });
    },
    
    // Check if device supports touch
    isTouchDevice: function() {
        return ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    },
    
    // Get safe area insets (for notch devices)
    getSafeAreaInsets: function() {
        const style = getComputedStyle(document.documentElement);
        return {
            top: parseInt(style.getPropertyValue('--sat') || 0),
            right: parseInt(style.getPropertyValue('--sar') || 0),
            bottom: parseInt(style.getPropertyValue('--sab') || 0),
            left: parseInt(style.getPropertyValue('--sal') || 0)
        };
    },
    
    // Initialize responsive features
    initialize: function() {
        this.setMobileViewportHeight();
        
        // Add touch class to body if touch device
        if (this.isTouchDevice()) {
            document.body.classList.add('touch-device');
        }
        
        // Add breakpoint class to body
        const updateBreakpointClass = () => {
            document.body.className = document.body.className.replace(/breakpoint-\w+/g, '');
            document.body.classList.add(`breakpoint-${this.getBreakpoint()}`);
        };
        
        updateBreakpointClass();
        window.addEventListener('resize', updateBreakpointClass);
    }
};

// Auto-initialize on load
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.erpResponsive.initialize();
    });
} else {
    window.erpResponsive.initialize();
}
