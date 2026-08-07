window.proseSpeech = {
    listVoices: function () {
        return new Promise(function (resolve) {
            var voices = speechSynthesis.getVoices();
            if (voices && voices.length > 0) {
                resolve(voices.map(function (v) { return { name: v.name, lang: v.lang }; }));
                return;
            }
            var timeout = setTimeout(function () {
                var v2 = speechSynthesis.getVoices();
                resolve((v2 || []).map(function (v) { return { name: v.name, lang: v.lang }; }));
            }, 500);
            if (speechSynthesis.onvoiceschanged !== undefined) {
                speechSynthesis.onvoiceschanged = function () {
                    clearTimeout(timeout);
                    var v3 = speechSynthesis.getVoices();
                    resolve((v3 || []).map(function (v) { return { name: v.name, lang: v.lang }; }));
                };
            }
        });
    },
    speak: function (chunks, voiceName, rate) {
        speechSynthesis.cancel();
        var voices = speechSynthesis.getVoices();
        var selectedVoice = null;
        if (voiceName) {
            for (var i = 0; i < voices.length; i++) {
                if (voices[i].name === voiceName) { selectedVoice = voices[i]; break; }
            }
        }
        var items = Array.isArray(chunks) ? chunks : [chunks];
        for (var j = 0; j < items.length; j++) {
            var text = items[j];
            if (!text || text.trim().length === 0) continue;
            var u = new SpeechSynthesisUtterance(text);
            if (selectedVoice) u.voice = selectedVoice;
            u.rate = typeof rate === 'number' && rate > 0 ? rate : 1.0;
            speechSynthesis.speak(u);
        }
        return true;
    },
    stop: function () {
        speechSynthesis.cancel();
    }
};
