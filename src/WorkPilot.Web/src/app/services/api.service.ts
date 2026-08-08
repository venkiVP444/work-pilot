import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Business,
  ServiceItem,
  AvailabilityRule,
  CustomerChatMessageResponse,
  BookingRequest,
  DashboardMetrics,
  OwnerChatResponse,
  ExecuteActionResult,
  OpportunityCard,
  AIAgentActionLog,
  BusinessSnapshot,
  EnhancedMetrics
} from '../models/workpilot.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = 'http://localhost:5050/api';

  constructor(private http: HttpClient) {}

  // Business API
  getBusiness(id: string): Observable<Business> {
    return this.http.get<Business>(`${this.baseUrl}/businesses/${id}`);
  }

  updateBusiness(id: string, payload: any): Observable<Business> {
    return this.http.put<Business>(`${this.baseUrl}/businesses/${id}`, payload);
  }

  getBusinesses(): Observable<Business[]> {
    return this.http.get<Business[]>(`${this.baseUrl}/businesses`);
  }

  createBusiness(payload: any): Observable<Business> {
    return this.http.post<Business>(`${this.baseUrl}/businesses`, payload);
  }

  // Services API
  getServices(businessId: string): Observable<ServiceItem[]> {
    return this.http.get<ServiceItem[]>(`${this.baseUrl}/businesses/${businessId}/services`);
  }

  createService(businessId: string, payload: any): Observable<ServiceItem> {
    return this.http.post<ServiceItem>(`${this.baseUrl}/businesses/${businessId}/services`, payload);
  }

  deleteService(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/services/${id}`);
  }

  // Availability API
  getAvailability(businessId: string): Observable<AvailabilityRule[]> {
    return this.http.get<AvailabilityRule[]>(`${this.baseUrl}/businesses/${businessId}/availability`);
  }

  createAvailability(businessId: string, payload: any): Observable<AvailabilityRule> {
    return this.http.post<AvailabilityRule>(`${this.baseUrl}/businesses/${businessId}/availability`, payload);
  }

  // Calendar API
  getCalendarConnectUrl(businessId: string): Observable<{ authorizationUrl: string }> {
    return this.http.get<{ authorizationUrl: string }>(`${this.baseUrl}/calendar/connect?businessId=${businessId}`);
  }

  // Customer Chat & Booking
  sendChatMessage(businessId: string, customerMessage: string, conversationId?: string): Observable<CustomerChatMessageResponse> {
    return this.http.post<CustomerChatMessageResponse>(`${this.baseUrl}/customer/${businessId}/conversation/message`, {
      customerMessage,
      conversationId
    });
  }

  createBookingRequest(businessId: string, command: any): Observable<BookingRequest> {
    return this.http.post<BookingRequest>(`${this.baseUrl}/customer/${businessId}/booking-request`, command);
  }

  // Owner Approvals
  getPendingBookingRequests(businessId: string): Observable<BookingRequest[]> {
    return this.http.get<BookingRequest[]>(`${this.baseUrl}/booking-requests/pending?businessId=${businessId}`);
  }

  getAllBookingRequests(businessId: string): Observable<BookingRequest[]> {
    return this.http.get<BookingRequest[]>(`${this.baseUrl}/booking-requests?businessId=${businessId}`);
  }

  approveBookingRequest(id: string, notes?: string): Observable<BookingRequest> {
    return this.http.post<BookingRequest>(`${this.baseUrl}/booking-requests/${id}/approve`, { notes });
  }

  retryBookingEmail(id: string): Observable<BookingRequest> {
    return this.http.post<BookingRequest>(`${this.baseUrl}/booking-requests/${id}/retry-email`, {});
  }

  rejectBookingRequest(id: string, reason: string): Observable<BookingRequest> {
    return this.http.post<BookingRequest>(`${this.baseUrl}/booking-requests/${id}/reject`, { reason });
  }

  // Metrics
  getMetrics(businessId: string): Observable<DashboardMetrics> {
    return this.http.get<DashboardMetrics>(`${this.baseUrl}/metrics/${businessId}`);
  }

  // ─── Owner AI Business OS ────────────────────────────────────────────────────

  ownerChat(businessId: string, message: string, lastActionId?: string): Observable<OwnerChatResponse> {
    return this.http.post<OwnerChatResponse>(`${this.baseUrl}/owner/${businessId}/chat`, {
      message,
      lastActionId
    });
  }

  executeAction(businessId: string, actionId: string, notes?: string): Observable<ExecuteActionResult> {
    return this.http.post<ExecuteActionResult>(`${this.baseUrl}/owner/${businessId}/execute-action`, {
      businessId,
      actionId,
      ownerNotes: notes
    });
  }

  rejectAction(businessId: string, actionId: string, reason: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/owner/${businessId}/reject-action/${actionId}`, { reason });
  }

  getOpportunities(businessId: string): Observable<OpportunityCard[]> {
    return this.http.get<OpportunityCard[]>(`${this.baseUrl}/owner/${businessId}/opportunities`);
  }

  getAIOperations(businessId: string, take = 20): Observable<AIAgentActionLog[]> {
    return this.http.get<AIAgentActionLog[]>(`${this.baseUrl}/owner/${businessId}/ai-operations?take=${take}`);
  }

  getBusinessSnapshot(businessId: string): Observable<BusinessSnapshot> {
    return this.http.get<BusinessSnapshot>(`${this.baseUrl}/owner/${businessId}/snapshot`);
  }

  getEnhancedMetrics(businessId: string): Observable<EnhancedMetrics> {
    return this.http.get<EnhancedMetrics>(`${this.baseUrl}/owner/${businessId}/metrics/enhanced`);
  }
}

