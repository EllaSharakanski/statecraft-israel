// Whether the page is hidden (a background tab, a minimised window or one behind others): QualityFallbackDriver does not
// measure frames then, since the browser throttles them (issue #216).
mergeInto(LibraryManager.library, {
  StatecraftDocumentHidden: function () { return typeof document !== "undefined" && document.hidden ? 1 : 0; },
});
