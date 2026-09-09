export type Product = { id: string; name: string; slug: string; description?: string; status: string; createdAtUtc: string; updatedAtUtc: string };
export type ProductEnvironment = { id: string; productId: string; name: string; environmentType: string; baseUrl: string; status: string; createdAtUtc: string; updatedAtUtc: string };
export type ProductConnection = { id: string; productEnvironmentId: string; connectionType: string; secretReference: string; isEnabled: boolean; lastSuccessfulConnectionAtUtc?: string; lastFailureAtUtc?: string; lastError?: string; createdAtUtc: string; updatedAtUtc: string };
export type EnvironmentHealthCheckResult = { environmentId: string; status: string; httpStatusCode?: number; latencyMs?: number; message?: string; checkedAtUtc: string };
export type SaaSEvent = { id: string; productId: string; environmentId?: string; type: string; externalEntityId?: string; payloadJson: string; occurredAtUtc: string; receivedAtUtc: string };
