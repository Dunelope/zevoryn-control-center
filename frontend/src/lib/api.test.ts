import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, connectionTypeToBackend, environmentTypeToBackend, normalizeApiResponse } from './api';

describe('enum API boundary', () => {
  afterEach(() => vi.restoreAllMocks());

  it.each([
    ['Development', 0],
    ['Staging', 1],
    ['Production', 2]
  ] as const)('%s maps to the backend environment enum', (value, expected) => {
    expect(environmentTypeToBackend(value)).toBe(expected);
  });

  it('maps connection types and rejects malformed values safely', () => {
    expect(connectionTypeToBackend('InternalApi')).toBe(0);
    expect(connectionTypeToBackend('HealthEndpoint')).toBe(1);
    expect(() => environmentTypeToBackend('Preview' as never)).toThrow(TypeError);
    expect(() => connectionTypeToBackend(99)).toThrow(TypeError);
  });

  it('normalizes known response enum numbers while preserving unknown values', () => {
    expect(normalizeApiResponse('/api/products/p/environments', { environmentType: 2, status: 1, connectionType: 0 })).toEqual({ environmentType: 'Production', status: 'Healthy', connectionType: 'InternalApi' });
    expect(normalizeApiResponse('/api/products/p/environments', { environmentType: 99, status: 'Legacy' })).toEqual({ environmentType: 99, status: 'Legacy' });
  });

  it('serializes environment creation with the numeric backend enum', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ environmentType: 1 }), { status: 201, headers: { 'Content-Type': 'application/json' } }));
    await api.createEnvironment('product-id', { name: 'Staging', environmentType: 'Staging', baseUrl: 'https://staging.example.com' });
    const request = fetchMock.mock.calls[0][1] as RequestInit;
    expect(JSON.parse(request.body as string)).toMatchObject({ name: 'Staging', environmentType: 1 });
  });
});
