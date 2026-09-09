import type { BetaCampaign, BetaInvitation, ConnectionType, EnvironmentHealthCheckResult, EnvironmentType, Product, ProductConnection, ProductEnvironment, SaaSEvent } from './types';

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';
export const enumNames: Record<string, readonly string[]> = {
  productStatus: ['Active', 'Inactive'],
  environmentStatus: ['Unknown', 'Healthy', 'Degraded', 'Offline'],
  environmentType: ['Development', 'Staging', 'Production'],
  connectionType: ['InternalApi', 'HealthEndpoint'],
  betaCampaignStatus: ['Draft', 'Active', 'Paused', 'Closed'],
  betaInvitationStatus: ['Pending', 'Sent', 'Accepted', 'Revoked', 'Expired', 'Failed']
};
export const environmentTypeValues: readonly EnvironmentType[] = ['Development', 'Staging', 'Production'];
export const connectionTypeValues: readonly ConnectionType[] = ['InternalApi', 'HealthEndpoint'];
const enumValues: Record<string, readonly string[]> = { ...enumNames, environmentType: environmentTypeValues, connectionType: connectionTypeValues };
const enumNumber = (kind: string, value: unknown): number => {
  const values = enumValues[kind];
  const result = typeof value === 'number' && Number.isInteger(value) ? value : values?.indexOf(value as string);
  if (!values || result === undefined || result < 0 || result >= values.length) throw new TypeError(`Unknown ${kind} enum value: ${String(value)}`);
  return result;
};
export function environmentTypeToBackend(value: EnvironmentType | number): number { return enumNumber('environmentType', value); }
export function connectionTypeToBackend(value: ConnectionType | number): number { return enumNumber('connectionType', value); }
function enumName(value: unknown, names: readonly string[]): unknown { return typeof value === 'number' && Number.isInteger(value) && names[value] ? names[value] : value; }
export function normalizeApiResponse(path: string, value: unknown): unknown {
  if (Array.isArray(value)) return value.map(item => normalizeApiResponse(path, item));
  if (!value || typeof value !== 'object') return value;
  const statusKind = path.includes('/beta/campaigns') ? (path.includes('/invitations') ? 'betaInvitationStatus' : 'betaCampaignStatus') : path.includes('/environments') || path.includes('health-check') ? 'environmentStatus' : 'productStatus';
  return Object.fromEntries(Object.entries(value).map(([key, item]) => [key, key === 'status' ? enumName(item, enumNames[statusKind]) : key === 'environmentType' ? enumName(item, environmentTypeValues) : key === 'connectionType' ? enumName(item, connectionTypeValues) : item]));
}
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, { headers: { 'Content-Type': 'application/json' }, ...init });
  if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.detail ?? 'Request failed'); }
  return response.status === 204 ? undefined as T : response.json().then(body => normalizeApiResponse(path, body) as T);
}
const json = (body: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(body) });
export const api = {
  products: () => request<Product[]>('/api/products'), product: (id: string) => request<Product>(`/api/products/${id}`),
  createProduct: (body: unknown) => request<Product>('/api/products', json(body)), updateProduct: (id: string, body: unknown) => request<Product>(`/api/products/${id}`, { ...json(body), method: 'PUT' }), deleteProduct: (id: string) => request<void>(`/api/products/${id}`, { method: 'DELETE' }),
  environments: (id: string) => request<ProductEnvironment[]>(`/api/products/${id}/environments`), environment: (productId: string, environmentId: string) => request<ProductEnvironment>(`/api/products/${productId}/environments/${environmentId}`),
  createEnvironment: (id: string, body: { name: string; environmentType: EnvironmentType; baseUrl: string }) => request<ProductEnvironment>(`/api/products/${id}/environments`, json({ ...body, environmentType: environmentTypeToBackend(body.environmentType) })), updateEnvironment: (productId: string, environmentId: string, body: { name: string; environmentType: EnvironmentType; baseUrl: string }) => request<ProductEnvironment>(`/api/products/${productId}/environments/${environmentId}`, { ...json({ ...body, environmentType: environmentTypeToBackend(body.environmentType) }), method: 'PUT' }), deleteEnvironment: (productId: string, environmentId: string) => request<void>(`/api/products/${productId}/environments/${environmentId}`, { method: 'DELETE' }), healthCheck: (productId: string, environmentId: string) => request<EnvironmentHealthCheckResult>(`/api/products/${productId}/environments/${environmentId}/health-check`, { method: 'POST' }),
  connections: (productId: string, environmentId: string) => request<ProductConnection[]>(`/api/products/${productId}/environments/${environmentId}/connections`), createConnection: (productId: string, environmentId: string, body: { connectionType: ConnectionType; secretReference: string; accessClientIdSecretReference?: string | null; accessClientSecretSecretReference?: string | null; isEnabled: boolean }) => request<ProductConnection>(`/api/products/${productId}/environments/${environmentId}/connections`, json({ ...body, connectionType: connectionTypeToBackend(body.connectionType) })), updateConnection: (productId: string, environmentId: string, connectionId: string, body: { connectionType: ConnectionType; secretReference: string; accessClientIdSecretReference?: string | null; accessClientSecretSecretReference?: string | null; isEnabled: boolean }) => request<ProductConnection>(`/api/products/${productId}/environments/${environmentId}/connections/${connectionId}`, { ...json({ ...body, connectionType: connectionTypeToBackend(body.connectionType) }), method: 'PUT' }), deleteConnection: (productId: string, environmentId: string, connectionId: string) => request<void>(`/api/products/${productId}/environments/${environmentId}/connections/${connectionId}`, { method: 'DELETE' }),
  events: () => request<SaaSEvent[]>('/api/events')
  , betaCampaigns: (query = '') => request<BetaCampaign[]>(`/api/beta/campaigns${query}`), betaCampaign: (id: string) => request<BetaCampaign>(`/api/beta/campaigns/${id}`), createBetaCampaign: (body: unknown) => request<BetaCampaign>('/api/beta/campaigns', json(body)), updateBetaCampaign: (id: string, body: unknown) => request<BetaCampaign>(`/api/beta/campaigns/${id}`, { ...json(body), method: 'PUT' }), activateBetaCampaign: (id: string) => request<void>(`/api/beta/campaigns/${id}/activate`, { method: 'POST' }), pauseBetaCampaign: (id: string) => request<void>(`/api/beta/campaigns/${id}/pause`, { method: 'POST' }), closeBetaCampaign: (id: string) => request<void>(`/api/beta/campaigns/${id}/close`, { method: 'POST' }), betaInvitations: (id: string) => request<BetaInvitation[]>(`/api/beta/campaigns/${id}/invitations`), createBetaInvitation: (id: string, body: unknown) => request<BetaInvitation>(`/api/beta/campaigns/${id}/invitations`, json(body)), retryBetaInvitation: (campaignId: string, invitationId: string) => request<BetaInvitation>(`/api/beta/campaigns/${campaignId}/invitations/${invitationId}/retry`, { method: 'POST' }), syncBetaInvitation: (campaignId: string, invitationId: string) => request<BetaInvitation>(`/api/beta/campaigns/${campaignId}/invitations/${invitationId}/sync`, { method: 'POST' }), revokeBetaInvitation: (campaignId: string, invitationId: string) => request<void>(`/api/beta/campaigns/${campaignId}/invitations/${invitationId}/revoke`, { method: 'POST' })
};
