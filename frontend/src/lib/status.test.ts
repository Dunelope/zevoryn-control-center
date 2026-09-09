import { describe, expect, it } from 'vitest';
import { statusClass, statusLabel } from './status';
import { normalizeApiResponse } from './api';

describe('status rendering', () => {
  it('renders the Product page safely for enum-number and missing legacy statuses', () => {
    expect(statusClass(0)).toBe('unknown');
    expect(statusLabel(0)).toBe('Unknown');
    expect(statusClass(undefined)).toBe('unknown');
    expect(statusLabel(undefined)).toBe('Unknown');
  });

  it('preserves the string enum contract for normal API responses', () => {
    expect(statusClass('Active')).toBe('active');
    expect(statusLabel('Active')).toBe('Active');
  });

  it('normalizes the backend numeric Product enum before rendering', () => {
    expect(normalizeApiResponse('/api/products', { status: 0 })).toEqual({ status: 'Active' });
  });
});
