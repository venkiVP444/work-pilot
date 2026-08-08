export interface Business {
  id: string;
  name: string;
  description: string;
  location: string;
  contactEmail: string;
  timeZone: string;
  cancellationPolicy: string;
  communicationTone: string;
  isCalendarConnected: boolean;
  googleCalendarId?: string;
  createdAt: string;
}

export interface ServiceItem {
  id: string;
  businessId: string;
  name: string;
  description: string;
  price: number;
  durationMinutes: number;
  isActive: boolean;
  createdAt: string;
}

export interface AvailabilityRule {
  id: string;
  businessId: string;
  dayOfWeek: number; // 0 = Sunday, 1 = Monday ... 6 = Saturday
  startTime: string;
  endTime: string;
  bufferMinutes: number;
  isActive: boolean;
}

export interface CalendarSlot {
  startTime: string;
  endTime: string;
  displayText: string;
}

export interface CustomerChatMessageResponse {
  conversationId: string;
  businessId: string;
  assistantMessage: string;
  proposedSlots: CalendarSlot[];
  missingInformation: string[];
  intent: string;
  decision: string;
  matchedServiceId?: string;
}

export interface BookingRequest {
  id: string;
  businessId: string;
  leadId: string;
  leadName: string;
  leadEmail: string;
  leadPhone?: string;
  serviceId: string;
  serviceName: string;
  servicePrice: number;
  serviceDurationMinutes: number;
  requestedStartTime: string;
  requestedEndTime: string;
  proposedSlotSummary: string;
  status: number; // 0 = PendingApproval, 1 = Approved, 2 = Rejected, 3 = Conflict
  ownerNotes?: string;
  createdAt: string;
  googleCalendarEventId?: string;
  emailDeliveryStatus?: string;
  emailDeliveryError?: string;
}

export interface DashboardMetrics {
  totalLeads: number;
  qualifiedLeads: number;
  pendingBookingRequests: number;
  pendingRequests?: number;
  confirmedBookings: number;
  conversionRatePercentage: number;
  conversionRate?: number;
  totalAIInteractions: number;
}

// ─── AI Business OS Models ────────────────────────────────────────────────────

export interface OwnerChatMessage {
  role: 'owner' | 'ai';
  content: string;
  timestamp: Date;
  actionPlan?: AIActionPlan;
  agentChain?: AIAgentStep[];
  requiresApproval?: boolean;
  actionId?: string;
  isTyping?: boolean;
}

export interface AIActionPlan {
  actionId: string;
  actionType: string;
  agentType: string;
  riskLevel: 'Low' | 'Medium' | 'High';
  title: string;
  description: string;
  estimatedImpact: string;
  estimatedRevenue: number;
  estimatedBookings: number;
  targetCustomerCount: number;
  whatWillHappen: string;
  whyRecommended: string;
  estimatedCost: number;
  status: string;
  createdAt: string;
}

export interface AIAgentStep {
  agent: string;
  action: string;
  result: string;
  timestamp: string;
  success: boolean;
}

export interface OwnerChatResponse {
  assistantMessage: string;
  reasoningSummary: string;
  businessSnapshot?: BusinessSnapshot;
  actionPlan?: AIActionPlan;
  opportunities: OpportunityCard[];
  agentChain: AIAgentStep[];
  requiresApproval: boolean;
  actionId?: string;
}

export interface ExecuteActionResult {
  actionId: string;
  success: boolean;
  message: string;
  customersReached: number;
  bookingRequestsGenerated: number;
  revenueImpact: number;
  failureReason?: string;
  executionSteps: AIAgentStep[];
}

export interface OpportunityCard {
  title: string;
  description: string;
  estimatedRevenue: string;
  actionLabel: string;
  actionType: string;
  affectedCustomers: number;
  priority: 'high' | 'medium' | 'low';
  icon: string;
}

export interface BusinessSnapshot {
  businessName: string;
  totalCustomers: number;
  activeCustomers: number;
  inactiveCustomers30Days: number;
  inactiveCustomers60Days: number;
  inactiveCustomers90Plus: number;
  revenueThisMonth: number;
  revenueLastMonth: number;
  bookingsThisMonth: number;
  bookingsLastMonth: number;
  pendingBookingRequests: number;
  emptySlotsThisWeek: number;
  averageOrderValue: number;
  totalConfirmedBookings: number;
  totalRevenue: number;
  topServices: string[];
}

export interface AIAgentActionLog {
  id: string;
  agentType: string;
  actionType: string;
  riskLevel: string;
  status: string;
  ownerIntent: string;
  reasoningSummary: string;
  agentChain: string;
  estimatedImpact: string;
  estimatedRevenue: number;
  estimatedBookings: number;
  actualOutcome?: string;
  actualRevenue: number;
  actualBookings: number;
  targetCustomerCount: number;
  failureReason?: string;
  createdAt: string;
  executedAt?: string;
  completedAt?: string;
}

export interface EnhancedMetrics {
  totalCustomers: number;
  activeCustomers: number;
  inactiveCustomers: number;
  totalLeads: number;
  qualifiedLeads: number;
  pendingBookingRequests: number;
  confirmedBookings: number;
  conversionRatePercentage: number;
  totalAIInteractions: number;
  revenueThisMonth: number;
  revenueLastMonth: number;
  revenueGrowthPercent: number;
  totalRevenue: number;
  averageOrderValue: number;
  bookingsThisMonth: number;
  bookingsLastMonth: number;
  totalCampaignsSent: number;
  totalCampaignBookings: number;
  totalCampaignRevenue: number;
  aiActionsExecuted: number;
  aiInfluencedRevenue: number;
}

