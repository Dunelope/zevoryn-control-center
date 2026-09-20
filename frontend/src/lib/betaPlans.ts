import type { BetaPlan } from './types';

export const betaPlanValues: readonly BetaPlan[] = ['Starter', 'Growth', 'Pro'];
export const defaultBetaPlan: BetaPlan = 'Starter';
export const betaPlanLabel = (plan: BetaPlan | undefined): string => plan ?? defaultBetaPlan;
