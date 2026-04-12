// TV static / CRT channel-change effect — from ryandebraal.com/index.htm
// Fires on 10% of page navigations (triggered from MainLayout.razor).
(function () {
  var overlay = null;

  window.__tvStaticShow = function () {
    if (!overlay) {
      overlay = document.createElement('canvas');
      overlay.id = 'crt-overlay';
      overlay.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;z-index:9998;pointer-events:none;opacity:1;';
      document.body.appendChild(overlay);
    }
    overlay.width = window.innerWidth;
    overlay.height = window.innerHeight;
    overlay.style.display = 'block';
    overlay.style.opacity = '1';
    var ctx = overlay.getContext('2d');
    var w = overlay.width, h = overlay.height;
    var start = performance.now();
    var duration = 300 + Math.floor(Math.random() * 150);
    var squeezeSpeed = 2 + Math.random() * 2;
    var tintR = Math.floor(Math.random() * 40);
    var tintG = Math.floor(Math.random() * 40);
    var tintB = Math.floor(Math.random() * 40);
    var hShiftChance = 0.02 + Math.random() * 0.06;
    var hShiftMax = 10 + Math.floor(Math.random() * 30);

    function drawStatic() {
      var elapsed = performance.now() - start;
      var pct = elapsed / duration;
      if (pct >= 1) {
        overlay.style.display = 'none';
        return;
      }
      var imgData = ctx.createImageData(w, h);
      var d = imgData.data;
      var fade = 1 - pct;
      var squeeze = Math.max(0, 1 - pct * squeezeSpeed);
      var barTop = Math.floor((1 - squeeze) * h * 0.5);
      var barBot = h - barTop;
      // random horizontal shift bands
      var shiftY = -1, shiftH = 0, shiftX = 0;
      if (Math.random() < hShiftChance) {
        shiftY = Math.floor(Math.random() * h);
        shiftH = 5 + Math.floor(Math.random() * 40);
        shiftX = Math.floor((Math.random() - 0.5) * hShiftMax * 2);
      }
      for (var i = 0; i < d.length; i += 4) {
        var px = (i / 4) | 0;
        var y = (px / w) | 0;
        if (y < barTop || y > barBot) {
          d[i] = d[i+1] = d[i+2] = 0;
          d[i+3] = Math.floor(255 * fade);
        } else {
          var v = (Math.random() * 255) | 0;
          d[i]   = Math.min(255, v + tintR);
          d[i+1] = Math.min(255, v + tintG);
          d[i+2] = Math.min(255, v + tintB);
          var opacity = y >= shiftY && y < shiftY + shiftH ? 240 : 200;
          d[i+3] = Math.floor(opacity * fade);
        }
      }
      ctx.putImageData(imgData, 0, 0);
      // horizontal distortion lines
      var lineCount = 2 + Math.floor(Math.random() * 8);
      for (var l = 0; l < lineCount; l++) {
        var ly = Math.floor(Math.random() * h);
        var lh = 1 + Math.floor(Math.random() * 4);
        var lop = (0.15 + Math.random() * 0.35) * fade;
        ctx.fillStyle = 'rgba(255,255,255,' + lop + ')';
        ctx.fillRect(0, ly, w, lh);
      }
      // occasional color flash band
      if (Math.random() < 0.15) {
        var bandY = Math.floor(Math.random() * h);
        var bandH = 2 + Math.floor(Math.random() * 8);
        var colors = ['rgba(255,0,0,','rgba(0,255,0,','rgba(0,0,255,','rgba(255,255,0,'];
        ctx.fillStyle = colors[Math.floor(Math.random() * colors.length)] + (0.2 * fade) + ')';
        ctx.fillRect(0, bandY, w, bandH);
      }
      requestAnimationFrame(drawStatic);
    }
    drawStatic();
  };
})();
