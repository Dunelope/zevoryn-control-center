import { describe, expect, it } from 'vitest';
import { betaPlanLabel, betaPlanValues, defaultBetaPlan } from './betaPlans';

describe('beta plans', () => {
  it('defaults campaign selection to Starter', () => {
    expect(defaultBetaPlan).toBe('Starter');
    expect(betaPlanValues).toEqual(['Starter', 'Growth', 'Pro']);
  });

  it('renders selected campaign and invitation plan labels safely', () => {
    expect(betaPlanLabel('Growth')).toBe('Growth');
    expect(betaPlanLabel(undefined)).toBe('Starter');
  });
});
