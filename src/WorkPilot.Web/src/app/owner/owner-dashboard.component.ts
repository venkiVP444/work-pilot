import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ApiService } from '../services/api.service';
import { BusinessContextService } from '../services/business-context.service';
import {
  Business, ServiceItem, AvailabilityRule, BookingRequest, DashboardMetrics,
  OwnerChatMessage, AIActionPlan, OpportunityCard, AIAgentActionLog, BusinessSnapshot,
  EnhancedMetrics, ExecuteActionResult
} from '../models/workpilot.models';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
<div class="dashboard-page">

  <!-- ── Top Nav ─────────────────────────────────────────────────────────── -->
  <header class="top-nav">
    <div class="brand">
      <span class="logo">⚡</span>
      <span class="title">WorkPilot <span class="ai-badge">AI</span></span>
    </div>
    <div class="header-center" *ngIf="snapshot">
      <span class="snap-pill">👥 {{snapshot.totalCustomers}} customers</span>
      <span class="snap-pill revenue">💰 \${{snapshot.revenueThisMonth | number:'1.0-0'}}/mo</span>
      <span class="snap-pill warn" *ngIf="snapshot.inactiveCustomers60Days > 0">
        ⚠️ {{snapshot.inactiveCustomers60Days}} inactive 60d+
      </span>
    </div>
    <div class="header-right">
      <a class="btn-pub-link" [routerLink]="['/book', businessId]" target="_blank">
        🌐 Customer Booking Page ↗
      </a>
      <div class="business-badge" *ngIf="business">
        <select class="business-selector" [ngModel]="businessId" (ngModelChange)="switchBusiness($event)">
          <option *ngFor="let b of allBusinesses" [value]="b.id">{{ b.name }}</option>
        </select>
        <button class="btn-create-biz" (click)="showBusinessForm = true">➕ New</button>
      </div>
    </div>
  </header>

  <div class="layout-body">

    <!-- ── Sidebar ────────────────────────────────────────────────────────── -->
    <aside class="sidebar">
      <div class="sidebar-section-label">AI BUSINESS OS</div>
      <button id="nav-ai-chat" [class.active]="activeTab==='ai-chat'" (click)="setTab('ai-chat')">
        <span class="nav-icon">🤖</span> AI Business Chat
        <span class="nav-new-badge">NEW</span>
      </button>
      <button id="nav-insights" [class.active]="activeTab==='insights'" (click)="setTab('insights')">
        <span class="nav-icon">💡</span> Business Insights
      </button>
      <button id="nav-ai-ops" [class.active]="activeTab==='ai-ops'" (click)="setTab('ai-ops')">
        <span class="nav-icon">🔬</span> AI Operations
      </button>

      <div class="sidebar-section-label">BOOKINGS</div>
      <button id="nav-overview" [class.active]="activeTab==='overview'" (click)="setTab('overview')">
        <span class="nav-icon">📊</span> Overview
      </button>
      <button id="nav-requests" [class.active]="activeTab==='requests'" (click)="setTab('requests')">
        <span class="nav-icon">📬</span> Booking Requests
        <span *ngIf="pendingCount>0" class="badge-count">{{pendingCount}}</span>
      </button>
      <button id="nav-services" [class.active]="activeTab==='services'" (click)="setTab('services')">
        <span class="nav-icon">✂️</span> Services
      </button>
      <button id="nav-availability" [class.active]="activeTab==='availability'" (click)="setTab('availability')">
        <span class="nav-icon">⏰</span> Availability
      </button>
      <button id="nav-calendar" [class.active]="activeTab==='calendar'" (click)="setTab('calendar')">
        <span class="nav-icon">📅</span> Google Calendar
      </button>
      <button id="nav-settings" [class.active]="activeTab==='settings'" (click)="setTab('settings')">
        <span class="nav-icon">⚙️</span> Settings
      </button>
    </aside>

    <!-- ── Main Content ──────────────────────────────────────────────────── -->
    <main class="content-area">

      <!-- ═══════════════════════════════════════════════════════════════════
           AI BUSINESS CHAT
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='ai-chat'" class="ai-chat-section">
        <div class="ai-chat-header">
          <div>
            <h2 class="section-title">AI Business Chat</h2>
            <p class="section-sub">Tell me your business goals — I'll analyze, plan, and act.</p>
          </div>
          <div class="ai-status-badge">
            <span class="pulse-dot"></span> AI Agents Online
          </div>
        </div>

        <!-- Opportunity Cards (Morning Brief) -->
        <div class="opportunity-strip" *ngIf="opportunities.length > 0 && chatMessages.length === 0">
          <div class="opp-label">🌅 Today's Opportunities</div>
          <div class="opp-cards">
            <div class="opp-card" *ngFor="let opp of opportunities" [class]="'opp-'+opp.priority"
                 (click)="quickSend('Help me ' + opp.actionLabel.toLowerCase())">
              <div class="opp-icon">{{opp.icon}}</div>
              <div class="opp-body">
                <div class="opp-title">{{opp.title}}</div>
                <div class="opp-desc">{{opp.description}}</div>
                <div class="opp-revenue" *ngIf="opp.estimatedRevenue">{{opp.estimatedRevenue}} potential</div>
              </div>
              <div class="opp-action">{{opp.actionLabel}} →</div>
            </div>
          </div>
        </div>

        <!-- Chat Messages -->
        <div class="chat-messages" #chatContainer>
          <div *ngIf="chatMessages.length === 0" class="chat-welcome">
            <div class="welcome-icon">🤖</div>
            <h3>Good {{greeting()}}, I'm your AI Business Operator</h3>
            <p>I have full visibility into your business. Tell me what you need:</p>
            <div class="starter-chips">
              <button class="chip" (click)="quickSend('I need to make 20% more profit this month')">
                💰 Grow profit 20%
              </button>
              <button class="chip" (click)="quickSend('How is my business performing?')">
                📊 Business performance
              </button>
              <button class="chip" (click)="quickSend('Help me reactivate inactive customers')">
                👥 Reactivate customers
              </button>
              <button class="chip" (click)="quickSend('Fill my empty appointment slots this week')">
                📅 Fill empty slots
              </button>
            </div>
          </div>

          <div *ngFor="let msg of chatMessages" class="chat-message" [class.owner-msg]="msg.role==='owner'" [class.ai-msg]="msg.role==='ai'">

            <!-- Owner message -->
            <div *ngIf="msg.role==='owner'" class="msg-bubble owner-bubble">
              <div class="msg-text">{{msg.content}}</div>
              <div class="msg-time">{{msg.timestamp | date:'shortTime'}}</div>
            </div>

            <!-- AI message -->
            <div *ngIf="msg.role==='ai'" class="msg-ai-wrapper">
              <div class="ai-avatar">🤖</div>
              <div class="msg-ai-content">

                <!-- Agent chain badges -->
                <div class="agent-chain" *ngIf="msg.agentChain && msg.agentChain.length > 0">
                  <span *ngFor="let step of msg.agentChain; let i=index" class="agent-step" [class.success]="step.success" [class.fail]="!step.success">
                    {{step.agent}}
                  </span>
                </div>

                <!-- Typing indicator -->
                <div *ngIf="msg.isTyping" class="typing-indicator">
                  <span></span><span></span><span></span>
                </div>

                <div *ngIf="!msg.isTyping" class="msg-bubble ai-bubble">
                  <div class="msg-text" [innerHTML]="formatMessage(msg.content)"></div>

                  <!-- Action Plan Card -->
                  <div *ngIf="msg.actionPlan" class="action-plan-card" [class]="'risk-' + msg.actionPlan.riskLevel.toLowerCase()">
                    <div class="plan-header">
                      <div class="plan-risk-badge" [class]="'risk-badge-' + msg.actionPlan.riskLevel.toLowerCase()">
                        {{getRiskIcon(msg.actionPlan.riskLevel)}} {{msg.actionPlan.riskLevel}} Risk
                      </div>
                      <div class="plan-title">{{msg.actionPlan.title}}</div>
                    </div>

                    <div class="plan-metrics">
                      <div class="plan-metric">
                        <span class="pm-label">Target Customers</span>
                        <span class="pm-value">{{msg.actionPlan.targetCustomerCount}}</span>
                      </div>
                      <div class="plan-metric">
                        <span class="pm-label">Est. Revenue</span>
                        <span class="pm-value green">\${{msg.actionPlan.estimatedRevenue | number:'1.0-0'}}</span>
                      </div>
                      <div class="plan-metric">
                        <span class="pm-label">Est. Bookings</span>
                        <span class="pm-value">{{msg.actionPlan.estimatedBookings}}</span>
                      </div>
                      <div class="plan-metric">
                        <span class="pm-label">Campaign Cost</span>
                        <span class="pm-value green">\${{msg.actionPlan.estimatedCost | number:'1.0-0'}}</span>
                      </div>
                    </div>

                    <details class="plan-details">
                      <summary>📋 What will happen?</summary>
                      <p class="plan-detail-text">{{msg.actionPlan.whatWillHappen}}</p>
                    </details>
                    <details class="plan-details">
                      <summary>💡 Why this recommendation?</summary>
                      <p class="plan-detail-text">{{msg.actionPlan.whyRecommended}}</p>
                    </details>

                    <!-- Approval Buttons -->
                    <div class="plan-actions" *ngIf="msg.actionPlan.status === 'AwaitingApproval' || msg.actionPlan.status === 'Proposed'">
                      <button class="btn-approve" id="btn-approve-{{msg.actionPlan.actionId}}"
                              [disabled]="executingActionId === msg.actionPlan.actionId"
                              (click)="approveAction(msg.actionPlan)">
                        <span *ngIf="executingActionId !== msg.actionPlan.actionId">✅ Approve & Execute</span>
                        <span *ngIf="executingActionId === msg.actionPlan.actionId" class="spinner">⏳ Executing...</span>
                      </button>
                      <button class="btn-reject-action" id="btn-reject-{{msg.actionPlan.actionId}}"
                              [disabled]="executingActionId === msg.actionPlan.actionId"
                              (click)="rejectAction(msg.actionPlan)">
                        ✕ Reject
                      </button>
                    </div>
                    <div class="plan-status-done" *ngIf="msg.actionPlan.status === 'Completed'">
                      ✅ Action completed successfully
                    </div>
                    <div class="plan-status-rejected" *ngIf="msg.actionPlan.status === 'Rejected'">
                      ✕ Action rejected
                    </div>
                  </div>

                  <div class="msg-time">{{msg.timestamp | date:'shortTime'}}</div>
                </div>
              </div>
            </div>

          </div>
        </div>

        <!-- Chat Input -->
        <div class="chat-input-area">
          <div class="chat-input-row">
            <textarea
              id="owner-chat-input"
              class="chat-textarea"
              [(ngModel)]="chatInput"
              placeholder="Tell me what you need... 'I need more revenue', 'Fill my empty slots', 'How's business going?'"
              rows="2"
              (keydown.enter)="onEnter($event)"
              [disabled]="isAiThinking"
            ></textarea>
            <button class="chat-send-btn" id="btn-send-ai-chat"
                    [disabled]="!chatInput.trim() || isAiThinking"
                    (click)="sendOwnerMessage()">
              <span *ngIf="!isAiThinking">Send ↑</span>
              <span *ngIf="isAiThinking" class="spinner">⏳</span>
            </button>
          </div>
          <div class="chat-hint">Press Enter to send • Shift+Enter for new line</div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           BUSINESS INSIGHTS
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='insights'" class="insights-section">
        <h2 class="section-title">Business Insights</h2>
        <p class="section-sub">Live view of your business health and AI-identified opportunities.</p>

        <!-- KPI Cards -->
        <div class="kpi-grid" *ngIf="enhancedMetrics">
          <div class="kpi-card revenue-card">
            <div class="kpi-icon">💰</div>
            <div class="kpi-value">\${{enhancedMetrics.revenueThisMonth | number:'1.0-0'}}</div>
            <div class="kpi-label">Revenue This Month</div>
            <div class="kpi-change" [class.positive]="enhancedMetrics.revenueGrowthPercent >= 0" [class.negative]="enhancedMetrics.revenueGrowthPercent < 0">
              {{enhancedMetrics.revenueGrowthPercent >= 0 ? '↑' : '↓'}} {{enhancedMetrics.revenueGrowthPercent | number:'1.1-1'}}% vs last month
            </div>
          </div>
          <div class="kpi-card">
            <div class="kpi-icon">👥</div>
            <div class="kpi-value">{{enhancedMetrics.totalCustomers}}</div>
            <div class="kpi-label">Total Customers</div>
            <div class="kpi-sub">{{enhancedMetrics.activeCustomers}} active this month</div>
          </div>
          <div class="kpi-card warn-card" *ngIf="enhancedMetrics.inactiveCustomers > 0">
            <div class="kpi-icon">⚠️</div>
            <div class="kpi-value">{{enhancedMetrics.inactiveCustomers}}</div>
            <div class="kpi-label">Inactive 60+ Days</div>
            <div class="kpi-sub">Click AI Chat to reactivate</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-icon">📅</div>
            <div class="kpi-value">{{enhancedMetrics.bookingsThisMonth}}</div>
            <div class="kpi-label">Bookings This Month</div>
            <div class="kpi-sub">Avg \${{enhancedMetrics.averageOrderValue | number:'1.0-0'}}/session</div>
          </div>
          <div class="kpi-card ai-card" *ngIf="enhancedMetrics.aiActionsExecuted > 0">
            <div class="kpi-icon">🤖</div>
            <div class="kpi-value">{{enhancedMetrics.aiActionsExecuted}}</div>
            <div class="kpi-label">AI Actions Executed</div>
            <div class="kpi-sub">\${{enhancedMetrics.aiInfluencedRevenue | number:'1.0-0'}} AI-influenced revenue</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-icon">📈</div>
            <div class="kpi-value">{{enhancedMetrics.conversionRatePercentage}}%</div>
            <div class="kpi-label">Lead Conversion Rate</div>
            <div class="kpi-sub">{{enhancedMetrics.confirmedBookings}} confirmed bookings</div>
          </div>
        </div>

        <!-- Customer Segments -->
        <div class="segments-card" *ngIf="snapshot">
          <h3 class="card-title">Customer Segments</h3>
          <div class="segment-bars">
            <div class="segment-row">
              <span class="seg-label">Active (last 30 days)</span>
              <div class="seg-bar-bg">
                <div class="seg-bar seg-active" [style.width.%]="getSegmentPct(snapshot.activeCustomers)"></div>
              </div>
              <span class="seg-count">{{snapshot.activeCustomers}}</span>
            </div>
            <div class="segment-row">
              <span class="seg-label">Inactive 30–59 days</span>
              <div class="seg-bar-bg">
                <div class="seg-bar seg-warn" [style.width.%]="getSegmentPct(snapshot.inactiveCustomers30Days)"></div>
              </div>
              <span class="seg-count">{{snapshot.inactiveCustomers30Days}}</span>
            </div>
            <div class="segment-row">
              <span class="seg-label">Inactive 60–89 days ← <strong>Primary Target</strong></span>
              <div class="seg-bar-bg">
                <div class="seg-bar seg-danger" [style.width.%]="getSegmentPct(snapshot.inactiveCustomers60Days)"></div>
              </div>
              <span class="seg-count">{{snapshot.inactiveCustomers60Days}}</span>
            </div>
            <div class="segment-row">
              <span class="seg-label">Inactive 90+ days</span>
              <div class="seg-bar-bg">
                <div class="seg-bar seg-critical" [style.width.%]="getSegmentPct(snapshot.inactiveCustomers90Plus)"></div>
              </div>
              <span class="seg-count">{{snapshot.inactiveCustomers90Plus}}</span>
            </div>
          </div>
          <div class="segments-cta" *ngIf="snapshot.inactiveCustomers60Days > 0">
            <button class="btn-primary" (click)="setTab('ai-chat'); quickSend('Reactivate my inactive customers')">
              🤖 Ask AI to reactivate {{snapshot.inactiveCustomers60Days}} inactive customers →
            </button>
          </div>
        </div>

        <!-- Opportunity Cards -->
        <div class="opp-section" *ngIf="opportunities.length > 0">
          <h3 class="card-title">💡 AI-Identified Opportunities</h3>
          <div class="opp-cards-vertical">
            <div class="opp-card-v" *ngFor="let opp of opportunities" [class]="'opp-v-'+opp.priority">
              <div class="opp-v-left">
                <span class="opp-v-icon">{{opp.icon}}</span>
                <div>
                  <div class="opp-v-title">{{opp.title}}</div>
                  <div class="opp-v-desc">{{opp.description}}</div>
                  <div class="opp-v-revenue" *ngIf="opp.estimatedRevenue">💰 {{opp.estimatedRevenue}} estimated</div>
                </div>
              </div>
              <button class="btn-opp-act" (click)="setTab('ai-chat'); quickSend(opp.actionLabel + ' — ' + opp.title)">
                {{opp.actionLabel}} →
              </button>
            </div>
          </div>
        </div>

        <!-- AI Campaign Results -->
        <div class="campaign-results" *ngIf="enhancedMetrics && enhancedMetrics.totalCampaignsSent > 0">
          <h3 class="card-title">📧 AI Campaign Results</h3>
          <div class="campaign-stats">
            <div class="cs-item"><span class="cs-v">{{enhancedMetrics.totalCampaignsSent}}</span><span class="cs-l">Campaigns Sent</span></div>
            <div class="cs-item"><span class="cs-v">{{enhancedMetrics.totalCampaignBookings}}</span><span class="cs-l">Bookings Generated</span></div>
            <div class="cs-item"><span class="cs-v">\${{enhancedMetrics.totalCampaignRevenue | number:'1.0-0'}}</span><span class="cs-l">Revenue from Campaigns</span></div>
            <div class="cs-item"><span class="cs-v">\${{enhancedMetrics.aiInfluencedRevenue | number:'1.0-0'}}</span><span class="cs-l">Total AI Revenue Impact</span></div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           AI OPERATIONS LOG
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='ai-ops'" class="ai-ops-section">
        <div class="section-header">
          <div>
            <h2 class="section-title">AI Operations Log</h2>
            <p class="section-sub">Full audit trail of every AI agent action — proposed, approved, and executed.</p>
          </div>
          <button class="btn-refresh-sm" (click)="loadAIOperations()">🔄 Refresh</button>
        </div>

        <div *ngIf="aiOperations.length === 0" class="empty-state">
          <div class="empty-icon">🤖</div>
          <h3>No AI actions yet</h3>
          <p>Use the AI Business Chat to get started. Every action will be logged here.</p>
          <button class="btn-primary" (click)="setTab('ai-chat')">Open AI Chat →</button>
        </div>

        <div class="ops-timeline">
          <div *ngFor="let op of aiOperations" class="ops-item" [class]="'ops-status-' + op.status.toLowerCase()">
            <div class="ops-time-col">
              <div class="ops-date">{{op.createdAt | date:'MMM d'}}</div>
              <div class="ops-time">{{op.createdAt | date:'h:mm a'}}</div>
            </div>
            <div class="ops-connector"><div class="ops-dot" [class]="'dot-' + op.status.toLowerCase()"></div></div>
            <div class="ops-content">
              <div class="ops-header">
                <div class="ops-agents">
                  <span *ngFor="let agent of op.agentChain.split(' → ')" class="ops-agent-badge">{{agent}}</span>
                </div>
                <span class="ops-status-badge" [class]="'status-' + op.status.toLowerCase()">{{op.status}}</span>
              </div>
              <div class="ops-intent">"{{op.ownerIntent}}"</div>
              <div class="ops-reasoning">{{op.reasoningSummary}}</div>
              <div class="ops-metrics" *ngIf="op.status === 'Completed'">
                <span class="ops-metric-chip" *ngIf="op.actualBookings > 0">📅 {{op.actualBookings}} bookings</span>
                <span class="ops-metric-chip" *ngIf="op.actualRevenue > 0">💰 \${{op.actualRevenue | number:'1.0-0'}} revenue</span>
                <span class="ops-metric-chip" *ngIf="op.targetCustomerCount > 0">👥 {{op.targetCustomerCount}} customers reached</span>
              </div>
              <div class="ops-estimated" *ngIf="op.status === 'AwaitingApproval' || op.status === 'Proposed'">
                <span class="est-chip">Est: {{op.estimatedImpact}}</span>
                <span class="est-chip">Est Revenue: \${{op.estimatedRevenue | number:'1.0-0'}}</span>
                <span class="risk-chip" [class]="'risk-' + op.riskLevel.toLowerCase()">{{op.riskLevel}} Risk</span>
              </div>
              <div class="ops-failure" *ngIf="op.failureReason">⚠️ {{op.failureReason}}</div>
            </div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           OVERVIEW (original, kept intact)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='overview'">
        <h2 class="section-title">Business Overview &amp; Metrics</h2>
        <div class="metrics-grid" *ngIf="metrics">
          <div class="metric-card">
            <span class="m-icon">👥</span>
            <div class="m-value">{{metrics.totalLeads}}</div>
            <div class="m-label">Total Leads</div>
          </div>
          <div class="metric-card">
            <span class="m-icon">📬</span>
            <div class="m-value">{{metrics.pendingBookingRequests}}</div>
            <div class="m-label">Pending Requests</div>
          </div>
          <div class="metric-card">
            <span class="m-icon">✅</span>
            <div class="m-value">{{metrics.confirmedBookings}}</div>
            <div class="m-label">Confirmed Bookings</div>
          </div>
          <div class="metric-card">
            <span class="m-icon">📈</span>
            <div class="m-value">{{metrics.conversionRatePercentage}}%</div>
            <div class="m-label">Conversion Rate</div>
          </div>
          <div class="metric-card">
            <span class="m-icon">🤖</span>
            <div class="m-value">{{metrics.totalAIInteractions}}</div>
            <div class="m-label">AI Interactions</div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           BOOKING REQUESTS (original, kept intact)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='requests'">
        <div class="section-header">
          <h2 class="section-title">Booking Requests Queue</h2>
          <div class="filter-tabs">
            <button [class.active]="statusFilter==='ALL'" (click)="statusFilter='ALL'">All ({{allRequests.length}})</button>
            <button [class.active]="statusFilter==='PENDING'" (click)="statusFilter='PENDING'">Pending ({{countByStatus('PENDING')}})</button>
            <button [class.active]="statusFilter==='CONFIRMED'" (click)="statusFilter='CONFIRMED'">Confirmed ({{countByStatus('CONFIRMED')}})</button>
            <button [class.active]="statusFilter==='CONFLICT'" (click)="statusFilter='CONFLICT'">Conflicts ({{countByStatus('CONFLICT')}})</button>
            <button [class.active]="statusFilter==='REJECTED'" (click)="statusFilter='REJECTED'">Rejected ({{countByStatus('REJECTED')}})</button>
          </div>
        </div>
        <div *ngIf="filteredRequests.length===0" class="empty-state">
          <p>🎉 No requests matching '{{statusFilter}}'</p>
          <button class="btn-primary" (click)="statusFilter='ALL'">Show All ({{allRequests.length}})</button>
        </div>
        <div class="request-cards">
          <div *ngFor="let req of filteredRequests" class="request-card" [ngClass]="getStatusCardClass(req.status)">
            <div class="req-header">
              <div>
                <h3 class="req-title">{{req.proposedSlotSummary}}</h3>
                <span class="req-service">\${{req.servicePrice}} | {{req.serviceName}} ({{req.serviceDurationMinutes}} min)</span>
              </div>
              <span class="status-badge-pill" [ngClass]="getStatusBadgeClass(req.status)">{{getStatusLabel(req.status)}}</span>
            </div>
            <div class="req-body">
              <p>👤 <strong>Customer:</strong> {{req.leadName}} ({{req.leadEmail}})</p>
              <p *ngIf="req.leadPhone">📞 <strong>Phone:</strong> {{req.leadPhone}}</p>
              <p class="req-time">🕒 Requested: {{req.createdAt | date:'medium'}}</p>
              <p *ngIf="req.googleCalendarEventId">📅 <strong>Calendar Event ID:</strong> {{req.googleCalendarEventId}}</p>
              <span *ngIf="req.emailDeliveryStatus" class="email-badge" [ngClass]="getEmailStatusBadgeClass(req.emailDeliveryStatus)">
                {{getEmailStatusLabel(req.emailDeliveryStatus)}}
              </span>
            </div>
            <div class="req-actions" *ngIf="isPendingOrConflict(req.status)">
              <button class="btn-approve" id="btn-book-approve-{{req.id}}" [disabled]="processingId===req.id" (click)="approveRequest(req.id)">
                {{processingId===req.id ? '⏳ Processing...' : '✅ Approve & Confirm'}}
              </button>
              <button class="btn-reject" [disabled]="processingId===req.id" (click)="rejectRequest(req.id)">✕ Reject</button>
              <button class="btn-retry-email" *ngIf="req.emailDeliveryStatus==='Failed'" [disabled]="processingId===req.id" (click)="retryEmail(req.id)">📧 Retry Email</button>
            </div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           SERVICES (original, kept intact)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='services'">
        <div class="section-header">
          <h2 class="section-title">Services Setup</h2>
          <button class="btn-primary" (click)="showServiceForm=!showServiceForm">+ Add Service</button>
        </div>
        <div class="service-form card" *ngIf="showServiceForm">
          <h3>New Service</h3>
          <div class="form-grid">
            <div class="form-group"><label>Name *</label><input [(ngModel)]="newServiceName" placeholder="e.g. Personal Training Session" /></div>
            <div class="form-group"><label>Price ($) *</label><input type="number" [(ngModel)]="newServicePrice" /></div>
            <div class="form-group"><label>Duration (min) *</label><input type="number" [(ngModel)]="newServiceDuration" /></div>
            <div class="form-group"><label>Description</label><textarea [(ngModel)]="newServiceDesc" rows="2"></textarea></div>
          </div>
          <div class="form-actions">
            <button class="btn-primary" (click)="createService()">✓ Create Service</button>
            <button class="btn-secondary" (click)="showServiceForm=false">Cancel</button>
          </div>
        </div>
        <div class="services-list">
          <div *ngFor="let s of services" class="service-row">
            <div class="service-info">
              <span class="svc-name">{{s.name}}</span>
              <span class="svc-meta">\${{s.price}} • {{s.durationMinutes}} min</span>
              <span class="svc-desc">{{s.description}}</span>
            </div>
            <button class="btn-danger-sm" (click)="deleteService(s.id)">Delete</button>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           AVAILABILITY (original, kept intact)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='availability'">
        <h2 class="section-title">Working Availability</h2>
        <div class="availability-list">
          <div *ngFor="let rule of availability" class="avail-row">
            <span class="avail-day">{{getDayName(rule.dayOfWeek)}}</span>
            <span class="avail-time">{{rule.startTime}} – {{rule.endTime}}</span>
            <span class="avail-buffer">{{rule.bufferMinutes}}min buffer</span>
            <span class="avail-status" [class.active-rule]="rule.isActive">{{rule.isActive ? 'Active' : 'Inactive'}}</span>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           CALENDAR (original, kept intact)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='calendar'">
        <h2 class="section-title">Google Calendar Integration</h2>
        <div class="card calendar-card">
          <div *ngIf="business?.isCalendarConnected" class="calendar-connected">
            <span class="cal-icon">📅</span>
            <div>
              <div class="cal-status">✅ Google Calendar Connected</div>
              <div class="cal-id">Calendar ID: {{business?.googleCalendarId}}</div>
            </div>
          </div>
          <div *ngIf="!business?.isCalendarConnected" class="calendar-disconnected">
            <p>Connect Google Calendar to enable real-time availability checking and automatic event creation.</p>
            <button class="btn-google" (click)="connectGoogleCalendar()">🔗 Connect Google Calendar</button>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           SETTINGS (original, kept intact)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='settings'">
        <h2 class="section-title">Business Settings</h2>
        <div class="card settings-card" *ngIf="business">
          <div class="form-grid">
            <div class="form-group"><label>Business Name</label><input [(ngModel)]="business.name" /></div>
            <div class="form-group"><label>Contact Email</label><input [(ngModel)]="business.contactEmail" /></div>
            <div class="form-group"><label>Location</label><input [(ngModel)]="business.location" /></div>
            <div class="form-group"><label>Timezone</label><input [(ngModel)]="business.timeZone" /></div>
            <div class="form-group full"><label>Description</label><textarea [(ngModel)]="business.description" rows="2"></textarea></div>
            <div class="form-group full"><label>Cancellation Policy</label><textarea [(ngModel)]="business.cancellationPolicy" rows="2"></textarea></div>
            <div class="form-group full"><label>Communication Tone</label><input [(ngModel)]="business.communicationTone" /></div>
          </div>
          <button class="btn-primary" (click)="saveSettings()">💾 Save Settings</button>
        </div>
      </section>

    </main>
  </div>

  <!-- Business Onboarding Modal -->
  <div class="modal-overlay" *ngIf="showBusinessForm">
    <div class="modal-card">
      <h3>Create New Business Profile</h3>
      <div class="form-group" style="margin-bottom: 12px; display: flex; flex-direction: column;">
        <label style="font-size: 12px; color: #94a3b8; margin-bottom: 4px;">Business Name *</label>
        <input [(ngModel)]="newBusinessName" placeholder="e.g. Alpha Yoga Studio" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(99,102,241,0.25); border-radius: 8px; color: #e2e8f0; padding: 10px 12px; font-size: 14px;" />
      </div>
      <div class="form-group" style="margin-bottom: 12px; display: flex; flex-direction: column;">
        <label style="font-size: 12px; color: #94a3b8; margin-bottom: 4px;">Description</label>
        <textarea [(ngModel)]="newBusinessDesc" placeholder="e.g. Boutique yoga and meditation studio" rows="2" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(99,102,241,0.25); border-radius: 8px; color: #e2e8f0; padding: 10px 12px; font-size: 14px; font-family: inherit;"></textarea>
      </div>
      <div class="form-group" style="margin-bottom: 12px; display: flex; flex-direction: column;">
        <label style="font-size: 12px; color: #94a3b8; margin-bottom: 4px;">Location</label>
        <input [(ngModel)]="newBusinessLoc" placeholder="e.g. Bangalore, India" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(99,102,241,0.25); border-radius: 8px; color: #e2e8f0; padding: 10px 12px; font-size: 14px;" />
      </div>
      <div class="form-group" style="margin-bottom: 16px; display: flex; flex-direction: column;">
        <label style="font-size: 12px; color: #94a3b8; margin-bottom: 4px;">Contact Email *</label>
        <input type="email" [(ngModel)]="newBusinessEmail" placeholder="e.g. contact@alphayoga.com" style="background: rgba(255,255,255,0.05); border: 1px solid rgba(99,102,241,0.25); border-radius: 8px; color: #e2e8f0; padding: 10px 12px; font-size: 14px;" />
      </div>
      <div class="form-actions" style="display: flex; gap: 10px; margin-top: 16px;">
        <button class="btn-primary" (click)="createBusiness()" [disabled]="!newBusinessName || !newBusinessEmail">Create Business</button>
        <button class="btn-secondary" (click)="showBusinessForm = false">Cancel</button>
      </div>
    </div>
  </div>
