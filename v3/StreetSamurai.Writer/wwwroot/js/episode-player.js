// Episode audio player — JS interop wrappers for the <audio> element on /listen.
// Blazor controls play/pause/seek through these helpers; the page-side C# code
// owns the virtual-timeline math.

(function () {
    function el(id) { return document.getElementById(id); }

    window.episodePlayer = {
        load(audioId, src, autoplay) {
            const a = el(audioId);
            if (!a) return;
            if (a.src !== src) a.src = src;
            if (autoplay) a.play().catch(() => { /* user gesture required, ignore */ });
        },

        play(audioId) {
            const a = el(audioId);
            if (!a) return false;
            a.play().catch(() => { });
            return !a.paused;
        },

        pause(audioId) {
            const a = el(audioId);
            if (!a) return false;
            a.pause();
            return !a.paused;
        },

        toggle(audioId) {
            const a = el(audioId);
            if (!a) return false;
            if (a.paused) {
                a.play().catch(() => { });
                return true;
            }
            a.pause();
            return false;
        },

        getCurrentTime(audioId) {
            const a = el(audioId);
            return a ? (a.currentTime || 0) : 0;
        },

        getDuration(audioId) {
            const a = el(audioId);
            return a ? (a.duration || 0) : 0;
        },

        isPlaying(audioId) {
            const a = el(audioId);
            return a ? !a.paused : false;
        },

        seek(audioId, sec) {
            const a = el(audioId);
            if (!a) return;
            a.currentTime = Math.max(0, sec);
        },
    };
})();
