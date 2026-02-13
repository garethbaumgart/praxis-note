const isMac = typeof navigator !== 'undefined' && /Mac|iPhone|iPad/.test(navigator.platform);

/**
 * Formats a shortcut definition into a platform-native display string.
 * Input uses canonical keys: Mod, Shift, Alt, plus a key name.
 *
 * Mac output:    ⌘⇧D  (compact symbols, no separators)
 * Win/Linux:     Ctrl+Shift+D  (text labels with + separators)
 */
export function formatShortcut(parts: {
  mod?: boolean;
  shift?: boolean;
  alt?: boolean;
  key: string;
}): string {
  if (isMac) {
    let s = '';
    if (parts.mod) s += '\u2318';
    if (parts.alt) s += '\u2325';
    if (parts.shift) s += '\u21E7';
    s += parts.key.toUpperCase();
    return s;
  } else {
    const keys: string[] = [];
    if (parts.mod) keys.push('Ctrl');
    if (parts.alt) keys.push('Alt');
    if (parts.shift) keys.push('Shift');
    keys.push(parts.key.toUpperCase());
    return keys.join('+');
  }
}
