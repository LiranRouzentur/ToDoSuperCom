// Animation constants and utilities

// Timing functions
export const TRANSITIONS = {
  fast: '150ms cubic-bezier(0.4, 0, 0.2, 1)',
  normal: '300ms cubic-bezier(0.4, 0, 0.2, 1)',
  slow: '500ms cubic-bezier(0.4, 0, 0.2, 1)',
  spring: '400ms cubic-bezier(0.34, 1.56, 0.64, 1)',
} as const;

/**
 * Converts hex color to RGB string format
 * Cached for performance to avoid recalculating same values
 */
const rgbCache = new Map<string, string>();

export function hexToRgb(hex: string): string {
  // Check cache first
  if (rgbCache.has(hex)) {
    return rgbCache.get(hex)!;
  }

  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
  if (!result) {
    return '0, 0, 0';
  }
  
  const rgb = `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}`;
  
  // Store in cache
  rgbCache.set(hex, rgb);
  
  return rgb;
}

// Stagger delay for list animations
export function getStaggerDelay(index: number, baseDelay: number = 50): number {
  return index * baseDelay;
}
