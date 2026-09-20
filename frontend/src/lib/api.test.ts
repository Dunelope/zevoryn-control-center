import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, betaPlanToBackend, connectionTypeToBackend, environmentTypeToBackend, normalizeApiResponse } from './api';

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

  it('maps supported beta plans to backend enum values', () => {
    expect(betaPlanToBackend('Starter')).toBe(0);
    expect(betaPlanToBackend('Growth')).toBe(1);
    expect(betaPlanToBackend('Pro')).toBe(2);
    expect(() => betaPlanToBackend('Enterprise' as never)).toThrow(TypeError);
  });

  it('normalizes known response enum numbers while preserving unknown values', () => {
    expect(normalizeApiResponse('/api/products/p/environments', { environmentType: 2, status: 1, connectionType: 0 })).toEqual({ environmentType: 'Production', status: 'Healthy', connectionType: 'InternalApi' });
    expect(normalizeApiResponse('/api/products/p/environments', { environmentType: 99, status: 'Legacy' })).toEqual({ environmentType: 99, status: 'Legacy' });
  });

  it('normalizes beta plan response values', () => {
    expect(normalizeApiResponse('/api/beta/campaigns', { betaPlan: 2 })).toEqual({ betaPlan: 'Pro' });
    expect(normalizeApiResponse('/api/beta/campaigns/c/invitations', { betaPlan: 1 })).toEqual({ betaPlan: 'Growth' });
  });

  it('sends the selected beta plan when creating a campaign', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({}), { status: 201, headers: { 'Content-Type': 'application/json' } }));
    await api.createBetaCampaign({ productId: 'p1', environmentId: 'e1', name: 'Growth beta', description: null, maxInvitations: 5, startsAtUtc: null, endsAtUtc: null, betaPlan: 'Growth' });
    expect(JSON.parse((fetchMock.mock.calls[0][1] as RequestInit).body as string).betaPlan).toBe(1);
  });

  it('serializes environment creation with the numeric backend enum', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ environmentType: 1 }), { status: 201, headers: { 'Content-Type': 'application/json' } }));
    await api.createEnvironment('product-id', { name: 'Staging', environmentType: 'Staging', baseUrl: 'https://staging.example.com' });
    const request = fetchMock.mock.calls[0][1] as RequestInit;
    expect(JSON.parse(request.body as string)).toMatchObject({ name: 'Staging', environmentType: 1 });
  });

  it('loads filtered feedback reports and patches status without control headers', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 })).mockResolvedValueOnce(new Response(JSON.stringify({ id: 'r1', status: 'InProgress' }), { status: 200 }));
    await api.feedbackReports('New', 'Bug'); await api.updateFeedbackReportStatus('r1', 'InProgress');
    expect(fetchMock.mock.calls[0][0]).toContain('/api/feedback-reports?status=New&type=Bug');
    expect(fetchMock.mock.calls[1][0]).toContain('/api/feedback-reports/r1/status');
    expect((fetchMock.mock.calls[1][1] as RequestInit).method).toBe('PATCH');
    expect((fetchMock.mock.calls[1][1] as RequestInit).headers).toEqual({ 'Content-Type': 'application/json' });
  });
});
