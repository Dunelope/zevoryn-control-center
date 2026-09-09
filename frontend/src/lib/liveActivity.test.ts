import { describe, expect, it } from 'vitest'; import { addLiveEvent } from './liveActivity';
describe('live activity', () => { it('prepends new events and removes duplicate ids', () => { const first = { id: '1' } as any; const second = { id: '2' } as any; expect(addLiveEvent([first], second)).toEqual([second, first]); expect(addLiveEvent([first], first)).toEqual([first]); }); });
