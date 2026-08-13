import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ApiService } from '../services/api.service';
import { Business, ServiceItem, CalendarSlot, CustomerChatMessageResponse } from '../models/workpilot.models';

interface ChatMessage {
  sender: 'user' | 'assistant';
  text: string;
  timestamp: Date;
  slots?: CalendarSlot[];
}

@Component({
  selector: 'app-customer-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="booking-page">
      <!-- Business Header Card -->
      <header class="business-header" *ngIf="business">
        <div class="header-content">
          <div class="brand-info">
            <div class="avatar-icon">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>
            </div>
            <div>
              <div class="title-row">
                <h1>{{ business.name }}</h1>
                <span class="verified-badge">
                  <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor"><path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/></svg> Verified Business
                </span>
              </div>
              <p class="tagline">{{ business.description }}</p>
              <div class="location-bar">
                <span>📍 {{ business.location }}</span>
                <span class="sep">•</span>
                <span>✉️ {{ business.contactEmail }}</span>
              </div>
            </div>
          </div>
          <div class="header-nav">
            <a class="nav-btn-owner" routerLink="/dashboard">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M15 3h6v6M10 14L21 3M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"/></svg>
              Owner Dashboard
            </a>
          </div>
        </div>
      </header>

      <div class="main-container">
        <!-- Available Services Sidebar -->
        <aside class="services-panel">
          <div class="panel-header">
            <h3>Available Services</h3>
            <span class="service-count">{{ services.length }} offered</span>
          </div>
          <div class="service-cards-list">
            <div class="service-card" *ngFor="let s of services" [class.selected]="selectedService?.id === s.id" (click)="selectService(s)">
              <div class="service-title">
                <strong>{{ s.name }}</strong>
                <span class="price">\${{ s.price }}</span>
              </div>
              <p class="service-desc">{{ s.description }}</p>
              <div class="service-meta">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
                {{ s.durationMinutes }} mins session
              </div>
            </div>
          </div>
        </aside>

        <!-- Chat & Booking Workflow Area -->
        <main class="chat-section">
          <div class="chat-header-bar">
            <div class="agent-title">
              <span class="ai-icon-spark">⚡</span>
              <div>
                <span class="agent-name">WorkPilot Autonomous Booking Assistant</span>
                <span class="agent-status"><span class="live-beacon"></span> Online & Syncing Calendar</span>
              </div>
            </div>
          </div>

          <div class="chat-messages" #scrollContainer>
            <div *ngFor="let msg of messages" class="message-row" [class.user]="msg.sender === 'user'">
              <div class="avatar">{{ msg.sender === 'user' ? '👤' : '🤖' }}</div>
              <div class="bubble">
                <div class="sender-name">{{ msg.sender === 'user' ? 'You' : 'WorkPilot AI' }}</div>
                <p>{{ msg.text }}</p>

                <!-- Available Slots Selection UI -->
                <div *ngIf="msg.slots && msg.slots.length > 0 && !bookingSubmitted" class="slots-container">
                  <p class="slots-title">📅 <strong>Select an available time slot:</strong></p>
                  <div class="slot-buttons">
                    <button 
                      *ngFor="let slot of msg.slots" 
                      class="slot-btn"
                      [class.active]="selectedSlot === slot"
                      [disabled]="isSubmittingBooking || bookingSubmitted"
                      (click)="onSelectSlot(slot)">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
                      {{ slot.displayText }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Loading Spinner -->
            <div *ngIf="isLoading" class="message-row">
              <div class="avatar">🤖</div>
              <div class="bubble loading">
                <span class="spinner-dots"><span></span><span></span><span></span></span>
                WorkPilot AI is checking live calendar availability...
              </div>
            </div>
          </div>

          <!-- Customer Lead Details Form (Visible after slot selected) -->
          <div *ngIf="selectedSlot && !bookingSubmitted" class="lead-form-card">
            <h4>Confirm Your Details for Slot: <span class="slot-highlight">{{ selectedSlot.displayText }}</span></h4>
            <div class="form-grid">
              <input type="text" [(ngModel)]="customerName" placeholder="Your Full Name *" [disabled]="isSubmittingBooking" required />
              <input type="email" [(ngModel)]="customerEmail" placeholder="Your Email Address *" [disabled]="isSubmittingBooking" required />
              <input type="tel" [(ngModel)]="customerPhone" placeholder="Phone Number (Optional)" [disabled]="isSubmittingBooking" />
              <button class="btn-submit" [disabled]="!customerName || !customerEmail || isSubmittingBooking || bookingSubmitted" (click)="submitBookingRequest()">
                {{ isSubmittingBooking ? '⏳ Submitting Request...' : 'Submit Booking Request' }}
              </button>
            </div>
          </div>

          <!-- Successful Booking Request Banner -->
          <div *ngIf="bookingSubmitted" class="success-banner">
            <div class="success-icon">🎉</div>
            <h3>Booking Request Submitted!</h3>
            <p>Your request for <strong>{{ selectedSlot?.displayText }}</strong> has been sent to {{ business?.name }}.</p>
            <div class="status-badge">Status: Pending Owner Approval</div>
            <p class="sub-text">Once approved, a Google Calendar invite and confirmation email will be sent automatically to <strong>{{ customerEmail }}</strong>.</p>
            
            <div class="banner-actions">
              <a class="btn-goto-dashboard" routerLink="/dashboard">
                Go to Owner Dashboard to Approve Request ➔
              </a>
            </div>
          </div>

          <!-- Chat Input Bar -->
          <div class="chat-input-bar" *ngIf="!bookingSubmitted">
            <input 
              type="text" 
              [(ngModel)]="userMessage" 
              (keyup.enter)="sendMessage()"
              [disabled]="isLoading || isSubmittingBooking"
              placeholder="e.g. Hi, I want to book personal training this Saturday morning..." 
            />
            <button (click)="sendMessage()" [disabled]="!userMessage.trim() || isLoading || isSubmittingBooking">
              Send
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>
            </button>
          </div>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .booking-page { font-family: var(--font-sans); background-color: var(--bg-canvas); color: var(--text-main); min-height: 100vh; display: flex; flex-direction: column; }
    
    .business-header { background: var(--bg-surface); padding: 20px 32px; border-bottom: 1px solid var(--border-subtle); }
    .header-content { display: flex; justify-content: space-between; align-items: center; max-width: 1240px; margin: 0 auto; width: 100%; }
    .brand-info { display: flex; align-items: center; gap: 16px; }
    .avatar-icon { width: 48px; height: 48px; background: rgba(99, 102, 241, 0.12); color: var(--ai-primary); border-radius: var(--radius-md); border: 1px solid rgba(99, 102, 241, 0.25); display: flex; align-items: center; justify-content: center; }
    .title-row { display: flex; align-items: center; gap: 10px; }
    .business-header h1 { margin: 0; font-size: 22px; font-weight: 700; color: #ffffff; font-family: var(--font-display); }
    .verified-badge { background: rgba(16, 185, 129, 0.12); color: var(--success-emerald); border: 1px solid rgba(16, 185, 129, 0.25); padding: 2px 8px; border-radius: var(--radius-full); font-size: 11px; font-weight: 600; display: inline-flex; align-items: center; gap: 4px; }
    .tagline { color: var(--text-muted); margin: 3px 0 4px 0; font-size: 13.5px; }
    .location-bar { color: var(--text-dim); font-size: 12.5px; display: flex; align-items: center; gap: 8px; }
    .sep { color: var(--border-medium); }

    .nav-btn-owner { background: var(--bg-surface-hover); color: var(--text-main); border: 1px solid var(--border-medium); padding: 8px 16px; border-radius: var(--radius-sm); font-weight: 600; text-decoration: none; font-size: 13px; transition: all 0.15s ease; display: inline-flex; align-items: center; gap: 6px; }
    .nav-btn-owner:hover { background: #222d42; border-color: var(--border-strong); color: #ffffff; }

    .main-container { display: grid; grid-template-columns: 320px 1fr; gap: 24px; max-width: 1240px; width: 100%; margin: 24px auto; padding: 0 24px; box-sizing: border-box; flex: 1; }
    
    .services-panel { background: var(--bg-surface); border-radius: var(--radius-lg); padding: 20px; border: 1px solid var(--border-subtle); height: fit-content; }
    .panel-header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-subtle); padding-bottom: 12px; margin-bottom: 16px; }
    .services-panel h3 { margin: 0; color: #ffffff; font-size: 15px; font-weight: 600; }
    .service-count { font-size: 12px; color: var(--text-dim); }

    .service-cards-list { display: flex; flex-direction: column; gap: 10px; }
    .service-card { background: var(--bg-card); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px; cursor: pointer; transition: all 0.15s ease; }
    .service-card:hover { border-color: var(--border-medium); background: var(--bg-card-hover); }
    .service-card.selected { border-color: var(--ai-primary); background: rgba(99, 102, 241, 0.08); }
    .service-title { display: flex; justify-content: space-between; align-items: center; }
    .service-title strong { color: #ffffff; font-size: 14px; }
    .price { color: var(--success-emerald); font-weight: 700; font-family: var(--font-mono); font-size: 13.5px; }
    .service-desc { font-size: 12.5px; color: var(--text-muted); margin: 6px 0 10px 0; line-height: 1.4; }
    .service-meta { font-size: 11.5px; color: #a5b4fc; display: flex; align-items: center; gap: 5px; }

    .chat-section { background: var(--bg-surface); border-radius: var(--radius-lg); border: 1px solid var(--border-subtle); display: flex; flex-direction: column; height: 640px; overflow: hidden; }
    .chat-header-bar { padding: 14px 20px; background: var(--bg-canvas); border-bottom: 1px solid var(--border-subtle); display: flex; align-items: center; justify-content: space-between; }
    .agent-title { display: flex; align-items: center; gap: 10px; }
    .ai-icon-spark { font-size: 16px; color: var(--ai-primary); }
    .agent-name { display: block; font-weight: 600; font-size: 13.5px; color: #ffffff; }
    .agent-status { display: flex; align-items: center; gap: 6px; font-size: 11.5px; color: var(--text-dim); }

    .chat-messages { flex: 1; padding: 20px; overflow-y: auto; display: flex; flex-direction: column; gap: 16px; }
    .message-row { display: flex; gap: 12px; align-items: flex-start; max-width: 85%; }
    .message-row.user { align-self: flex-end; flex-direction: row-reverse; }
    .avatar { width: 34px; height: 34px; border-radius: 50%; background: var(--bg-card-hover); border: 1px solid var(--border-medium); display: flex; align-items: center; justify-content: center; font-size: 15px; flex-shrink: 0; }
    .message-row.user .avatar { background: var(--ai-primary); border-color: transparent; }

    .bubble { background: var(--bg-card); border: 1px solid var(--border-subtle); padding: 12px 16px; border-radius: var(--radius-md); font-size: 13.5px; line-height: 1.5; color: var(--text-main); }
    .message-row.user .bubble { background: var(--ai-gradient); border-color: transparent; color: #ffffff; }
    .sender-name { font-size: 11px; color: var(--text-dim); margin-bottom: 4px; font-weight: 600; }
    .message-row.user .sender-name { color: rgba(255,255,255,0.7); text-align: right; }

    .slots-container { margin-top: 12px; background: var(--bg-canvas); padding: 14px; border-radius: var(--radius-md); border: 1px solid var(--border-medium); }
    .slots-title { margin: 0 0 10px 0; font-size: 13px; color: var(--text-muted); }
    .slot-buttons { display: flex; flex-direction: column; gap: 8px; }
    .slot-btn { background: var(--bg-surface); color: var(--info-cyan); border: 1px solid rgba(6, 182, 212, 0.3); padding: 10px 14px; border-radius: var(--radius-sm); font-weight: 600; font-size: 13px; cursor: pointer; text-align: left; transition: all 0.15s ease; display: flex; align-items: center; gap: 8px; }
    .slot-btn:hover, .slot-btn.active { background: var(--info-cyan); color: #090d16; border-color: var(--info-cyan); }
    .slot-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    .lead-form-card { background: rgba(99, 102, 241, 0.08); border: 1px solid rgba(99, 102, 241, 0.3); border-radius: var(--radius-md); padding: 16px 20px; margin: 0 20px 16px 20px; }
    .lead-form-card h4 { margin: 0 0 12px 0; color: #a5b4fc; font-size: 13.5px; font-weight: 600; }
    .slot-highlight { color: #ffffff; font-weight: 700; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr 1fr auto; gap: 10px; }
    .btn-submit { background: var(--success-emerald); color: #ffffff; border: none; padding: 10px 18px; border-radius: var(--radius-sm); font-weight: 600; font-size: 13px; cursor: pointer; transition: all 0.15s ease; white-space: nowrap; }
    .btn-submit:hover:not(:disabled) { background: #059669; }
    .btn-submit:disabled { opacity: 0.5; cursor: not-allowed; background: #475569; }

    .success-banner { background: var(--success-bg); border: 1px solid rgba(16, 185, 129, 0.3); border-radius: var(--radius-lg); padding: 24px; text-align: center; margin: 20px; }
    .success-icon { font-size: 40px; margin-bottom: 8px; }
    .success-banner h3 { margin: 0 0 6px 0; color: #ffffff; font-size: 18px; }
    .status-badge { display: inline-block; background: var(--warning-bg); color: var(--warning-amber); border: 1px solid rgba(245, 158, 11, 0.3); font-weight: 600; padding: 4px 14px; border-radius: var(--radius-full); font-size: 12px; margin: 10px 0; }
    .sub-text { font-size: 13px; color: var(--text-muted); margin-top: 8px; }
    .banner-actions { margin-top: 16px; }
    .btn-goto-dashboard { background: var(--ai-primary); color: #ffffff; padding: 10px 20px; border-radius: var(--radius-sm); font-weight: 600; text-decoration: none; display: inline-block; font-size: 13px; transition: all 0.15s ease; }
    .btn-goto-dashboard:hover { background: var(--ai-primary-hover); }

    .chat-input-bar { display: flex; gap: 10px; padding: 16px 20px; background: var(--bg-canvas); border-top: 1px solid var(--border-subtle); }
    .chat-input-bar input { flex: 1; }
    .chat-input-bar button { background: var(--ai-primary); color: #ffffff; border: none; padding: 10px 20px; border-radius: var(--radius-sm); font-weight: 600; font-size: 13px; cursor: pointer; display: flex; align-items: center; gap: 6px; transition: all 0.15s ease; }
    .chat-input-bar button:hover:not(:disabled) { background: var(--ai-primary-hover); }
    .chat-input-bar button:disabled { opacity: 0.5; cursor: not-allowed; }

    .spinner-dots { display: inline-flex; gap: 4px; margin-right: 6px; }
    .spinner-dots span { width: 4px; height: 4px; background: var(--ai-primary); border-radius: 50%; animation: pulseGlow 1s infinite alternate; }
    .spinner-dots span:nth-child(2) { animation-delay: 0.2s; }
    .spinner-dots span:nth-child(3) { animation-delay: 0.4s; }
  `]
})
export class CustomerBookingComponent implements OnInit {
  businessId = '11111111-1111-1111-1111-111111111111';
  business: Business | null = null;
  services: ServiceItem[] = [];
  selectedService: ServiceItem | null = null;

  messages: ChatMessage[] = [];
  userMessage = '';
  isLoading = false;
  conversationId?: string;

  selectedSlot: CalendarSlot | null = null;
  customerName = '';
  customerEmail = '';
  customerPhone = '';
  isSubmittingBooking = false;
  bookingSubmitted = false;

  constructor(private route: ActivatedRoute, private api: ApiService) {}

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      if (params['businessId']) {
        this.businessId = params['businessId'];
      }
      this.loadBusinessData();
    });
  }

  loadBusinessData(): void {
    this.api.getBusiness(this.businessId).subscribe(b => {
      this.business = b;
      this.messages.push({
        sender: 'assistant',
        text: `Hi! Welcome to ${b.name}. I'm WorkPilot AI. How can I help you with your booking today?`,
        timestamp: new Date()
      });
    });

    this.api.getServices(this.businessId).subscribe(list => {
      this.services = list;
      if (list.length > 0) this.selectedService = list[0];
    });
  }

  selectService(service: ServiceItem): void {
    this.selectedService = service;
  }

  sendMessage(): void {
    if (!this.userMessage.trim() || this.isLoading || this.isSubmittingBooking) return;

    const text = this.userMessage.trim();
    this.messages.push({ sender: 'user', text, timestamp: new Date() });
    this.userMessage = '';
    this.isLoading = true;

    this.api.sendChatMessage(this.businessId, text, this.conversationId).subscribe({
      next: (res: CustomerChatMessageResponse) => {
        this.isLoading = false;
        this.conversationId = res.conversationId;
        this.messages.push({
          sender: 'assistant',
          text: res.assistantMessage,
          timestamp: new Date(),
          slots: res.proposedSlots
        });
      },
      error: () => {
        this.isLoading = false;
        this.messages.push({
          sender: 'assistant',
          text: 'I apologize, I encountered an issue checking calendar slots. What day and time work best for you?',
          timestamp: new Date()
        });
      }
    });
  }

  onSelectSlot(slot: CalendarSlot): void {
    if (this.isSubmittingBooking || this.bookingSubmitted) return;
    this.selectedSlot = slot;
  }

  submitBookingRequest(): void {
    if (this.isSubmittingBooking || this.bookingSubmitted) return;
    if (!this.selectedSlot || !this.customerName || !this.customerEmail || !this.selectedService) return;

    this.isSubmittingBooking = true;

    const payload = {
      businessId: this.businessId,
      conversationId: this.conversationId || '00000000-0000-0000-0000-000000000000',
      serviceId: this.selectedService.id,
      requestedStartTime: this.selectedSlot.startTime,
      requestedEndTime: this.selectedSlot.endTime,
      customerName: this.customerName,
      customerEmail: this.customerEmail,
      customerPhone: this.customerPhone
    };

    this.api.createBookingRequest(this.businessId, payload).subscribe({
      next: () => {
        this.isSubmittingBooking = false;
        this.bookingSubmitted = true;
      },
      error: (err) => {
        this.isSubmittingBooking = false;
        alert('Failed to submit booking request: ' + (err.error?.error || 'Unknown error'));
      }
    });
  }
}
