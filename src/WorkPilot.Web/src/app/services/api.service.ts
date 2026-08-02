import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Business,
  ServiceItem,
  AvailabilityRule,
  CustomerChatMessageResponse,
  BookingRequest,
  DashboardMetrics
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
}
