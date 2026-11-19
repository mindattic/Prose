<?php
# Appended to the wiki's LocalSettings.php (by hand, over FTP) for a prose2wiki universe wiki. No secrets here:
# the database password and keys stay in the server copy only. Replace glmz-logo.png with the universe's logo file,
# uploaded to resources/assets/.

# ---- prose2wiki (Prose tools/wiki, PRS-T1038/T1039/T1040) -------------------------------------------------
# The GLMZ logo (from the owner), uploaded by FTP next to MediaWiki's own assets.
$wgLogos = [
	'1x' => "$wgResourceBasePath/resources/assets/glmz-logo.png",
	'icon' => "$wgResourceBasePath/resources/assets/glmz-logo.png",
];
# Book covers and the books' publish files (.pdf, .docx, .epub, .md, .txt) are uploaded by the sync.
# Only administrators upload (the sync's bot account is one); readers just download.
$wgEnableUploads = true;
$wgFileExtensions = array_values( array_unique( array_merge( $wgFileExtensions, [ 'png', 'gif', 'jpg', 'jpeg', 'webp', 'pdf', 'docx', 'epub', 'md', 'txt' ] ) ) );
$wgGroupPermissions['user']['upload'] = false;
$wgGroupPermissions['user']['reupload'] = false;
$wgGroupPermissions['user']['reupload-shared'] = false;
$wgGroupPermissions['sysop']['upload'] = true;
$wgGroupPermissions['sysop']['reupload'] = true;
$wgMaxUploadSize = 100 * 1024 * 1024;
# .md and .epub have no entry in MediaWiki's MIME map; the sync is the only uploader.
$wgVerifyMimeType = false;
# The books: Book:<Title> and its chapters Book:<Title>/<n>, searched and counted like articles.
define( 'NS_BOOK', 3000 );
define( 'NS_BOOK_TALK', 3001 );
$wgExtraNamespaces[NS_BOOK] = 'Book';
$wgExtraNamespaces[NS_BOOK_TALK] = 'Book_talk';
$wgNamespacesWithSubpages[NS_BOOK] = true;
$wgContentNamespaces[] = NS_BOOK;
$wgNamespacesToBeSearchedDefault[NS_BOOK] = true;
# Chapter and book pages show their real titles ("Chapter 1 - The Cot", the book title in italics).
$wgRestrictDisplayTitle = false;
# One look: the Cyberspace theme. Vector's own Automatic/Light/Dark "Color (beta)" choice is off (PRS-T1051).
$wgVectorNightMode = [ 'logged_in' => false, 'logged_out' => false, 'beta' => false ];
$wgDefaultUserOptions['vector-theme'] = 'day';
