/* MindAttic Cyberspace backdrop for MediaWiki: mounts MindAttic.Components@V4's Cyberspace engine behind the page.
   The article is a <main>, which the engine keeps its effects out of. Skipped for reduced motion, print and editing. */
( function () {
	var base = 'https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V4/web/components/';
	var reduced = window.matchMedia && window.matchMedia( '(prefers-reduced-motion: reduce)' ).matches;
	var editing = /^(edit|submit)$/.test( mw.config.get( 'wgAction' ) );
	if ( reduced || editing || mw.config.get( 'skin' ) !== 'vector-2022' ) {
		return;
	}
	mw.loader.load( base + 'Cyberspace/frontpage.css', 'text/css' );
	$( function () {
		[ 'cyberspace-sl-fine', 'cyberspace-sl-coarse', 'console-bg-host' ].forEach( function ( cls ) {
			var layer = document.createElement( 'div' );
			layer.className = cls;
			layer.setAttribute( 'aria-hidden', 'true' );
			document.body.appendChild( layer );
		} );
		var scripts = [ 'SacredGeometry/sacred-geometry.js', 'Hyperspace/hyperspace.js',
			'Hyperspace/hyperspace-reader.js', 'Cyberspace/console-bg.js' ];
		scripts.reduce( function ( chain, path ) {
			return chain.then( function () {
				return mw.loader.getScript( base + path );
			} );
		}, $.Deferred().resolve().promise() );
	} );
}() );
