export function statusClass(status: unknown): string {
  return typeof status === 'string' && status.trim() ? status.toLowerCase() : 'unknown';
}

export function statusLabel(status: unknown): string {
  return typeof status === 'string' && status.trim() ? status : 'Unknown';
}
