import type { BetaCampaign } from './types';
export function betaMetrics(campaigns: BetaCampaign[]) { return { activeCampaigns: campaigns.filter(c => c.status === 'Active').length, pendingInvitations: campaigns.reduce((sum, c) => sum + c.pendingInvitations, 0), acceptedInvitations: campaigns.reduce((sum, c) => sum + c.acceptedInvitations, 0) }; }
