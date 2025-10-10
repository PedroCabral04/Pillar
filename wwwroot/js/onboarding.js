window.onboardingHelper = {
    scrollToElement: function (selector) {
        const element = document.querySelector(selector);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return true;
        }
        return false;
    },

    getElementPosition: function (selector) {
        const element = document.querySelector(selector);
        if (element) {
            const rect = element.getBoundingClientRect();
            return {
                top: rect.top,
                left: rect.left,
                width: rect.width,
                height: rect.height,
                bottom: rect.bottom,
                right: rect.right
            };
        }
        return null;
    },

    highlightElement: function (selector) {
        const element = document.querySelector(selector);
        if (element) {
            element.classList.add('onboarding-highlight');
            return true;
        }
        return false;
    },

    removeHighlight: function (selector) {
        const element = document.querySelector(selector);
        if (element) {
            element.classList.remove('onboarding-highlight');
            return true;
        }
        return false;
    },

    focusElement: function (selector) {
        const element = document.querySelector(selector);
        if (element && typeof element.focus === 'function') {
            element.focus();
            return true;
        }
        return false;
    }
};
