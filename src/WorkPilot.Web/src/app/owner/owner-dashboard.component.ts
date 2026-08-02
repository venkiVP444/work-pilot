import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ApiService } from '../services/api.service';
import { Business, ServiceItem, AvailabilityRule, BookingRequest, DashboardMetrics } from '../models/workpilot.models';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="dashboard-page">
      <!-- Top Navigation Header -->
      <header class="top-nav">
        <div class="brand">
          <span class="logo">⚡</span>
          <span class="title">WorkPilot AI</span>
        </div>

        <div class="header-nav-actions">
          <a class="btn-pub-link" routerLink="/book/11111111-1111-1111-1111-111111111111" target="_blank">
            🌐 Public Customer Booking Page ↗
          </a>
          <button class="btn-refresh-sm" (click)="loadDashboardData()">
            🔄 Refresh Queue
          </button>
        </div>

        <div class="business-badge" *ngIf="business">
          <span class="b-name">{{ business.name }}</span>
          <span class="status-pill" [class.connected]="business.isCalendarConnected">
            {{ business.isCalendarConnected ? '• Google Calendar Live' : '• Demo Mode' }}
          </span>
        </div>
      </header>

      <div class="layout-body">
        <!-- Sidebar Navigation -->
        <aside class="sidebar">
          <button [class.active]="activeTab === 'overview'" (click)="activeTab = 'overview'">📊 Overview & Metrics</button>
          <button [class.active]="activeTab === 'requests'" (click)="activeTab = 'requests'">
            📬 Booking Requests
            <span *ngIf="pendingCount > 0" class="badge-count">{{ pendingCount }}</span>
          </button>
          <button [class.active]="activeTab === 'services'" (click)="activeTab = 'services'">✂️ Services Setup</button>
          <button [class.active]="activeTab === 'availability'" (click)="activeTab = 'availability'">⏰ Working Availability</button>
          <button [class.active]="activeTab === 'calendar'" (click)="activeTab = 'calendar'">📅 Google Calendar</button>
          <button [class.active]="activeTab === 'settings'" (click)="activeTab = 'settings'">⚙️ Business Settings</button>
        </aside>

        <!-- Main Content Area -->
        <main class="content-area">
          <!-- Overview Metrics -->
          <section *ngIf="activeTab === 'overview'">
            <h2 class="section-title">Business Overview & AI Metrics</h2>
            
            <div class="metrics-grid" *ngIf="metrics">
              <div class="metric-card">
                <span class="m-icon">👥</span>
                <div class="m-value">{{ metrics.totalLeads }}</div>
                <div class="m-label">Total Qualified Leads</div>
              </div>
              <div class="metric-card">
                <span class="m-icon">📬</span>
                <div class="m-value">{{ metrics.pendingBookingRequests }}</div>
                <div class="m-label">Pending Approval Requests</div>
              </div>
              <div class="metric-card">
                <span class="m-icon">✅</span>
                <div class="m-value">{{ metrics.confirmedBookings }}</div>
                <div class="m-label">Confirmed Calendar Bookings</div>
              </div>
              <div class="metric-card">
                <span class="m-icon">📈</span>
                <div class="m-value">{{ metrics.conversionRatePercentage }}%</div>
                <div class="m-label">Lead Conversion Rate</div>
              </div>
              <div class="metric-card">
                <span class="m-icon">🤖</span>
                <div class="m-value">{{ metrics.totalAIInteractions }}</div>
                <div class="m-label">AI Interactions Handled</div>
              </div>
            </div>
          </section>

          <!-- Booking Requests Queue -->
          <section *ngIf="activeTab === 'requests'">
            <div class="section-header">
              <h2 class="section-title">Booking Requests Queue</h2>
              <div class="filter-tabs">
                <button [class.active]="statusFilter === 'ALL'" (click)="statusFilter = 'ALL'">All ({{ allRequests.length }})</button>
                <button [class.active]="statusFilter === 'PENDING'" (click)="statusFilter = 'PENDING'">Pending ({{ countByStatus('PENDING') }})</button>
                <button [class.active]="statusFilter === 'CONFIRMED'" (click)="statusFilter = 'CONFIRMED'">Confirmed ({{ countByStatus('CONFIRMED') }})</button>
                <button [class.active]="statusFilter === 'CONFLICT'" (click)="statusFilter = 'CONFLICT'">Conflicts ({{ countByStatus('CONFLICT') }})</button>
                <button [class.active]="statusFilter === 'REJECTED'" (click)="statusFilter = 'REJECTED'">Rejected ({{ countByStatus('REJECTED') }})</button>
              </div>
            </div>

            <div *ngIf="filteredRequests.length === 0" class="empty-state">
              <p>🎉 No booking requests matching current filter ('{{ statusFilter }}')!</p>
              <button class="btn-primary" (click)="statusFilter = 'ALL'">Show All Requests ({{ allRequests.length }})</button>
            </div>

            <div class="request-cards">
              <div *ngFor="let req of filteredRequests" class="request-card" [ngClass]="getStatusCardClass(req.status)">
                <div class="req-header">
                  <div>
                    <h3 class="req-title">{{ req.proposedSlotSummary }}</h3>
                    <span class="req-service">\${{ req.servicePrice }} | {{ req.serviceName }} ({{ req.serviceDurationMinutes }} mins)</span>
                  </div>
                  <span class="status-badge-pill" [ngClass]="getStatusBadgeClass(req.status)">
                    {{ getStatusLabel(req.status) }}
                  </span>
                </div>
                
                <div class="req-body">
                  <p>👤 <strong>Customer:</strong> {{ req.leadName }} ({{ req.leadEmail }})</p>
                  <p *ngIf="req.leadPhone">📞 <strong>Phone:</strong> {{ req.leadPhone }}</p>
                  <p class="req-time">🕒 Requested At: {{ req.createdAt | date:'medium' }}</p>
                  
                  <div class="status-indicators-grid">
                    <p *ngIf="req.googleCalendarEventId" class="gcal-id">
                      📅 <strong>Google Calendar Event ID:</strong> <code>{{ req.googleCalendarEventId }}</code>
                    </p>
                    <p class="email-status-line">
                      ✉️ <strong>Email Delivery Status:</strong>
                      <span class="email-badge" [ngClass]="getEmailStatusBadgeClass(req.emailDeliveryStatus)">
                        {{ getEmailStatusLabel(req.emailDeliveryStatus) }}
                      </span>
                    </p>
                  </div>

                  <p *ngIf="req.ownerNotes" class="notes-text">📝 <strong>Notes:</strong> {{ req.ownerNotes }}</p>
                </div>

                <!-- Retry Email Button if Email Delivery Failed -->
                <div *ngIf="req.emailDeliveryStatus === 'Failed'" class="retry-box">
                  <button class="btn-retry-email" [disabled]="processingId === req.id" (click)="retryEmail(req.id)">
                    {{ processingId === req.id ? 'Retrying Email...' : '🔄 Retry Confirmation Email' }}
                  </button>
                  <span *ngIf="req.emailDeliveryError" class="error-hint">{{ req.emailDeliveryError }}</span>
                </div>

                <!-- Owner Action Buttons for Pending & Conflict Requests -->
                <div class="req-actions" *ngIf="isPendingOrConflict(req.status)">
                  <button class="btn-approve" [disabled]="processingId === req.id" (click)="approveRequest(req.id)">
                    {{ processingId === req.id ? 'Processing...' : '✓ Approve & Add to Google Calendar' }}
                  </button>
                  <button class="btn-reject" [disabled]="processingId === req.id" (click)="rejectRequest(req.id)">
                    ✕ Reject Request
                  </button>
                </div>
              </div>
            </div>
          </section>

          <!-- Services Setup -->
          <section *ngIf="activeTab === 'services'">
            <h2 class="section-title">Manage Services</h2>
            <div class="add-service-box">
              <h3>Add New Fitness Service</h3>
              <div class="form-row">
                <input type="text" [(ngModel)]="newServiceName" placeholder="Service Name (e.g. 1-on-1 Personal Training)" />
                <input type="number" [(ngModel)]="newServicePrice" placeholder="Price (\$)" />
                <input type="number" [(ngModel)]="newServiceDuration" placeholder="Duration (Minutes)" />
              </div>
              <textarea [(ngModel)]="newServiceDesc" placeholder="Description of what is included..."></textarea>
              <button class="btn-primary" (click)="createService()">+ Add Service</button>
            </div>

            <div class="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Service Name</th>
                    <th>Duration</th>
                    <th>Price</th>
                    <th>Description</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let s of services">
                    <td><strong>{{ s.name }}</strong></td>
                    <td>{{ s.durationMinutes }} mins</td>
                    <td>\${{ s.price }}</td>
                    <td>{{ s.description }}</td>
                    <td>
                      <button class="btn-danger-sm" (click)="deleteService(s.id)">Delete</button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- Working Availability Setup -->
          <section *ngIf="activeTab === 'availability'">
            <h2 class="section-title">Configured Business Working Hours</h2>
            <div class="table-container">
              <table>
                <thead>
                  <tr>
                    <th>Day of Week</th>
                    <th>Opening Time</th>
                    <th>Closing Time</th>
                    <th>Buffer Time</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngFor="let r of availability">
                    <td><strong>{{ getDayName(r.dayOfWeek) }}</strong></td>
                    <td>{{ r.startTime }}</td>
                    <td>{{ r.endTime }}</td>
                    <td>{{ r.bufferMinutes }} mins buffer</td>
                    <td><span class="badge-active">{{ r.isActive ? 'Active' : 'Disabled' }}</span></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </section>

          <!-- Google Calendar Integration -->
          <section *ngIf="activeTab === 'calendar'">
            <h2 class="section-title">Google Calendar Integration</h2>
            <div class="calendar-card">
              <div class="c-icon">📅</div>
              <h3>Google Calendar API Sync</h3>
              <p>WorkPilot AI synchronizes live Free/Busy calendar intervals and automatically creates Calendar Events once bookings are approved.</p>
              
              <div class="status-box">
                <p><strong>Connection Status:</strong></p>
                <span class="status-indicator" [class.active]="business?.isCalendarConnected">
                  {{ business?.isCalendarConnected ? '✅ Google Calendar Live Connected' : '⚠️ Google Calendar Disconnected (Demo / Simulated Mode Active)' }}
                </span>
                <p class="mode-info" *ngIf="!business?.isCalendarConnected">
                  <em>Note: Click below to authorize Google OAuth and sync bookings directly into your primary Google Calendar!</em>
                </p>
              </div>

              <div class="connection-box">
                <button class="btn-connect" (click)="connectGoogleCalendar()">
                  {{ business?.isCalendarConnected ? 'Re-Connect Google Calendar OAuth' : 'Connect Google Calendar OAuth' }}
                </button>
              </div>
            </div>
          </section>

          <!-- Business Settings -->
          <section *ngIf="activeTab === 'settings' && business">
            <h2 class="section-title">Business Profile & Settings</h2>
            <div class="settings-form">
              <label>Business Name</label>
              <input type="text" [(ngModel)]="business.name" />
              
              <label>Location</label>
              <input type="text" [(ngModel)]="business.location" />

              <label>Contact Email</label>
              <input type="email" [(ngModel)]="business.contactEmail" />

              <label>Cancellation Policy</label>
              <textarea [(ngModel)]="business.cancellationPolicy"></textarea>

              <button class="btn-primary" (click)="saveSettings()">Save Settings</button>
            </div>
          </section>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-page { font-family: 'Segoe UI', system-ui, sans-serif; background-color: #0f172a; color: #f8fafc; min-height: 100vh; display: flex; flex-direction: column; }
    .top-nav { background: #1e293b; padding: 16px 32px; border-bottom: 1px solid #334155; display: flex; justify-content: space-between; align-items: center; }
    .brand { display: flex; align-items: center; gap: 10px; font-size: 20px; font-weight: bold; }
    .b-name { font-weight: bold; margin-right: 10px; color: #cbd5e1; }
    .status-pill { font-size: 12px; background: #334155; color: #fbbf24; padding: 4px 10px; border-radius: 12px; }
    .status-pill.connected { background: #064e3b; color: #34d399; }

    .header-nav-actions { display: flex; align-items: center; gap: 12px; }
    .btn-pub-link { background: #334155; color: #38bdf8; padding: 8px 14px; border-radius: 8px; font-weight: 600; text-decoration: none; font-size: 13px; border: 1px solid #475569; transition: all 0.2s; }
    .btn-pub-link:hover { background: #475569; color: #ffffff; }
    .btn-refresh-sm { background: #6366f1; color: #ffffff; border: none; padding: 8px 14px; border-radius: 8px; font-weight: bold; cursor: pointer; font-size: 13px; }

    .layout-body { display: grid; grid-template-columns: 240px 1fr; flex: 1; }
    .sidebar { background: #1e293b; border-right: 1px solid #334155; padding: 24px 16px; display: flex; flex-direction: column; gap: 8px; }
    .sidebar button { background: transparent; border: none; color: #94a3b8; padding: 12px 16px; border-radius: 8px; text-align: left; font-size: 14px; cursor: pointer; font-weight: 600; transition: all 0.2s; display: flex; justify-content: space-between; align-items: center; }
    .sidebar button:hover, .sidebar button.active { background: #334155; color: #ffffff; }
    .badge-count { background: #ef4444; color: #ffffff; font-size: 11px; padding: 2px 6px; border-radius: 10px; font-weight: bold; }

    .content-area { padding: 32px; }
    .section-title { font-size: 22px; margin-top: 0; margin-bottom: 24px; color: #f1f5f9; }

    .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; }
    .metric-card { background: #1e293b; border: 1px solid #334155; border-radius: 16px; padding: 20px; text-align: center; }
    .m-icon { font-size: 28px; }
    .m-value { font-size: 32px; font-weight: bold; color: #38bdf8; margin: 8px 0; }
    .m-label { font-size: 13px; color: #94a3b8; }

    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
    .filter-tabs { display: flex; gap: 8px; background: #0f172a; padding: 4px; border-radius: 8px; border: 1px solid #334155; }
    .filter-tabs button { background: transparent; border: none; color: #94a3b8; padding: 6px 12px; border-radius: 6px; font-size: 13px; cursor: pointer; }
    .filter-tabs button.active { background: #334155; color: #ffffff; font-weight: bold; }

    .empty-state { text-align: center; padding: 48px; background: #1e293b; border-radius: 16px; color: #94a3b8; border: 1px border-dashed #334155; display: flex; flex-direction: column; align-items: center; gap: 16px; }

    .request-cards { display: flex; flex-direction: column; gap: 16px; }
    .request-card { background: #1e293b; border-radius: 12px; padding: 20px; border-left: 4px solid #475569; }
    .pending-border { border-left-color: #f59e0b; }
    .approved-border { border-left-color: #22c55e; }
    .rejected-border { border-left-color: #ef4444; }
    .conflict-border { border-left-color: #f97316; }

    .req-header { display: flex; justify-content: space-between; align-items: flex-start; }
    .req-title { margin: 0; font-size: 18px; color: #ffffff; }
    .req-service { font-size: 13px; color: #94a3b8; }
    
    .status-badge-pill { font-size: 12px; font-weight: bold; padding: 4px 12px; border-radius: 12px; }
    .pill-pending { background: #fef3c7; color: #92400e; }
    .pill-approved { background: #dcfce7; color: #166534; }
    .pill-rejected { background: #fee2e2; color: #991b1b; }
    .pill-conflict { background: #ffedd5; color: #9a3412; }

    .req-body { margin: 16px 0; font-size: 14px; color: #cbd5e1; }
    .status-indicators-grid { display: flex; flex-direction: column; gap: 6px; margin-top: 8px; }
    .gcal-id { color: #38bdf8; font-size: 13px; margin: 0; }
    .email-status-line { font-size: 13px; margin: 0; display: flex; align-items: center; gap: 8px; }
    .email-badge { font-size: 11px; font-weight: bold; padding: 2px 8px; border-radius: 10px; }
    .email-sent { background: #dcfce7; color: #166534; }
    .email-simulated { background: #fef3c7; color: #92400e; }
    .email-failed { background: #fee2e2; color: #991b1b; }
    .email-none { background: #e2e8f0; color: #475569; }

    .retry-box { margin-top: 10px; display: flex; align-items: center; gap: 12px; }
    .btn-retry-email { background: #f59e0b; color: #ffffff; border: none; padding: 6px 14px; border-radius: 6px; font-weight: bold; cursor: pointer; font-size: 12px; }
    .error-hint { color: #ef4444; font-size: 12px; }

    .notes-text { color: #fbbf24; font-size: 13px; margin-top: 6px; }

    .req-actions { display: flex; gap: 12px; margin-top: 12px; }
    .btn-approve { background: #22c55e; color: #ffffff; border: none; padding: 10px 18px; border-radius: 8px; font-weight: bold; cursor: pointer; }
    .btn-approve:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-reject { background: #ef4444; color: #ffffff; border: none; padding: 10px 18px; border-radius: 8px; font-weight: bold; cursor: pointer; }
    .btn-reject:disabled { opacity: 0.5; cursor: not-allowed; }

    .add-service-box { background: #1e293b; border: 1px solid #334155; border-radius: 12px; padding: 20px; margin-bottom: 24px; }
    .form-row { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: 12px; margin-bottom: 12px; }
    input, textarea { background: #0f172a; border: 1px solid #475569; color: #ffffff; padding: 10px 14px; border-radius: 8px; font-size: 14px; width: 100%; box-sizing: border-box; }
    textarea { margin-bottom: 12px; height: 80px; }
    .btn-primary { background: #6366f1; color: #ffffff; border: none; padding: 10px 20px; border-radius: 8px; font-weight: bold; cursor: pointer; }
    
    .table-container table { width: 100%; border-collapse: collapse; background: #1e293b; border-radius: 12px; overflow: hidden; }
    th, td { padding: 14px 18px; text-align: left; border-bottom: 1px solid #334155; font-size: 14px; }
    th { background: #0f172a; color: #94a3b8; }
    .btn-danger-sm { background: #ef4444; color: white; border: none; padding: 4px 10px; border-radius: 6px; cursor: pointer; }

    .calendar-card { background: #1e293b; border: 1px solid #334155; border-radius: 16px; padding: 32px; text-align: center; max-width: 600px; }
    .c-icon { font-size: 48px; }
    .status-box { margin: 20px 0; background: #0f172a; padding: 16px; border-radius: 12px; border: 1px solid #334155; }
    .mode-info { font-size: 12px; color: #94a3b8; margin-top: 8px; }
    .connection-box { display: flex; flex-direction: column; gap: 16px; align-items: center; margin-top: 16px; }
    .btn-connect { background: #4f46e5; color: #ffffff; border: none; padding: 12px 28px; border-radius: 8px; font-weight: bold; cursor: pointer; }
    .settings-form { max-width: 600px; display: flex; flex-direction: column; gap: 12px; }
  `]
})
export class OwnerDashboardComponent implements OnInit, OnDestroy {
  businessId = '11111111-1111-1111-1111-111111111111';
  activeTab = 'requests'; // Default to requests tab for fast workflow!
  statusFilter = 'ALL';

  business: Business | null = null;
  services: ServiceItem[] = [];
  availability: AvailabilityRule[] = [];
  allRequests: BookingRequest[] = [];
  metrics: DashboardMetrics | null = null;
  processingId: string | null = null;
  refreshInterval: any;

  // New Service Form
  newServiceName = '';
  newServicePrice = 85;
  newServiceDuration = 60;
  newServiceDesc = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.loadDashboardData();
    // Auto-refresh queue every 5 seconds
    this.refreshInterval = setInterval(() => this.loadDashboardData(), 5000);
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  loadDashboardData(): void {
    this.api.getBusiness(this.businessId).subscribe(b => this.business = b);
    this.api.getServices(this.businessId).subscribe(s => this.services = s);
    this.api.getAvailability(this.businessId).subscribe(a => this.availability = a);
    this.api.getAllBookingRequests(this.businessId).subscribe(r => this.allRequests = r);
    this.api.getMetrics(this.businessId).subscribe(m => this.metrics = m);
  }

  get pendingCount(): number {
    return this.countByStatus('PENDING');
  }

  get filteredRequests(): BookingRequest[] {
    if (this.statusFilter === 'ALL') return this.allRequests;
    return this.allRequests.filter(r => this.getStatusNormalized(r.status) === this.statusFilter);
  }

  countByStatus(status: string): number {
    return this.allRequests.filter(r => this.getStatusNormalized(r.status) === status).length;
  }

  approveRequest(id: string): void {
    if (this.processingId === id) return;
    this.processingId = id;

    this.api.approveBookingRequest(id, 'Approved from owner dashboard').subscribe({
      next: (res) => {
        this.processingId = null;
        const evtId = res.googleCalendarEventId || 'Simulated Event';
        const emailStatus = res.emailDeliveryStatus || 'Sent';
        
        // Show approval alert
        alert(`✓ Booking Approved & Confirmed!\n\nGoogle Calendar Event ID: ${evtId}\nEmail Delivery Status: ${emailStatus}`);
        
        // Switch filter to ALL so the approved request remains visible on screen!
        this.statusFilter = 'ALL';
        this.loadDashboardData();
      },
      error: (err) => {
        this.processingId = null;
        const message = err.error?.error || 'Calendar conflict or server error.';
        alert(`⚠️ Could Not Approve Booking:\n${message}`);
        this.loadDashboardData();
      }
    });
  }

  retryEmail(id: string): void {
    if (this.processingId === id) return;
    this.processingId = id;

    this.api.retryBookingEmail(id).subscribe({
      next: (res) => {
        this.processingId = null;
        alert(`Email Retry Result: ${res.emailDeliveryStatus}`);
        this.loadDashboardData();
      },
      error: (err) => {
        this.processingId = null;
        alert(`Retry Failed: ${err.error?.error || err.message}`);
        this.loadDashboardData();
      }
    });
  }

  rejectRequest(id: string): void {
    if (this.processingId === id) return;
    const reason = prompt('Reason for rejection:', 'Slot not available');
    if (!reason) return;

    this.processingId = id;

    this.api.rejectBookingRequest(id, reason).subscribe({
      next: () => {
        this.processingId = null;
        alert('Booking Request Rejected.');
        this.loadDashboardData();
      },
      error: () => {
        this.processingId = null;
      }
    });
  }

  createService(): void {
    if (!this.newServiceName) return;
    const payload = {
      name: this.newServiceName,
      price: this.newServicePrice,
      durationMinutes: this.newServiceDuration,
      description: this.newServiceDesc
    };

    this.api.createService(this.businessId, payload).subscribe({
      next: () => {
        this.newServiceName = '';
        this.newServiceDesc = '';
        this.loadDashboardData();
      }
    });
  }

  deleteService(id: string): void {
    if (confirm('Delete this service?')) {
      this.api.deleteService(id).subscribe(() => this.loadDashboardData());
    }
  }

  connectGoogleCalendar(): void {
    this.api.getCalendarConnectUrl(this.businessId).subscribe(res => {
      window.location.href = res.authorizationUrl;
    });
  }

  saveSettings(): void {
    if (!this.business) return;
    this.api.updateBusiness(this.businessId, this.business).subscribe(() => {
      alert('Business Settings Saved Successfully.');
    });
  }

  getDayName(day: number): string {
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return days[day] || 'Day';
  }

  getStatusNormalized(status: any): string {
    if (status === null || status === undefined) return 'PENDING';
    const s = String(status).trim().toLowerCase();
    if (s === '0' || s === 'pendingapproval' || s === 'pending') return 'PENDING';
    if (s === '1' || s === 'approved' || s === 'confirmed') return 'CONFIRMED';
    if (s === '2' || s === 'rejected') return 'REJECTED';
    if (s === '3' || s === 'conflict') return 'CONFLICT';
    return 'PENDING';
  }

  getStatusLabel(status: any): string {
    const norm = this.getStatusNormalized(status);
    if (norm === 'PENDING') return '⏳ Pending Approval';
    if (norm === 'CONFIRMED') return '✅ Confirmed (Google Calendar)';
    if (norm === 'REJECTED') return '✕ Rejected';
    if (norm === 'CONFLICT') return '⚠️ Calendar Conflict';
    return 'Pending Approval';
  }

  getStatusBadgeClass(status: any): string {
    const norm = this.getStatusNormalized(status);
    if (norm === 'PENDING') return 'pill-pending';
    if (norm === 'CONFIRMED') return 'pill-approved';
    if (norm === 'REJECTED') return 'pill-rejected';
    if (norm === 'CONFLICT') return 'pill-conflict';
    return 'pill-pending';
  }

  getStatusCardClass(status: any): string {
    const norm = this.getStatusNormalized(status);
    if (norm === 'PENDING') return 'pending-border';
    if (norm === 'CONFIRMED') return 'approved-border';
    if (norm === 'REJECTED') return 'rejected-border';
    if (norm === 'CONFLICT') return 'conflict-border';
    return 'pending-border';
  }

  isPendingOrConflict(status: any): boolean {
    const norm = this.getStatusNormalized(status);
    return norm === 'PENDING' || norm === 'CONFLICT';
  }

  getEmailStatusLabel(status?: string): string {
    if (status === 'Sent') return '✓ Email Sent';
    if (status === 'Simulated') return '⚡ Email Simulated';
    if (status === 'Failed') return '✕ Email Failed';
    return 'Not Attempted';
  }

  getEmailStatusBadgeClass(status?: string): string {
    if (status === 'Sent') return 'email-sent';
    if (status === 'Simulated') return 'email-simulated';
    if (status === 'Failed') return 'email-failed';
    return 'email-none';
  }
}
