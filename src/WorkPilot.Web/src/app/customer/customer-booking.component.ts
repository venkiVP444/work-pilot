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
            <div class="avatar-icon">🏋️‍♂️</div>
            <div>
              <h1>{{ business.name }}</h1>
              <p class="tagline">{{ business.description }}</p>
              <p class="location">📍 {{ business.location }} | ✉️ {{ business.contactEmail }}</p>
            </div>
          </div>
          <div class="header-nav">
            <a class="nav-btn-owner" routerLink="/dashboard">
              🔑 Owner Dashboard ➔
            </a>
          </div>
        </div>
      </header>

      <div class="main-container">
        <!-- Available Services Sidebar -->
        <aside class="services-panel">
          <h3>Available Services</h3>
          <div class="service-card" *ngFor="let s of services" [class.selected]="selectedService?.id === s.id" (click)="selectService(s)">
            <div class="service-title">
              <strong>{{ s.name }}</strong>
              <span class="price">\${{ s.price }}</span>
            </div>
            <p class="service-desc">{{ s.description }}</p>
            <div class="service-meta">⏱️ {{ s.durationMinutes }} mins session</div>
          </div>
        </aside>

        <!-- Chat & Booking Workflow Area -->
        <main class="chat-section">
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
                      {{ slot.displayText }}
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Loading Spinner -->
            <div *ngIf="isLoading" class="message-row">
              <div class="avatar">🤖</div>
              <div class="bubble loading">WorkPilot AI is checking live calendar availability...</div>
            </div>
          </div>

          <!-- Customer Lead Details Form (Visible after slot selected) -->
          <div *ngIf="selectedSlot && !bookingSubmitted" class="lead-form-card">
            <h4>Confirm Your Details for Slot: {{ selectedSlot.displayText }}</h4>
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
            <p class="sub-text">Once the trainer approves your request, a real Google Calendar invite and confirmation email will be sent automatically to <strong>{{ customerEmail }}</strong>.</p>
            
            <div class="banner-actions">
              <a class="btn-goto-dashboard" routerLink="/dashboard">
                🔑 Go to Owner Dashboard to Approve Request ➔
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
            <button (click)="sendMessage()" [disabled]="!userMessage.trim() || isLoading || isSubmittingBooking">Send</button>
          </div>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .booking-page { font-family: 'Segoe UI', system-ui, sans-serif; background-color: #0f172a; color: #f8fafc; min-height: 100vh; display: flex; flex-direction: column; }
    .business-header { background: linear-gradient(135deg, #1e293b 0%, #334155 100%); padding: 24px 32px; border-bottom: 1px solid #475569; }
    .header-content { display: flex; justify-content: space-between; align-items: center; max-width: 1200px; margin: 0 auto; width: 100%; }
    .brand-info { display: flex; align-items: center; gap: 20px; }
    .avatar-icon { font-size: 40px; background: rgba(99, 102, 241, 0.2); padding: 12px; border-radius: 16px; border: 1px solid #6366f1; }
    .business-header h1 { margin: 0; font-size: 26px; color: #ffffff; }
    .tagline { color: #94a3b8; margin: 4px 0; font-size: 15px; }
    .location { color: #cbd5e1; font-size: 13px; margin: 0; }
    .nav-btn-owner { background: #6366f1; color: #ffffff; padding: 10px 18px; border-radius: 8px; font-weight: bold; text-decoration: none; font-size: 14px; transition: all 0.2s; display: inline-block; }
    .nav-btn-owner:hover { background: #4f46e5; }
    
    .main-container { display: grid; grid-template-columns: 320px 1fr; gap: 24px; max-width: 1200px; width: 100%; margin: 24px auto; padding: 0 24px; box-sizing: border-box; flex: 1; }
    .services-panel { background: #1e293b; border-radius: 16px; padding: 20px; border: 1px solid #334155; height: fit-content; }
    .services-panel h3 { margin-top: 0; color: #f1f5f9; font-size: 18px; border-bottom: 1px solid #334155; padding-bottom: 12px; }
    .service-card { background: #0f172a; border: 1px solid #334155; border-radius: 12px; padding: 16px; margin-bottom: 12px; cursor: pointer; transition: all 0.2s; }
    .service-card:hover, .service-card.selected { border-color: #6366f1; background: #1e1b4b; }
    .service-title { display: flex; justify-content: space-between; align-items: center; }
    .price { color: #4ade80; font-weight: bold; }
    .service-desc { font-size: 13px; color: #94a3b8; margin: 8px 0; }
    .service-meta { font-size: 12px; color: #818cf8; }

    .chat-section { background: #1e293b; border-radius: 16px; border: 1px solid #334155; display: flex; flex-direction: column; height: 620px; overflow: hidden; }
    .chat-messages { flex: 1; padding: 24px; overflow-y: auto; display: flex; flex-direction: column; gap: 16px; }
    .message-row { display: flex; gap: 12px; align-items: flex-start; max-width: 80%; }
    .message-row.user { align-self: flex-end; flex-direction: row-reverse; }
    .avatar { font-size: 20px; background: #334155; width: 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
    .bubble { background: #334155; padding: 14px 18px; border-radius: 16px; font-size: 15px; line-height: 1.5; color: #f8fafc; }
    .message-row.user .bubble { background: #4f46e5; color: #ffffff; }
    .sender-name { font-size: 11px; color: #94a3b8; margin-bottom: 4px; font-weight: 600; }
    
    .slots-container { margin-top: 14px; background: #0f172a; padding: 14px; border-radius: 12px; border: 1px solid #475569; }
    .slots-title { margin: 0 0 10px 0; font-size: 14px; color: #e2e8f0; }
    .slot-buttons { display: flex; flex-direction: column; gap: 8px; }
    .slot-btn { background: #1e293b; color: #38bdf8; border: 1px solid #38bdf8; padding: 10px 14px; border-radius: 8px; font-weight: 600; cursor: pointer; text-align: left; transition: all 0.2s; }
    .slot-btn:hover, .slot-btn.active { background: #38bdf8; color: #0f172a; }
    .slot-btn:disabled { opacity: 0.5; cursor: not-allowed; }

    .lead-form-card { background: #1e1b4b; border: 1px solid #6366f1; border-radius: 12px; padding: 16px 20px; margin: 0 24px 16px 24px; }
    .lead-form-card h4 { margin: 0 0 12px 0; color: #a5b4fc; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr 1fr auto; gap: 10px; }
    .form-grid input { background: #0f172a; border: 1px solid #475569; color: #ffffff; padding: 10px 14px; border-radius: 8px; font-size: 14px; }
    .btn-submit { background: #22c55e; color: #ffffff; border: none; padding: 10px 20px; border-radius: 8px; font-weight: bold; cursor: pointer; transition: all 0.2s; }
    .btn-submit:disabled { opacity: 0.5; cursor: not-allowed; background: #475569; }

    .success-banner { background: linear-gradient(135deg, #064e3b 0%, #022c22 100%); border: 1px solid #10b981; border-radius: 12px; padding: 24px; text-align: center; margin: 24px; }
    .success-icon { font-size: 48px; }
    .status-badge { display: inline-block; background: #fef08a; color: #854d0e; font-weight: bold; padding: 6px 16px; border-radius: 20px; margin: 12px 0; }
    .sub-text { font-size: 13px; color: #a7f3d0; margin-top: 8px; }
    .banner-actions { margin-top: 18px; }
    .btn-goto-dashboard { background: #6366f1; color: #ffffff; padding: 12px 24px; border-radius: 8px; font-weight: bold; text-decoration: none; display: inline-block; font-size: 14px; }

    .chat-input-bar { display: flex; gap: 12px; padding: 16px 24px; background: #0f172a; border-top: 1px solid #334155; }
    .chat-input-bar input { flex: 1; background: #1e293b; border: 1px solid #475569; color: #ffffff; padding: 12px 18px; border-radius: 12px; font-size: 15px; }
    .chat-input-bar button { background: #6366f1; color: #ffffff; border: none; padding: 12px 24px; border-radius: 12px; font-weight: bold; cursor: pointer; }
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
