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
