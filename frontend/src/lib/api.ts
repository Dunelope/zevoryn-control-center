import type { EnvironmentHealthCheckResult, Product, ProductConnection, ProductEnvironment, SaaSEvent } from './types';

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, { headers: { 'Content-Type': 'application/json' }, ...init });
  if (!response.ok) { const body = await response.json().catch(() => ({})); throw new Error(body.detail ?? 'Request failed'); }
  return response.status === 204 ? undefined as T : response.json();
}
const json = (body: unknown): RequestInit => ({ method: 'POST', body: JSON.stringify(body) });
export const api = {
  products: () => request<Product[]>('/api/products'), product: (id: string) => request<Product>(`/api/products/${id}`),
  createProduct: (body: unknown) => request<Product>('/api/products', json(body)), updateProduct: (id: string, body: unknown) => request<Product>(`/api/products/${id}`, { ...json(body), method: 'PUT' }), deleteProduct: (id: string) => request<void>(`/api/products/${id}`, { method: 'DELETE' }),
  environments: (id: string) => request<ProductEnvironment[]>(`/api/products/${id}/environments`), environment: (productId: string, environmentId: string) => request<ProductEnvironment>(`/api/products/${productId}/environments/${environmentId}`),
  createEnvironment: (id: string, body: unknown) => request<ProductEnvironment>(`/api/products/${id}/environments`, json(body)), updateEnvironment: (productId: string, environmentId: string, body: unknown) => request<ProductEnvironment>(`/api/products/${productId}/environments/${environmentId}`, { ...json(body), method: 'PUT' }), deleteEnvironment: (productId: string, environmentId: string) => request<void>(`/api/products/${productId}/environments/${environmentId}`, { method: 'DELETE' }), healthCheck: (productId: string, environmentId: string) => request<EnvironmentHealthCheckResult>(`/api/products/${productId}/environments/${environmentId}/health-check`, { method: 'POST' }),
  connections: (productId: string, environmentId: string) => request<ProductConnection[]>(`/api/products/${productId}/environments/${environmentId}/connections`), createConnection: (productId: string, environmentId: string, body: unknown) => request<ProductConnection>(`/api/products/${productId}/environments/${environmentId}/connections`, json(body)), updateConnection: (productId: string, environmentId: string, connectionId: string, body: unknown) => request<ProductConnection>(`/api/products/${productId}/environments/${environmentId}/connections/${connectionId}`, { ...json(body), method: 'PUT' }), deleteConnection: (productId: string, environmentId: string, connectionId: string) => request<void>(`/api/products/${productId}/environments/${environmentId}/connections/${connectionId}`, { method: 'DELETE' }),
  events: () => request<SaaSEvent[]>('/api/events')
};
