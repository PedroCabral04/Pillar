window.erpNotifications = (function () {
    let audioContext = null;
    let cachedBuffers = {};

    async function getAudioContext() {
        if (!audioContext) {
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }
        // Resume context if suspended (browsers require user interaction)
        if (audioContext.state === 'suspended') {
            await audioContext.resume();
        }
        return audioContext;
    }

    return {
        playSound: async function (soundUrl, volume = 0.7) {
            try {
                const ctx = await getAudioContext();
                
                // Try to use cached buffer
                if (!cachedBuffers[soundUrl]) {
                    const response = await fetch(soundUrl);
                    if (!response.ok) {
                        console.warn('Notification sound not found:', soundUrl);
                        return;
                    }
                    const arrayBuffer = await response.arrayBuffer();
                    cachedBuffers[soundUrl] = await ctx.decodeAudioData(arrayBuffer);
                }

                const source = ctx.createBufferSource();
                source.buffer = cachedBuffers[soundUrl];

                const gainNode = ctx.createGain();
                gainNode.gain.value = Math.max(0, Math.min(1, volume));

                source.connect(gainNode);
                gainNode.connect(ctx.destination);
                source.start(0);
            } catch (err) {
                console.warn('Failed to play notification sound:', err);
            }
        },

        preloadSound: async function (soundUrl) {
            try {
                const ctx = await getAudioContext();
                if (!cachedBuffers[soundUrl]) {
                    const response = await fetch(soundUrl);
                    if (response.ok) {
                        const arrayBuffer = await response.arrayBuffer();
                        cachedBuffers[soundUrl] = await ctx.decodeAudioData(arrayBuffer);
                    }
                }
            } catch (err) {
                console.warn('Failed to preload notification sound:', err);
            }
        },

        testSound: async function (volume = 0.7) {
            // Play a simple beep for testing
            try {
                const ctx = await getAudioContext();
                const oscillator = ctx.createOscillator();
                const gainNode = ctx.createGain();

                oscillator.type = 'sine';
                oscillator.frequency.value = 440; // A4 note
                gainNode.gain.value = Math.max(0, Math.min(1, volume));

                oscillator.connect(gainNode);
                gainNode.connect(ctx.destination);

                oscillator.start();
                setTimeout(() => oscillator.stop(), 200);
            } catch (err) {
                console.warn('Failed to play test sound:', err);
            }
        }
    };
})();