</div>
  `,
  styles: [`
/* ═══════════════════════════════════════════════════════════════════════════
   WORKPILOT AI DASHBOARD — PREMIUM DESIGN
   ═══════════════════════════════════════════════════════════════════════════ */

:host { display: block; font-family: 'Segoe UI', Inter, -apple-system, sans-serif; }

.dashboard-page {
  min-height: 100vh;
  background: #0a0a0f;
  color: #e2e8f0;
  display: flex;
  flex-direction: column;
}

.business-selector {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(99, 102, 241, 0.35);
  border-radius: 8px;
  color: #e2e8f0;
  padding: 6px 12px;
  font-size: 13px;
  outline: none;
  font-weight: 600;
  cursor: pointer;
  margin-right: 8px;
  font-family: inherit;
}
.business-selector option {
  background: #1e293b;
  color: #e2e8f0;
}
.btn-create-biz {
  background: rgba(99, 102, 241, 0.15);
  border: 1px solid rgba(99, 102, 241, 0.35);
  color: #a5b4fc;
  padding: 6px 12px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 12px;
  font-weight: bold;
  transition: all 0.2s;
}
.btn-create-biz:hover {
  background: rgba(99, 102, 241, 0.3);
  color: white;
}
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal-card {
  background: #1e293b;
  border: 1px solid rgba(99, 102, 241, 0.3);
  border-radius: 16px;
  padding: 24px;
  width: 100%;
  max-width: 460px;
  box-shadow: 0 20px 25px -5px rgba(0,0,0,0.5);
}
.modal-card h3 { margin: 0 0 16px 0; color: #f1f5f9; font-size: 18px; font-weight: 700; }

/* ── Top Nav ─────────────────────────────────────────────────────────────── */
.top-nav {
  background: rgba(16,16,28,0.95);
  border-bottom: 1px solid rgba(99,102,241,0.2);
  padding: 0 24px;
  height: 60px;
  display: flex;
  align-items: center;
  gap: 16px;
  backdrop-filter: blur(10px);
  position: sticky;
  top: 0;
  z-index: 100;
}

.brand { display: flex; align-items: center; gap: 10px; flex-shrink: 0; }
.logo { font-size: 22px; filter: drop-shadow(0 0 8px #6366f1); }
.title { font-size: 18px; font-weight: 700; color: #e2e8f0; letter-spacing: -0.3px; }
.ai-badge {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  font-size: 10px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 6px;
  vertical-align: middle;
  margin-left: 2px;
}

.header-center { display: flex; gap: 8px; flex: 1; justify-content: center; }
.snap-pill {
  background: rgba(99,102,241,0.12);
  border: 1px solid rgba(99,102,241,0.25);
  color: #a5b4fc;
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 20px;
  white-space: nowrap;
}
.snap-pill.revenue { background: rgba(16,185,129,0.12); border-color: rgba(16,185,129,0.25); color: #6ee7b7; }
.snap-pill.warn { background: rgba(245,158,11,0.12); border-color: rgba(245,158,11,0.25); color: #fcd34d; }

.header-right { display: flex; align-items: center; gap: 12px; margin-left: auto; flex-shrink: 0; }
.btn-pub-link {
  color: #a5b4fc;
  text-decoration: none;
  font-size: 12px;
  padding: 6px 12px;
  border: 1px solid rgba(99,102,241,0.3);
  border-radius: 6px;
  transition: all 0.2s;
}
.btn-pub-link:hover { background: rgba(99,102,241,0.15); color: #c7d2fe; }
.business-badge { display: flex; align-items: center; gap: 8px; }
.b-name { font-weight: 600; font-size: 13px; color: #c7d2fe; }
.status-pill { font-size: 11px; color: #94a3b8; padding: 3px 8px; border-radius: 20px; background: rgba(255,255,255,0.04); }
.status-pill.connected { color: #6ee7b7; }

/* ── Layout ──────────────────────────────────────────────────────────────── */
.layout-body { display: flex; flex: 1; overflow: hidden; height: calc(100vh - 60px); }

/* ── Sidebar ─────────────────────────────────────────────────────────────── */
.sidebar {
  width: 220px;
  flex-shrink: 0;
  background: rgba(16,16,28,0.8);
  border-right: 1px solid rgba(99,102,241,0.15);
  padding: 16px 8px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow-y: auto;
}

.sidebar-section-label {
  font-size: 10px;
  font-weight: 700;
  color: #4b5563;
  letter-spacing: 0.1em;
  padding: 12px 12px 4px;
  text-transform: uppercase;
}

.sidebar button {
  width: 100%;
  padding: 10px 12px;
  background: transparent;
  border: none;
  color: #94a3b8;
  text-align: left;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.15s;
  position: relative;
}
.sidebar button:hover { background: rgba(99,102,241,0.1); color: #c7d2fe; }
.sidebar button.active { background: rgba(99,102,241,0.2); color: #a5b4fc; font-weight: 600; border-left: 3px solid #6366f1; }
.nav-icon { font-size: 14px; }
.nav-new-badge {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  font-size: 9px;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 4px;
  margin-left: auto;
}
.badge-count {
  background: #ef4444;
  color: white;
  font-size: 10px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 10px;
  margin-left: auto;
}

/* ── Content Area ────────────────────────────────────────────────────────── */
.content-area { flex: 1; overflow-y: auto; padding: 24px 28px; }

.section-title { font-size: 22px; font-weight: 700; color: #f1f5f9; margin: 0 0 4px; }
.section-sub { color: #64748b; font-size: 14px; margin: 0 0 24px; }
.section-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 20px; }

/* ── AI Chat Section ─────────────────────────────────────────────────────── */
.ai-chat-section { display: flex; flex-direction: column; height: calc(100vh - 60px - 48px); }

.ai-chat-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.ai-status-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  color: #6ee7b7;
  font-size: 12px;
  background: rgba(16,185,129,0.1);
  border: 1px solid rgba(16,185,129,0.25);
  padding: 6px 12px;
  border-radius: 20px;
}

.pulse-dot {
  width: 8px; height: 8px;
  background: #10b981;
  border-radius: 50%;
  animation: pulse 2s infinite;
}
@keyframes pulse { 0%, 100% { opacity: 1; transform: scale(1); } 50% { opacity: 0.5; transform: scale(1.3); } }

/* Opportunity Strip */
.opportunity-strip { margin-bottom: 16px; }
.opp-label { font-size: 12px; color: #64748b; margin-bottom: 10px; text-transform: uppercase; letter-spacing: 0.05em; }
.opp-cards { display: flex; gap: 12px; overflow-x: auto; padding-bottom: 4px; }

.opp-card {
  flex-shrink: 0;
  width: 240px;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: 12px;
  padding: 14px;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.opp-card:hover { background: rgba(99,102,241,0.1); border-color: rgba(99,102,241,0.4); transform: translateY(-2px); }
.opp-card.opp-high { border-color: rgba(239,68,68,0.3); }
.opp-card.opp-medium { border-color: rgba(245,158,11,0.3); }
.opp-card.opp-low { border-color: rgba(99,102,241,0.25); }
.opp-icon { font-size: 20px; }
.opp-body { flex: 1; }
.opp-title { font-size: 13px; font-weight: 600; color: #e2e8f0; }
.opp-desc { font-size: 11px; color: #64748b; margin-top: 4px; line-height: 1.4; }
.opp-revenue { font-size: 11px; color: #6ee7b7; margin-top: 4px; }
.opp-action { font-size: 11px; color: #6366f1; font-weight: 600; }

/* Chat Messages */
.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 12px 0;
  display: flex;
  flex-direction: column;
  gap: 16px;
  scroll-behavior: smooth;
}

.chat-welcome {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  text-align: center;
  padding: 40px 20px;
}
.welcome-icon { font-size: 48px; margin-bottom: 16px; filter: drop-shadow(0 0 20px #6366f1); }
.chat-welcome h3 { font-size: 20px; color: #e2e8f0; margin: 0 0 8px; }
.chat-welcome p { color: #64748b; margin: 0 0 24px; }
.starter-chips { display: flex; flex-wrap: wrap; gap: 8px; justify-content: center; }
.chip {
  background: rgba(99,102,241,0.12);
  border: 1px solid rgba(99,102,241,0.3);
  color: #a5b4fc;
  padding: 8px 14px;
  border-radius: 20px;
  cursor: pointer;
  font-size: 13px;
  transition: all 0.2s;
}
.chip:hover { background: rgba(99,102,241,0.25); border-color: #6366f1; color: white; }

.chat-message { display: flex; }
.chat-message.owner-msg { justify-content: flex-end; }
.chat-message.ai-msg { justify-content: flex-start; }

.msg-bubble {
  max-width: 600px;
  padding: 12px 16px;
  border-radius: 14px;
  font-size: 14px;
  line-height: 1.6;
}
.owner-bubble {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  border-radius: 14px 14px 4px 14px;
}
.msg-ai-wrapper { display: flex; gap: 12px; max-width: 90%; }
.ai-avatar {
  width: 36px; height: 36px;
  background: linear-gradient(135deg, #1e1b4b, #312e81);
  border: 2px solid rgba(99,102,241,0.4);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}
.msg-ai-content { flex: 1; display: flex; flex-direction: column; gap: 6px; }

.agent-chain { display: flex; gap: 4px; flex-wrap: wrap; }
.agent-step {
  font-size: 10px;
  padding: 2px 8px;
  border-radius: 10px;
  background: rgba(99,102,241,0.15);
  border: 1px solid rgba(99,102,241,0.25);
  color: #a5b4fc;
}
.agent-step.fail { background: rgba(239,68,68,0.1); border-color: rgba(239,68,68,0.25); color: #fca5a5; }

.ai-bubble {
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(99,102,241,0.2);
  color: #e2e8f0;
  border-radius: 4px 14px 14px 14px;
}

.msg-text { white-space: pre-wrap; }
.msg-time { font-size: 10px; color: #475569; margin-top: 4px; }

/* Typing Indicator */
.typing-indicator {
  display: flex;
  gap: 4px;
  padding: 14px 20px;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.15);
  border-radius: 4px 14px 14px 14px;
  width: fit-content;
}
.typing-indicator span {
  width: 8px; height: 8px;
  background: #6366f1;
  border-radius: 50%;
  animation: bounce 1.2s infinite;
}
.typing-indicator span:nth-child(2) { animation-delay: 0.2s; }
.typing-indicator span:nth-child(3) { animation-delay: 0.4s; }
@keyframes bounce { 0%, 60%, 100% { transform: translateY(0); } 30% { transform: translateY(-6px); } }

/* Action Plan Card */
.action-plan-card {
  background: rgba(16,16,28,0.8);
  border: 1px solid rgba(99,102,241,0.3);
  border-radius: 12px;
  overflow: hidden;
  margin-top: 12px;
}
.action-plan-card.risk-medium { border-color: rgba(245,158,11,0.4); }
.action-plan-card.risk-high { border-color: rgba(239,68,68,0.4); }

.plan-header { padding: 14px 16px; background: rgba(99,102,241,0.08); display: flex; align-items: center; gap: 10px; }
.plan-risk-badge { font-size: 11px; font-weight: 700; padding: 4px 10px; border-radius: 20px; }
.risk-badge-low { background: rgba(16,185,129,0.15); color: #6ee7b7; }
.risk-badge-medium { background: rgba(245,158,11,0.15); color: #fcd34d; }
.risk-badge-high { background: rgba(239,68,68,0.15); color: #fca5a5; }
.plan-title { font-weight: 600; color: #e2e8f0; font-size: 14px; }

.plan-metrics { display: flex; gap: 0; border-bottom: 1px solid rgba(99,102,241,0.1); }
.plan-metric { flex: 1; padding: 12px 14px; text-align: center; border-right: 1px solid rgba(99,102,241,0.1); }
.plan-metric:last-child { border-right: none; }
.pm-label { display: block; font-size: 10px; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 4px; }
.pm-value { font-size: 18px; font-weight: 700; color: #e2e8f0; }
.pm-value.green { color: #6ee7b7; }

.plan-details { padding: 10px 16px; border-bottom: 1px solid rgba(99,102,241,0.08); }
.plan-details summary { font-size: 12px; color: #a5b4fc; cursor: pointer; user-select: none; }
.plan-detail-text { font-size: 13px; color: #94a3b8; margin-top: 8px; line-height: 1.5; white-space: pre-wrap; }

.plan-actions { display: flex; gap: 10px; padding: 14px 16px; }
.btn-approve {
  background: linear-gradient(135deg, #10b981, #059669);
  color: white;
  border: none;
  padding: 10px 24px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: all 0.2s;
  flex: 1;
}
.btn-approve:hover:not(:disabled) { filter: brightness(1.1); transform: translateY(-1px); }
.btn-approve:disabled { opacity: 0.6; cursor: not-allowed; }

.btn-reject-action {
  background: transparent;
  border: 1px solid rgba(239,68,68,0.4);
  color: #f87171;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}
.btn-reject-action:hover:not(:disabled) { background: rgba(239,68,68,0.1); }

.plan-status-done { padding: 12px 16px; color: #6ee7b7; font-size: 13px; }
.plan-status-rejected { padding: 12px 16px; color: #f87171; font-size: 13px; }

/* Chat Input */
.chat-input-area {
  padding: 14px 0 0;
  border-top: 1px solid rgba(99,102,241,0.15);
  margin-top: 8px;
}
.chat-input-row { display: flex; gap: 10px; }
.chat-textarea {
  flex: 1;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.25);
  border-radius: 10px;
  color: #e2e8f0;
  padding: 12px 14px;
  font-size: 14px;
  resize: none;
  outline: none;
  transition: border-color 0.2s;
  font-family: inherit;
}
.chat-textarea:focus { border-color: #6366f1; }
.chat-textarea::placeholder { color: #475569; }
.chat-send-btn {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  border: none;
  padding: 12px 20px;
  border-radius: 10px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: all 0.2s;
  min-width: 80px;
}
.chat-send-btn:hover:not(:disabled) { filter: brightness(1.1); transform: translateY(-1px); }
.chat-send-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.chat-hint { font-size: 11px; color: #374151; margin-top: 6px; }

/* ── Insights ────────────────────────────────────────────────────────────── */
.kpi-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px; margin-bottom: 28px; }

.kpi-card {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: 14px;
  padding: 20px;
  transition: all 0.2s;
}
.kpi-card:hover { border-color: rgba(99,102,241,0.4); background: rgba(99,102,241,0.06); }
.kpi-card.revenue-card { border-color: rgba(16,185,129,0.3); background: rgba(16,185,129,0.04); }
.kpi-card.warn-card { border-color: rgba(239,68,68,0.3); background: rgba(239,68,68,0.04); }
.kpi-card.ai-card { border-color: rgba(139,92,246,0.3); background: rgba(139,92,246,0.04); }

.kpi-icon { font-size: 24px; margin-bottom: 10px; }
.kpi-value { font-size: 30px; font-weight: 800; color: #f1f5f9; line-height: 1; }
.kpi-label { font-size: 12px; color: #64748b; margin-top: 6px; text-transform: uppercase; letter-spacing: 0.04em; }
.kpi-change { font-size: 12px; margin-top: 4px; }
.kpi-change.positive { color: #6ee7b7; }
.kpi-change.negative { color: #f87171; }
.kpi-sub { font-size: 12px; color: #475569; margin-top: 4px; }

/* Customer Segments */
.segments-card {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: 14px;
  padding: 24px;
  margin-bottom: 24px;
}
.card-title { font-size: 16px; font-weight: 700; color: #e2e8f0; margin: 0 0 18px; }
.segment-bars { display: flex; flex-direction: column; gap: 12px; }
.segment-row { display: flex; align-items: center; gap: 12px; }
.seg-label { font-size: 13px; color: #94a3b8; width: 280px; flex-shrink: 0; }
.seg-bar-bg { flex: 1; height: 8px; background: rgba(255,255,255,0.08); border-radius: 4px; overflow: hidden; }
.seg-bar { height: 100%; border-radius: 4px; transition: width 0.5s; }
.seg-active { background: linear-gradient(90deg, #10b981, #6ee7b7); }
.seg-warn { background: linear-gradient(90deg, #f59e0b, #fcd34d); }
.seg-danger { background: linear-gradient(90deg, #ef4444, #f87171); }
.seg-critical { background: linear-gradient(90deg, #991b1b, #dc2626); }
.seg-count { font-size: 13px; font-weight: 700; color: #e2e8f0; width: 30px; text-align: right; }
.segments-cta { margin-top: 16px; }

/* Vertical Opportunity Cards */
.opp-section { margin-bottom: 24px; }
.opp-cards-vertical { display: flex; flex-direction: column; gap: 12px; }
.opp-card-v {
  display: flex;
  align-items: center;
  gap: 16px;
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: 12px;
  padding: 16px 20px;
  transition: all 0.2s;
}
.opp-card-v:hover { border-color: rgba(99,102,241,0.4); }
.opp-card-v.opp-v-high { border-left: 4px solid #ef4444; }
.opp-card-v.opp-v-medium { border-left: 4px solid #f59e0b; }
.opp-card-v.opp-v-low { border-left: 4px solid #6366f1; }
.opp-v-left { display: flex; align-items: flex-start; gap: 14px; flex: 1; }
.opp-v-icon { font-size: 24px; flex-shrink: 0; }
.opp-v-title { font-size: 14px; font-weight: 600; color: #e2e8f0; }
.opp-v-desc { font-size: 13px; color: #64748b; margin-top: 4px; }
.opp-v-revenue { font-size: 13px; color: #6ee7b7; margin-top: 4px; }
.btn-opp-act {
  background: rgba(99,102,241,0.15);
  border: 1px solid rgba(99,102,241,0.3);
  color: #a5b4fc;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  transition: all 0.2s;
  flex-shrink: 0;
}
.btn-opp-act:hover { background: rgba(99,102,241,0.3); color: white; }

/* Campaign Stats */
.campaign-results { background: rgba(255,255,255,0.04); border: 1px solid rgba(99,102,241,0.2); border-radius: 14px; padding: 24px; }
.campaign-stats { display: flex; gap: 32px; }
.cs-item { display: flex; flex-direction: column; gap: 4px; }
.cs-v { font-size: 24px; font-weight: 800; color: #f1f5f9; }
.cs-l { font-size: 11px; color: #64748b; text-transform: uppercase; letter-spacing: 0.04em; }

/* ── AI Operations ────────────────────────────────────────────────────────── */
.ai-ops-section {}

.ops-timeline { display: flex; flex-direction: column; gap: 0; }
.ops-item {
  display: flex;
  gap: 0;
  position: relative;
}
.ops-item::before {
  content: '';
  position: absolute;
  left: 80px;
  top: 28px;
  bottom: -20px;
  width: 2px;
  background: rgba(99,102,241,0.1);
}
.ops-item:last-child::before { display: none; }

.ops-time-col { width: 72px; padding: 16px 0; flex-shrink: 0; text-align: right; }
.ops-date { font-size: 11px; color: #64748b; }
.ops-time { font-size: 11px; color: #475569; }

.ops-connector { width: 20px; display: flex; flex-direction: column; align-items: center; padding: 16px 0; flex-shrink: 0; }
.ops-dot {
  width: 12px; height: 12px;
  border-radius: 50%;
  border: 2px solid;
  flex-shrink: 0;
  margin-top: 4px;
}
.dot-completed { border-color: #10b981; background: #10b981; }
.dot-awaitingapproval { border-color: #f59e0b; background: transparent; }
.dot-executing { border-color: #6366f1; background: #6366f1; }
.dot-proposed { border-color: #64748b; background: transparent; }
.dot-rejected { border-color: #ef4444; background: transparent; }
.dot-failed { border-color: #ef4444; background: #ef4444; }

.ops-content {
  flex: 1;
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(99,102,241,0.15);
  border-radius: 10px;
  padding: 14px 16px;
  margin: 8px 0 16px;
}
.ops-header { display: flex; align-items: center; gap: 10px; margin-bottom: 8px; }
.ops-agents { display: flex; gap: 4px; flex-wrap: wrap; flex: 1; }
.ops-agent-badge { font-size: 10px; background: rgba(99,102,241,0.15); border: 1px solid rgba(99,102,241,0.25); color: #a5b4fc; padding: 2px 8px; border-radius: 10px; }
.ops-status-badge { font-size: 11px; font-weight: 600; padding: 3px 10px; border-radius: 10px; }
.status-completed { background: rgba(16,185,129,0.15); color: #6ee7b7; }
.status-awaitingapproval { background: rgba(245,158,11,0.15); color: #fcd34d; }
.status-executing { background: rgba(99,102,241,0.15); color: #a5b4fc; }
.status-proposed { background: rgba(100,116,139,0.15); color: #94a3b8; }
.status-rejected { background: rgba(239,68,68,0.12); color: #f87171; }
.status-failed { background: rgba(239,68,68,0.2); color: #fca5a5; }
.ops-intent { font-size: 13px; color: #a5b4fc; margin-bottom: 4px; font-style: italic; }
.ops-reasoning { font-size: 12px; color: #64748b; margin-bottom: 8px; }
.ops-metrics { display: flex; gap: 8px; flex-wrap: wrap; }
.ops-metric-chip { font-size: 12px; background: rgba(16,185,129,0.1); border: 1px solid rgba(16,185,129,0.2); color: #6ee7b7; padding: 3px 10px; border-radius: 12px; }
.ops-estimated { display: flex; gap: 8px; flex-wrap: wrap; }
.est-chip { font-size: 12px; background: rgba(99,102,241,0.1); border: 1px solid rgba(99,102,241,0.2); color: #a5b4fc; padding: 3px 10px; border-radius: 12px; }
.risk-chip { font-size: 12px; padding: 3px 10px; border-radius: 12px; }
.risk-low { background: rgba(16,185,129,0.1); color: #6ee7b7; }
.risk-medium { background: rgba(245,158,11,0.1); color: #fcd34d; }
.risk-high { background: rgba(239,68,68,0.1); color: #f87171; }
.ops-failure { font-size: 12px; color: #f87171; margin-top: 6px; }

/* ── Metrics Grid (Overview) ─────────────────────────────────────────────── */
.metrics-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 16px; }
.metric-card {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(99,102,241,0.2);
  border-radius: 14px;
  padding: 20px;
  text-align: center;
}
.m-icon { font-size: 28px; display: block; margin-bottom: 10px; }
.m-value { font-size: 32px; font-weight: 800; color: #f1f5f9; }
.m-label { font-size: 12px; color: #64748b; margin-top: 6px; text-transform: uppercase; letter-spacing: 0.04em; }

/* ── Booking Request Cards ────────────────────────────────────────────────── */
.request-cards { display: flex; flex-direction: column; gap: 14px; }
.request-card {
  background: rgba(255,255,255,0.03);
  border: 1px solid rgba(99,102,241,0.15);
  border-radius: 12px;
  padding: 18px 20px;
  border-left: 4px solid rgba(99,102,241,0.3);
}
.request-card.pending-border { border-left-color: #f59e0b; }
.request-card.approved-border { border-left-color: #10b981; }
.request-card.rejected-border { border-left-color: #ef4444; }
.request-card.conflict-border { border-left-color: #dc2626; }

.req-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 12px; }
.req-title { font-size: 15px; font-weight: 700; color: #e2e8f0; margin: 0 0 4px; }
.req-service { font-size: 13px; color: #64748b; }
.status-badge-pill { font-size: 12px; font-weight: 600; padding: 4px 12px; border-radius: 20px; white-space: nowrap; }
.pill-pending { background: rgba(245,158,11,0.15); color: #fcd34d; }
.pill-approved { background: rgba(16,185,129,0.15); color: #6ee7b7; }
.pill-rejected { background: rgba(239,68,68,0.12); color: #f87171; }
.pill-conflict { background: rgba(239,68,68,0.2); color: #fca5a5; }

.req-body p { font-size: 13px; color: #94a3b8; margin: 4px 0; }
.req-time { color: #64748b !important; font-size: 12px !important; }
.email-badge { display: inline-block; font-size: 11px; padding: 2px 10px; border-radius: 10px; margin-top: 6px; }
.email-sent { background: rgba(16,185,129,0.12); color: #6ee7b7; }
.email-simulated { background: rgba(99,102,241,0.12); color: #a5b4fc; }
.email-failed { background: rgba(239,68,68,0.12); color: #f87171; }
.email-none { background: rgba(100,116,139,0.12); color: #94a3b8; }

.req-actions { display: flex; gap: 10px; margin-top: 14px; }
.btn-reject { background: transparent; border: 1px solid rgba(239,68,68,0.3); color: #f87171; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 13px; transition: all 0.2s; }
.btn-reject:hover:not(:disabled) { background: rgba(239,68,68,0.1); }
.btn-retry-email { background: transparent; border: 1px solid rgba(99,102,241,0.3); color: #a5b4fc; padding: 8px 16px; border-radius: 8px; cursor: pointer; font-size: 13px; }

/* ── Services ────────────────────────────────────────────────────────────── */
.services-list { display: flex; flex-direction: column; gap: 10px; margin-top: 16px; }
.service-row { background: rgba(255,255,255,0.04); border: 1px solid rgba(99,102,241,0.2); border-radius: 10px; padding: 14px 16px; display: flex; align-items: center; gap: 16px; }
.service-info { flex: 1; display: flex; flex-direction: column; gap: 3px; }
.svc-name { font-weight: 600; color: #e2e8f0; font-size: 14px; }
.svc-meta { font-size: 13px; color: #6366f1; }
.svc-desc { font-size: 12px; color: #64748b; }

/* ── Availability ────────────────────────────────────────────────────────── */
.availability-list { display: flex; flex-direction: column; gap: 8px; }
.avail-row { background: rgba(255,255,255,0.04); border: 1px solid rgba(99,102,241,0.2); border-radius: 8px; padding: 12px 16px; display: flex; align-items: center; gap: 16px; }
.avail-day { font-weight: 600; color: #e2e8f0; width: 100px; }
.avail-time { color: #a5b4fc; flex: 1; }
.avail-buffer { font-size: 12px; color: #64748b; }
.avail-status { font-size: 12px; color: #475569; }
.active-rule { color: #6ee7b7 !important; }

/* ── Cards ────────────────────────────────────────────────────────────────── */
.card { background: rgba(255,255,255,0.04); border: 1px solid rgba(99,102,241,0.2); border-radius: 14px; padding: 24px; }
.service-form { margin-bottom: 20px; }
.service-form h3 { margin: 0 0 16px; color: #e2e8f0; font-size: 16px; }
.calendar-card {}
.calendar-connected { display: flex; align-items: center; gap: 16px; }
.cal-icon { font-size: 32px; }
.cal-status { font-weight: 600; color: #6ee7b7; }
.cal-id { font-size: 12px; color: #64748b; margin-top: 4px; }
.calendar-disconnected { text-align: center; }
.calendar-disconnected p { color: #64748b; margin-bottom: 16px; }
.btn-google { background: white; color: #1a1a1a; border: none; padding: 10px 20px; border-radius: 8px; cursor: pointer; font-size: 14px; font-weight: 600; display: flex; align-items: center; gap: 8px; margin: 0 auto; }
.settings-card {}
.settings-card button { margin-top: 16px; }

/* ── Forms ────────────────────────────────────────────────────────────────── */
.form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-group.full { grid-column: 1 / -1; }
.form-group label { font-size: 12px; color: #64748b; text-transform: uppercase; letter-spacing: 0.05em; }
.form-group input, .form-group textarea, .form-group select {
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(99,102,241,0.25);
  border-radius: 8px;
  color: #e2e8f0;
  padding: 10px 12px;
  font-size: 14px;
  outline: none;
  transition: border-color 0.2s;
  font-family: inherit;
}
.form-group input:focus, .form-group textarea:focus { border-color: #6366f1; }
.form-actions { display: flex; gap: 10px; margin-top: 16px; }

/* ── Buttons ──────────────────────────────────────────────────────────────── */
.btn-primary {
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  transition: all 0.2s;
}
.btn-primary:hover { filter: brightness(1.1); transform: translateY(-1px); }
.btn-secondary { background: transparent; border: 1px solid rgba(99,102,241,0.3); color: #a5b4fc; padding: 10px 20px; border-radius: 8px; cursor: pointer; font-size: 14px; }
.btn-danger-sm { background: rgba(239,68,68,0.1); border: 1px solid rgba(239,68,68,0.3); color: #f87171; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 12px; }
.btn-refresh-sm { background: transparent; border: 1px solid rgba(99,102,241,0.25); color: #94a3b8; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 12px; }
.btn-refresh-sm:hover { background: rgba(99,102,241,0.1); }

/* ── Filter Tabs ──────────────────────────────────────────────────────────── */
.filter-tabs { display: flex; gap: 6px; }
.filter-tabs button { background: transparent; border: 1px solid rgba(99,102,241,0.2); color: #64748b; padding: 6px 12px; border-radius: 6px; cursor: pointer; font-size: 12px; transition: all 0.15s; }
.filter-tabs button.active { background: rgba(99,102,241,0.2); border-color: rgba(99,102,241,0.4); color: #a5b4fc; }

/* ── Empty State ──────────────────────────────────────────────────────────── */
.empty-state { text-align: center; padding: 60px 20px; }
.empty-icon { font-size: 48px; margin-bottom: 16px; }
.empty-state h3 { color: #e2e8f0; margin: 0 0 8px; }
.empty-state p { color: #64748b; margin: 0 0 20px; }

/* ── Spinner ─────────────────────────────────────────────────────────────── */
.spinner { animation: spin 1s linear infinite; display: inline-block; }
@keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
  `]
})
export class OwnerDashboardComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatContainer') chatContainer!: ElementRef;

  get businessId(): string {
    return this.businessContext.getBusinessId();
  }

  allBusinesses: Business[] = [];

  activeTab = 'ai-chat';
  business: Business | null = null;
  services: ServiceItem[] = [];
  availability: AvailabilityRule[] = [];
  allRequests: BookingRequest[] = [];
  metrics: DashboardMetrics | null = null;
  snapshot: BusinessSnapshot | null = null;
  enhancedMetrics: EnhancedMetrics | null = null;

  // AI Chat state
  chatMessages: OwnerChatMessage[] = [];
  chatInput = '';
  isAiThinking = false;
  executingActionId: string | null = null;

  // Opportunities
  opportunities: OpportunityCard[] = [];

  // AI Operations
  aiOperations: AIAgentActionLog[] = [];

  // Booking queue state
  statusFilter = 'ALL';
  processingId: string | null = null;

  // Service form
  showServiceForm = false;
  newServiceName = '';
  newServicePrice = 85;
  newServiceDuration = 60;
  newServiceDesc = '';

  // Business onboarding form
  showBusinessForm = false;
  newBusinessName = '';
  newBusinessDesc = '';
  newBusinessLoc = '';
  newBusinessEmail = '';

  private shouldScrollChat = false;

  constructor(
    private api: ApiService,
    private businessContext: BusinessContextService
  ) {}

  ngOnInit(): void {
    this.loadBusinesses();
    this.loadDashboardData();
    this.loadOpportunities();
    this.loadSnapshot();
    this.loadEnhancedMetrics();
  }

  ngOnDestroy(): void {}

  ngAfterViewChecked(): void {
    if (this.shouldScrollChat) {
      this.scrollChatToBottom();
      this.shouldScrollChat = false;
    }
  }

  setTab(tab: string): void {
    this.activeTab = tab;
    if (tab === 'ai-ops') this.loadAIOperations();
    if (tab === 'insights') { this.loadSnapshot(); this.loadEnhancedMetrics(); }
  }

  loadBusinesses(): void {
    this.api.getBusinesses().subscribe({
      next: (list) => {
        this.allBusinesses = list;
      },
      error: () => {}
    });
  }

  switchBusiness(id: string): void {
    this.businessContext.setBusinessId(id);
    this.chatMessages = [];
    this.opportunities = [];
    this.aiOperations = [];
    this.loadDashboardData();
    this.loadOpportunities();
    this.loadSnapshot();
    this.loadEnhancedMetrics();
  }

  createBusiness(): void {
    if (!this.newBusinessName || !this.newBusinessEmail) return;
    this.api.createBusiness({
      name: this.newBusinessName,
      description: this.newBusinessDesc,
      location: this.newBusinessLoc,
      contactEmail: this.newBusinessEmail,
      timeZone: 'UTC',
      cancellationPolicy: '24 hours notice',
      communicationTone: 'Friendly'
    }).subscribe({
      next: (b) => {
        this.showBusinessForm = false;
        this.newBusinessName = '';
        this.newBusinessDesc = '';
        this.newBusinessLoc = '';
        this.newBusinessEmail = '';
        this.loadBusinesses();
        this.switchBusiness(b.id);
      }
    });
  }

  loadDashboardData(): void {
    this.api.getBusiness(this.businessId).subscribe({
      next: b => this.business = b,
      error: () => {
        const defaultBusinessId = '11111111-1111-1111-1111-111111111111';
        if (this.businessId !== defaultBusinessId) {
          this.businessContext.clearBusinessId();
          this.loadDashboardData();
          this.loadOpportunities();
          this.loadSnapshot();
          this.loadEnhancedMetrics();
        }
      }
    });
    this.api.getServices(this.businessId).subscribe({
      next: s => this.services = s,
      error: () => {}
    });
    this.api.getAvailability(this.businessId).subscribe({
      next: a => this.availability = a,
      error: () => {}
    });
    this.api.getAllBookingRequests(this.businessId).subscribe({
      next: r => this.allRequests = r,
      error: () => {}
    });
    this.api.getMetrics(this.businessId).subscribe({
      next: m => this.metrics = m,
      error: () => {}
    });
  }

  loadOpportunities(): void {
    this.api.getOpportunities(this.businessId).subscribe({
      next: opps => this.opportunities = opps,
      error: () => {}
    });
  }

  loadSnapshot(): void {
    this.api.getBusinessSnapshot(this.businessId).subscribe({
      next: s => this.snapshot = s,
      error: () => {}
    });
  }

  loadEnhancedMetrics(): void {
    this.api.getEnhancedMetrics(this.businessId).subscribe({
      next: m => this.enhancedMetrics = m,
      error: () => {}
    });
  }

  loadAIOperations(): void {
    this.api.getAIOperations(this.businessId).subscribe({
      next: ops => this.aiOperations = ops,
      error: () => {}
    });
  }

  // ─── Owner Chat ────────────────────────────────────────────────────────────

  greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'morning';
    if (h < 17) return 'afternoon';
    return 'evening';
  }

  quickSend(msg: string): void {
    this.chatInput = msg;
    this.sendOwnerMessage();
  }

  onEnter(e: Event): void {
    const ke = e as KeyboardEvent;
    if (!ke.shiftKey) {
      e.preventDefault();
      this.sendOwnerMessage();
    }
  }

  sendOwnerMessage(): void {
    const msg = this.chatInput.trim();
    if (!msg || this.isAiThinking) return;

    this.chatInput = '';
    this.isAiThinking = true;

    // Add owner message
    this.chatMessages.push({ role: 'owner', content: msg, timestamp: new Date() });

    // Add typing indicator
    const typingMsg: OwnerChatMessage = { role: 'ai', content: '', timestamp: new Date(), isTyping: true };
    this.chatMessages.push(typingMsg);
    this.shouldScrollChat = true;

    this.api.ownerChat(this.businessId, msg).subscribe({
      next: (res) => {
        // Remove typing indicator
        this.chatMessages = this.chatMessages.filter(m => !m.isTyping);
        this.isAiThinking = false;

        const aiMsg: OwnerChatMessage = {
          role: 'ai',
          content: res.assistantMessage,
          timestamp: new Date(),
          actionPlan: res.actionPlan || undefined,
          agentChain: res.agentChain,
          requiresApproval: res.requiresApproval,
          actionId: res.actionId || undefined,
          isTyping: false
        };
        this.chatMessages.push(aiMsg);
        this.shouldScrollChat = true;

        // Update snapshot if returned
        if (res.businessSnapshot) this.snapshot = res.businessSnapshot;
        if (res.opportunities?.length) this.opportunities = res.opportunities;
      },
      error: () => {
        this.chatMessages = this.chatMessages.filter(m => !m.isTyping);
        this.isAiThinking = false;
        this.chatMessages.push({
          role: 'ai',
          content: 'I had trouble connecting. Please try again in a moment.',
          timestamp: new Date(),
          isTyping: false
        });
        this.shouldScrollChat = true;
      }
    });
  }

  approveAction(plan: AIActionPlan): void {
    if (this.executingActionId) return;
    this.executingActionId = plan.actionId;

    this.api.executeAction(this.businessId, plan.actionId, 'Approved by owner').subscribe({
      next: (result: ExecuteActionResult) => {
        this.executingActionId = null;
        plan.status = 'Completed';

        const resultMsg: OwnerChatMessage = {
          role: 'ai',
          content: `✅ **Action Complete!**\n\n${result.message}\n\n` +
            (result.customersReached > 0 ? `👥 Customers reached: **${result.customersReached}**\n` : '') +
            (result.bookingRequestsGenerated > 0 ? `📅 Booking requests: **${result.bookingRequestsGenerated}**\n` : '') +
            (result.revenueImpact > 0 ? `💰 Revenue impact: **$${result.revenueImpact.toFixed(0)}**\n` : '') +
            `\nThis has been logged to your AI Operations dashboard.`,
          timestamp: new Date(),
          agentChain: result.executionSteps,
          isTyping: false
        };
        this.chatMessages.push(resultMsg);
        this.shouldScrollChat = true;
        this.loadAIOperations();
        this.loadEnhancedMetrics();
        this.loadSnapshot();
      },
      error: (err) => {
        this.executingActionId = null;
        const errMsg = err.error?.error || 'Execution failed. Please try again.';
        this.chatMessages.push({
          role: 'ai',
          content: `⚠️ Action failed: ${errMsg}`,
          timestamp: new Date(),
          isTyping: false
        });
        this.shouldScrollChat = true;
      }
    });
  }

  rejectAction(plan: AIActionPlan): void {
    const reason = window.prompt('Why are you rejecting this action? (optional)') || 'Rejected by owner';
    plan.status = 'Rejected';

    this.api.rejectAction(this.businessId, plan.actionId, reason).subscribe({
      next: () => {
        this.chatMessages.push({
          role: 'ai',
          content: `✕ Action rejected. I'll note this for future recommendations. Is there something else you'd like me to try?`,
          timestamp: new Date(),
          isTyping: false
        });
        this.shouldScrollChat = true;
      },
      error: () => {}
    });
  }

  formatMessage(text: string): string {
    // Convert **bold** and line breaks to HTML
    return text
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }

  private scrollChatToBottom(): void {
    try {
      if (this.chatContainer) {
        this.chatContainer.nativeElement.scrollTop = this.chatContainer.nativeElement.scrollHeight;
      }
    } catch {}
  }

  // ─── Booking Queue ─────────────────────────────────────────────────────────

  get pendingCount(): number { return this.countByStatus('PENDING'); }

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
        alert(`✅ Booking Confirmed!\nCalendar Event: ${res.googleCalendarEventId || 'Simulated'}\nEmail: ${res.emailDeliveryStatus || 'Sent'}`);
        this.statusFilter = 'ALL';
        this.loadDashboardData();
      },
      error: (err) => {
        this.processingId = null;
        alert(`⚠️ Could not approve: ${err.error?.error || 'Server error.'}`);
        this.loadDashboardData();
      }
    });
  }

  retryEmail(id: string): void {
    if (this.processingId === id) return;
    this.processingId = id;
    this.api.retryBookingEmail(id).subscribe({
      next: (res) => { this.processingId = null; alert(`Email retry: ${res.emailDeliveryStatus}`); this.loadDashboardData(); },
      error: (err) => { this.processingId = null; alert(`Retry failed: ${err.error?.error}`); }
    });
  }

  rejectRequest(id: string): void {
    if (this.processingId === id) return;
    const reason = prompt('Reason for rejection:', 'Slot not available');
    if (!reason) return;
    this.processingId = id;
    this.api.rejectBookingRequest(id, reason).subscribe({
      next: () => { this.processingId = null; this.loadDashboardData(); },
      error: () => { this.processingId = null; }
    });
  }

  // ─── Services ─────────────────────────────────────────────────────────────

  createService(): void {
    if (!this.newServiceName) return;
    this.api.createService(this.businessId, {
      name: this.newServiceName,
      price: this.newServicePrice,
      durationMinutes: this.newServiceDuration,
      description: this.newServiceDesc
    }).subscribe({ next: () => { this.showServiceForm = false; this.newServiceName = ''; this.loadDashboardData(); } });
  }

  deleteService(id: string): void {
    if (confirm('Delete this service?')) {
      this.api.deleteService(id).subscribe(() => this.loadDashboardData());
    }
  }

  // ─── Calendar & Settings ───────────────────────────────────────────────────

  connectGoogleCalendar(): void {
    this.api.getCalendarConnectUrl(this.businessId).subscribe(res => { window.location.href = res.authorizationUrl; });
  }

  saveSettings(): void {
    if (!this.business) return;
    this.api.updateBusiness(this.businessId, this.business).subscribe(() => alert('Settings saved.'));
  }

  // ─── Utilities ────────────────────────────────────────────────────────────

  getSegmentPct(count: number): number {
    if (!this.snapshot || this.snapshot.totalCustomers === 0) return 0;
    return Math.round((count / this.snapshot.totalCustomers) * 100);
  }

  getRiskIcon(risk: string): string {
    if (risk === 'Low') return '✅';
    if (risk === 'High') return '🔴';
    return '⚠️';
  }

  getDayName(day: number): string {
    return ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][day] || 'Day';
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
    const n = this.getStatusNormalized(status);
    if (n === 'PENDING') return '⏳ Pending Approval';
    if (n === 'CONFIRMED') return '✅ Confirmed';
    if (n === 'REJECTED') return '✕ Rejected';
    if (n === 'CONFLICT') return '⚠️ Conflict';
    return 'Pending';
  }

  getStatusBadgeClass(status: any): string {
    const n = this.getStatusNormalized(status);
    if (n === 'PENDING') return 'pill-pending';
    if (n === 'CONFIRMED') return 'pill-approved';
    if (n === 'REJECTED') return 'pill-rejected';
    if (n === 'CONFLICT') return 'pill-conflict';
    return 'pill-pending';
  }

  getStatusCardClass(status: any): string {
    const n = this.getStatusNormalized(status);
    if (n === 'PENDING') return 'pending-border';
    if (n === 'CONFIRMED') return 'approved-border';
    if (n === 'REJECTED') return 'rejected-border';
    if (n === 'CONFLICT') return 'conflict-border';
    return 'pending-border';
  }

  isPendingOrConflict(status: any): boolean {
    const n = this.getStatusNormalized(status);
    return n === 'PENDING' || n === 'CONFLICT';
  }

  getEmailStatusLabel(status?: string): string {
    if (status === 'Sent') return '✓ Email Sent';
    if (status === 'Simulated') return '⚡ Simulated';
    if (status === 'Failed') return '✕ Failed';
    return 'Not Sent';
  }

  getEmailStatusBadgeClass(status?: string): string {
    if (status === 'Sent') return 'email-sent';
    if (status === 'Simulated') return 'email-simulated';
    if (status === 'Failed') return 'email-failed';
    return 'email-none';
  }
}
