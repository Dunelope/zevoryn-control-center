import type { SaaSEvent } from './types';
export function addLiveEvent(current: SaaSEvent[], next: SaaSEvent, limit = 20): SaaSEvent[] { return [next, ...current.filter(item => item.id !== next.id)].slice(0, limit); }
