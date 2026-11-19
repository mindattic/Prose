/* MindAttic Cyberspace backdrop for MediaWiki, mounted the way mindattic.com mounts it: MindAttic.Components@4234f880055d3365651fcda2ca15ef6307c0d68a's
   circuit-board parallax, scan-lines and effects behind the page, a tap outside the content sparking one effect where
   you tapped. The article (<main>), the header, the pinned side columns and the footer are keep-out zones, so no
   effect spawns over what you read or click. Off for reduced motion and while editing. */
( function () {
	var base = 'https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@4234f880055d3365651fcda2ca15ef6307c0d68a/web/components/';
	var reduced = window.matchMedia && window.matchMedia( '(prefers-reduced-motion: reduce)' ).matches;
	var editing = /^(edit|submit)$/.test( mw.config.get( 'wgAction' ) );
	if ( reduced || editing || mw.config.get( 'skin' ) !== 'vector-2022' ) {
		return;
	}

	// Host settings console-bg.js reads when it loads (same as mindattic.com).
	window.__cyberspaceCircuitboardSrcs = [ 0, 1, 2 ].map( function ( n ) {
		return base + 'Cyberspace/assets/circuitboard.0' + n + '.png';
	} );
	window.__cyberspaceKeepoutSelectors = [
		'#content', '.vector-header-container', '.vector-column-start .vector-pinned-container',
		'.vector-column-end .vector-pinned-container', '.vector-sticky-header', '.mw-footer-container'
	].join( ', ' );

	mw.loader.load( base + 'Cyberspace/frontpage.css', 'text/css' );

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
			if ( e.button > 0 || ( e.target.closest && e.target.closest( 'a, button, input, select, textarea, label, [role="button"]' ) ) ) {
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
		[ 'SacredGeometry/sacred-geometry.js', 'Hyperspace/hyperspace.js', 'Hyperspace/hyperspace-reader.js',
			'Cyberspace/console-bg.js' ].reduce( function ( chain, path ) {
			return chain.then( function () {
				return mw.loader.getScript( base + path );
			} );
		}, $.Deferred().resolve().promise() ).then( tapToSpawn );
	} );
}() );
