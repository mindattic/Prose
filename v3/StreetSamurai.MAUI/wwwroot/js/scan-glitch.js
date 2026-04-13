// Scan-line glitch boxes — a single blurry unicode character that
// drifts in anywhere on screen, holds for a moment, then fades out.

(function () {
    'use strict';

    function rand(lo, hi) { return Math.floor(Math.random() * (hi - lo + 1)) + lo; }
    function pick(str)    { return str[Math.floor(Math.random() * str.length)]; }

    // Rich pool: geometric, mathematical, box-drawing, script fragments, misc
    var CHARS =
        '░▒▓█▌▐▀▄■□▪▫◆◇○●◉◎⊗⊕⊙∅' +
        '∞≠≈∫∂∆Ωπμλφψξζ' +
        '⌂⌀⌘⌬⌭⌫' +
        '✦✧✩✫✭✯✱✲✳✴✵✶✷✸✹✺✻✼' +
        '⬡⬢⬠⬟⬜⬝' +
        '⠿⠻⠷⠾⠽⠯⠫⠳' +
        '₿€¥₩₹Φ₽₼₺₴₦' +
        '←→↑↓↔↕⇐⇒⇑⇓⇔⟵⟶⟷' +
        'アイウエオカキクケコサシスセソタチツテトナニヌネノ' +
        'ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩαβγδεζηθ' +
        '龍炎風水土金木火';

    function spawnBatch() {
        var n = Math.random() < 0.35 ? rand(2, 4) : 1;
        for (var i = 0; i < n; i++) {
            (function () {
                var el = document.createElement('div');
                el.className   = 'sl-glitch-box';
                el.style.left  = rand(1, 93) + 'vw';
                el.style.top   = rand(1, 91) + 'vh';
                el.style.fontSize = rand(12, 22) + 'px';
                el.style.filter   = 'blur(' + (0.8 + Math.random() * 2.2).toFixed(1) + 'px)';
                el.textContent = pick(CHARS);
                document.body.appendChild(el);
                setTimeout(function () {
                    el.classList.add('sl-glitch-box--out');
                    setTimeout(function () {
                        if (el.parentNode) el.parentNode.removeChild(el);
                    }, 620);
                }, rand(400, 2000));
            })();
        }
        setTimeout(spawnBatch, rand(700, 3200));
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', spawnBatch);
    } else {
        spawnBatch();
    }
})();
