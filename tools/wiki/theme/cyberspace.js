/* MindAttic Cyberspace backdrop for MediaWiki, mounted the way mindattic.com mounts it: MindAttic.Components@4234f880055d3365651fcda2ca15ef6307c0d68a's
   circuit-board parallax, scan-lines and effects behind the page, a tap outside the page sparking one effect where
   you tapped. The engine is told to keep its effects out of the whole page column (header, side menus, article,
   footer); the side columns and footer are see-through, so the circuit board and scan-lines show there, and the
   effect layer is clipped out of the article itself, so nothing it draws can ever reach the text, wanderers included.
   Off for reduced motion and while editing.

   Book chapters (prose2wiki's .prose-book) get zen mode: the chapter alone on a dark or light page, remembered between
   chapters, with the arrow keys turning pages. */
( function () {
	var base = 'https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@4234f880055d3365651fcda2ca15ef6307c0d68a/web/components/';
	var root = document.documentElement;

	function store( key, value ) {
		try {
			if ( value === undefined ) {
				return window.localStorage.getItem( key );
			}
			window.localStorage.setItem( key, value );
		} catch ( e ) {}
		return null;
	}

	// ---- zen mode ---------------------------------------------------------------------------------------------------
	function backdropPaused( paused ) {
		var cb = window.consoleBg;
		if ( cb && cb.setAutoSpawn ) {
			try { cb.setAutoSpawn( !paused ); } catch ( e ) {}
		}
	}

	function setZen( on ) {
		root.classList.toggle( 'prose-zen', on );
		store( 'prose-zen', on ? '1' : '0' );
		backdropPaused( on );
		var b = document.querySelector( '.prose-zen-toggle .prose-zen-on' );
		if ( b ) {
			b.textContent = on ? 'Leave zen' : 'Zen mode';
		}
	}

	function setShade( light ) {
		root.classList.toggle( 'prose-zen-light', light );
		store( 'prose-zen-shade', light ? 'light' : 'dark' );
		var b = document.querySelector( '.prose-zen-toggle .prose-zen-shade' );
		if ( b ) {
			b.textContent = light ? 'Dark' : 'Light';
		}
	}

	function zenControls() {
		if ( !document.querySelector( '.prose-book' ) ) {
			return;
		}
		var bar = document.createElement( 'div' );
		bar.className = 'prose-zen-toggle';
		bar.innerHTML = '<button type="button" class="prose-zen-shade"></button><button type="button" class="prose-zen-on"></button>';
		document.body.appendChild( bar );
		bar.querySelector( '.prose-zen-on' ).addEventListener( 'click', function () {
			setZen( !root.classList.contains( 'prose-zen' ) );
		} );
		bar.querySelector( '.prose-zen-shade' ).addEventListener( 'click', function () {
			setShade( !root.classList.contains( 'prose-zen-light' ) );
		} );
		setShade( store( 'prose-zen-shade' ) === 'light' );
		setZen( store( 'prose-zen' ) === '1' );
		document.addEventListener( 'keydown', function ( e ) {
			if ( e.altKey || e.ctrlKey || e.metaKey || /^(INPUT|TEXTAREA|SELECT)$/.test( ( e.target && e.target.tagName ) || '' ) ) {
				return;
			}
			var go = e.key === 'ArrowLeft' ? '.prose-prev a' : e.key === 'ArrowRight' ? '.prose-next a' : null;
			if ( go && document.querySelector( go ) ) {
				window.location.href = document.querySelector( go ).href;
			} else if ( e.key === 'Escape' && root.classList.contains( 'prose-zen' ) ) {
				setZen( false );
			}
		} );
	}

	// A link can open a chapter in zen: ?zen=dark or ?zen=light (?zen=off leaves it).
	var asked = ( /[?&]zen=(dark|light|off)\b/.exec( window.location.search ) || [] )[ 1 ];
	if ( asked ) {
		store( 'prose-zen', asked === 'off' ? '0' : '1' );
		if ( asked !== 'off' ) {
			store( 'prose-zen-shade', asked );
		}
	}
	// Zen applies before the backdrop or anything else paints, so a reader in zen never sees the page flash.
	if ( store( 'prose-zen' ) === '1' && /\/\d+$/.test( mw.config.get( 'wgPageName' ) || '' ) && mw.config.get( 'wgNamespaceNumber' ) === 3000 ) {
		root.classList.add( 'prose-zen' );
		if ( store( 'prose-zen-shade' ) === 'light' ) {
			root.classList.add( 'prose-zen-light' );
		}
	}
	$( zenControls );

	// ---- the backdrop -----------------------------------------------------------------------------------------------
	var reduced = window.matchMedia && window.matchMedia( '(prefers-reduced-motion: reduce)' ).matches;
	var editing = /^(edit|submit)$/.test( mw.config.get( 'wgAction' ) );
	if ( reduced || editing || mw.config.get( 'skin' ) !== 'vector-2022' ) {
		return;
	}

	// Host settings console-bg.js reads when it loads (same as mindattic.com).
	window.__cyberspaceCircuitboardSrcs = [ 0, 1, 2 ].map( function ( n ) {
		return base + 'Cyberspace/assets/circuitboard.0' + n + '.png';
	} );
	var column = '.mw-page-container';
	var article = '.mw-body';
	window.__cyberspaceKeepoutSelectors = [
		column, '#content', '.vector-header-container', '.vector-column-start .vector-pinned-container',
		'.vector-column-end .vector-pinned-container', '.vector-sticky-header', '.mw-footer-container'
	].join( ', ' );

	mw.loader.load( base + 'Cyberspace/frontpage.css', 'text/css' );

	// The effect layer is clipped out of the article, so no effect is ever drawn over the text.
	function clipToMargins() {
		var host = document.querySelector( '.console-bg-host' );
		var page = document.querySelector( article ) || document.querySelector( column );
		if ( !host || !page ) {
			return;
		}
		var h = host.getBoundingClientRect(), r = page.getBoundingClientRect();
		var w = Math.ceil( h.width ), ht = Math.ceil( h.height );
		var l = Math.floor( r.left - h.left ), t = Math.floor( r.top - h.top );
		var rt = Math.ceil( r.right - h.left ), b = Math.ceil( r.bottom - h.top );
		host.style.clipPath = 'path(evenodd, "M0 0H' + w + 'V' + ht + 'H0Z M' + l + ' ' + t + 'H' + rt + 'V' + b + 'H' + l + 'Z")';
	}
	var clipQueued = false;
	function queueClip() {
		if ( !clipQueued ) {
			clipQueued = true;
			window.requestAnimationFrame( function () {
				clipQueued = false;
				clipToMargins();
			} );
		}
	}

	function inColumn( x, y ) {
		var page = document.querySelector( column );
		if ( !page ) {
			return false;
		}
		var r = page.getBoundingClientRect();
		return x >= r.left && x <= r.right && y >= r.top && y <= r.bottom;
	}

	function tapToSpawn() {
		var cb = window.consoleBg;
		var fx = cb && cb._demo;
		if ( !fx ) {
			return;
		}
		var effects = [ 'spawnError', 'spawnWarning', 'spawnWindow', 'spawnMemo', 'spawnFrag', 'spawnArtifact',
			'spawnGeoWindow', 'spawnNetConnect', 'spawnMorseDot', 'spawnFolderRip', 'spawnCascade',
			'spawnArtifactPredator', 'spawnHyperspaceReader' ];
		document.addEventListener( 'pointerdown', function ( e ) {
			if ( e.button > 0 || root.classList.contains( 'prose-zen' ) || inColumn( e.clientX, e.clientY ) ||
				( e.target.closest && e.target.closest( 'a, button, input, select, textarea, label, [role="button"]' ) ) ) {
				return;
			}
			var o = { x: e.clientX / window.innerWidth * 100, y: e.clientY / window.innerHeight * 100 };
			if ( cb.inKeepout && cb.inKeepout( o.x, o.y ) ) {
				return;
			}
			try {
				if ( cb.spawnSparkBurst ) {
					cb.spawnSparkBurst( o.x, o.y );
				}
			} catch ( err ) {}
			var order = effects.slice().sort( function () { return Math.random() - 0.5; } );
			for ( var i = 0; i < order.length; i++ ) {
				try {
					if ( typeof fx[ order[ i ] ] === 'function' && fx[ order[ i ] ]( o ) !== false ) {
						break;
					}
				} catch ( err ) {}
			}
		} );
	}

	$( function () {
		[ 'cyberspace-sl-fine', 'cyberspace-sl-coarse', 'console-bg-host' ].forEach( function ( cls ) {
			var layer = document.createElement( 'div' );
			layer.className = cls;
			layer.setAttribute( 'aria-hidden', 'true' );
			document.body.appendChild( layer );
		} );
		clipToMargins();
		window.addEventListener( 'resize', queueClip );
		window.addEventListener( 'scroll', queueClip, { passive: true } );
		if ( window.ResizeObserver ) {
			new window.ResizeObserver( queueClip ).observe( document.querySelector( article ) || document.body );
		}
		[ 'SacredGeometry/sacred-geometry.js', 'Hyperspace/hyperspace.js', 'Hyperspace/hyperspace-reader.js',
			'Cyberspace/console-bg.js' ].reduce( function ( chain, path ) {
			return chain.then( function () {
				return mw.loader.getScript( base + path );
			} );
		}, $.Deferred().resolve().promise() ).then( function () {
			clipToMargins();
			backdropPaused( root.classList.contains( 'prose-zen' ) );
			tapToSpawn();
		} );
	} );
}() );
