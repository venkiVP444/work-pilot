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
      <div class="brand-logo-glow">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon></svg>
      </div>
      <span class="title">WorkPilot <span class="ai-badge">AI OS</span></span>
    </div>
    
    <div class="header-center" *ngIf="snapshot">
      <span class="snap-pill">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle><path d="M23 21v-2a4 4 0 0 0-3-3.87"></path><path d="M16 3.13a4 4 0 0 1 0 7.75"></path></svg>
        {{snapshot.totalCustomers}} customers
      </span>
      <span class="snap-pill revenue">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="1" x2="12" y2="23"></line><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path></svg>
        \${{snapshot.revenueThisMonth | number:'1.0-0'}}/mo
      </span>
      <span class="snap-pill warn" *ngIf="snapshot.inactiveCustomers60Days > 0">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>
        {{snapshot.inactiveCustomers60Days}} inactive 60d+
      </span>
    </div>

    <div class="header-right">
      <a class="btn-pub-link" [routerLink]="['/book', businessId]" target="_blank">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
        Booking Page ↗
      </a>
      <div class="business-badge" *ngIf="business">
        <select class="business-selector" [ngModel]="businessId" (ngModelChange)="switchBusiness($event)">
          <option *ngFor="let b of allBusinesses" [value]="b.id">{{ b.name }}</option>
        </select>
        <button class="btn-create-biz" (click)="showBusinessForm = true">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
          New
        </button>
      </div>
    </div>
  </header>

  <div class="layout-body">

    <!-- ── Sidebar ────────────────────────────────────────────────────────── -->
    <aside class="sidebar">
      <div class="sidebar-section-label">AI BUSINESS OS</div>
      <button id="nav-ai-chat" [class.active]="activeTab==='ai-chat'" (click)="setTab('ai-chat')">
        <span class="nav-icon ai-nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2a2 2 0 0 1 2 2v2a2 2 0 0 1-2 2 2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z"></path><rect x="4" y="8" width="16" height="12" rx="2"></rect><path d="M9 13v2"></path><path d="M15 13v2"></path></svg>
        </span>
        AI Business Chat
        <span class="nav-new-badge">HERO</span>
      </button>
      <button id="nav-insights" [class.active]="activeTab==='insights'" (click)="setTab('insights')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>
        </span>
        Business Insights
      </button>
      <button id="nav-ai-ops" [class.active]="activeTab==='ai-ops'" (click)="setTab('ai-ops')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline></svg>
        </span>
        AI Operations
      </button>

      <div class="sidebar-section-label">BOOKINGS & MANAGEMENT</div>
      <button id="nav-overview" [class.active]="activeTab==='overview'" (click)="setTab('overview')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect></svg>
        </span>
        Overview
      </button>
      <button id="nav-requests" [class.active]="activeTab==='requests'" (click)="setTab('requests')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg>
        </span>
        Booking Requests
        <span *ngIf="pendingCount>0" class="badge-count">{{pendingCount}}</span>
      </button>
      <button id="nav-services" [class.active]="activeTab==='services'" (click)="setTab('services')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="6" cy="6" r="3"></circle><circle cx="6" cy="18" r="3"></circle><line x1="20" y1="4" x2="8.12" y2="15.88"></line><line x1="14.47" y1="14.48" x2="20" y2="20"></line><line x1="8.12" y1="8.12" x2="12" y2="12"></line></svg>
        </span>
        Services
      </button>
      <button id="nav-availability" [class.active]="activeTab==='availability'" (click)="setTab('availability')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>
        </span>
        Availability
      </button>
      <button id="nav-calendar" [class.active]="activeTab==='calendar'" (click)="setTab('calendar')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
        </span>
        Google Calendar
      </button>
      <button id="nav-settings" [class.active]="activeTab==='settings'" (click)="setTab('settings')">
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path></svg>
        </span>
        Settings
      </button>
    </aside>

    <!-- ── Main Content ──────────────────────────────────────────────────── -->
    <main class="content-area">

      <!-- ═══════════════════════════════════════════════════════════════════
           AI BUSINESS CHAT (VISUAL HERO)
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='ai-chat'" class="ai-chat-section hero-chat-container">
        <div class="ai-chat-header">
          <div>
            <h2 class="section-title flex-title">
              AI Autonomous Operator
              <span class="ai-hero-tag">Active Agent Suite</span>
            </h2>
            <p class="section-sub">Describe your business target — AI agents will analyze data, design plans, and execute outreach.</p>
          </div>
          <div class="ai-status-badge glowing-hero-status">
            <span class="live-beacon"></span> Autonomous Orchestrator Online
          </div>
        </div>

        <!-- Opportunity Cards (Morning Brief) -->
        <div class="opportunity-strip" *ngIf="opportunities.length > 0 && chatMessages.length === 0">
          <div class="opp-label">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
            Morning AI Brief — Recommended Business Actions
          </div>
          <div class="opp-cards">
            <div class="opp-card" *ngFor="let opp of opportunities" [class]="'opp-'+opp.priority"
                 (click)="quickSend('Help me ' + opp.actionLabel.toLowerCase())">
              <div class="opp-card-head">
                <span class="opp-icon">{{opp.icon}}</span>
                <span class="opp-action">{{opp.actionLabel}} →</span>
              </div>
              <div class="opp-body">
                <div class="opp-title">{{opp.title}}</div>
                <div class="opp-desc">{{opp.description}}</div>
                <div class="opp-revenue" *ngIf="opp.estimatedRevenue">Potential: {{opp.estimatedRevenue}}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Chat Messages Container -->
        <div class="chat-messages hero-chat-messages" #chatContainer>
          <div *ngIf="chatMessages.length === 0" class="chat-welcome">
            <div class="welcome-icon-glow">
              <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2a2 2 0 0 1 2 2v2a2 2 0 0 1-2 2 2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z"></path><rect x="4" y="8" width="16" height="12" rx="2"></rect><path d="M9 13v2"></path><path d="M15 13v2"></path></svg>
            </div>
            <h3>Good {{greeting()}}, I'm your AI Business Operator</h3>
            <p>I monitor customer history, availability gaps, and revenue goals in real-time. Pick an objective below or type a custom command:</p>
            <div class="starter-chips">
              <button class="chip" (click)="quickSend('I need to make 20% more profit this month')">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="1" x2="12" y2="23"></line><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path></svg>
                Grow profit 20%
              </button>
              <button class="chip" (click)="quickSend('How is my business performing?')">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="20" x2="18" y2="10"></line><line x1="12" y1="20" x2="12" y2="4"></line><line x1="6" y1="20" x2="6" y2="14"></line></svg>
                Business performance
              </button>
              <button class="chip" (click)="quickSend('Help me reactivate inactive customers')">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>
                Reactivate inactive customers
              </button>
              <button class="chip" (click)="quickSend('Fill my empty appointment slots this week')">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line></svg>
                Fill empty slots this week
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
                    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"></polyline></svg>
                    {{step.agent}}
                  </span>
                </div>

                <!-- Typing indicator -->
                <div *ngIf="msg.isTyping" class="typing-indicator">
                  <span></span><span></span><span></span>
                </div>

                <div *ngIf="!msg.isTyping" class="msg-bubble ai-bubble hero-ai-bubble">
                  <div class="msg-text" [innerHTML]="formatMessage(msg.content)"></div>

                  <!-- Action Plan Card -->
                  <div *ngIf="msg.actionPlan" class="action-plan-card hero-action-card" [class]="'risk-' + msg.actionPlan.riskLevel.toLowerCase()">
                    <div class="plan-header">
                      <div class="plan-title-block">
                        <span class="plan-hero-badge">AI Action Proposal</span>
                        <div class="plan-title">{{msg.actionPlan.title}}</div>
                      </div>
                      <div class="plan-risk-badge" [class]="'risk-badge-' + msg.actionPlan.riskLevel.toLowerCase()">
                        {{getRiskIcon(msg.actionPlan.riskLevel)}} {{msg.actionPlan.riskLevel}} Risk
                      </div>
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

                    <details class="plan-details" open>
                      <summary>📋 What will happen?</summary>
                      <p class="plan-detail-text">{{msg.actionPlan.whatWillHappen}}</p>
                    </details>
                    <details class="plan-details">
                      <summary>💡 Why this recommendation?</summary>
                      <p class="plan-detail-text">{{msg.actionPlan.whyRecommended}}</p>
                    </details>

                    <!-- Approval Buttons -->
                    <div class="plan-actions" *ngIf="msg.actionPlan.status === 'AwaitingApproval' || msg.actionPlan.status === 'Proposed'">
                      <button class="btn-approve btn-saas btn-primary" id="btn-approve-{{msg.actionPlan.actionId}}"
                              [disabled]="executingActionId === msg.actionPlan.actionId"
                              (click)="approveAction(msg.actionPlan)">
                        <span *ngIf="executingActionId !== msg.actionPlan.actionId">
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"></polyline></svg>
                          Approve & Execute
                        </span>
                        <span *ngIf="executingActionId === msg.actionPlan.actionId" class="spinner">⏳ Executing Orchestration...</span>
                      </button>
                      <button class="btn-reject-action btn-saas btn-ghost" id="btn-reject-{{msg.actionPlan.actionId}}"
                              [disabled]="executingActionId === msg.actionPlan.actionId"
                              (click)="rejectAction(msg.actionPlan)">
                        ✕ Reject
                      </button>
                    </div>
                    <div class="plan-status-done" *ngIf="msg.actionPlan.status === 'Completed'">
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"></polyline></svg>
                      Action completed successfully
                    </div>
                    <div class="plan-status-rejected" *ngIf="msg.actionPlan.status === 'Rejected'">
                      ✕ Action rejected by owner
                    </div>
                  </div>

                  <div class="msg-time">{{msg.timestamp | date:'shortTime'}}</div>
                </div>
              </div>
            </div>

          </div>
        </div>

        <!-- Chat Input -->
        <div class="chat-input-area hero-chat-input-area">
          <div class="chat-input-row">
            <textarea
              id="owner-chat-input"
              class="chat-textarea"
              [(ngModel)]="chatInput"
              placeholder="Tell your AI Operator what to do... 'I need more revenue', 'Reactivate inactive clients', 'Fill open slots'"
              rows="2"
              (keydown.enter)="onEnter($event)"
              [disabled]="isAiThinking"
            ></textarea>
            <button class="chat-send-btn btn-saas btn-primary" id="btn-send-ai-chat"
                    [disabled]="!chatInput.trim() || isAiThinking"
                    (click)="sendOwnerMessage()">
              <span *ngIf="!isAiThinking">
                Send
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>
              </span>
              <span *ngIf="isAiThinking" class="spinner">⏳</span>
            </button>
          </div>
          <div class="chat-hint">Press Enter to send • Shift+Enter for line break • AI agent chain executes automatically</div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           BUSINESS INSIGHTS
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='insights'" class="insights-section">
        <h2 class="section-title">Business Insights</h2>
        <p class="section-sub">Live metrics, customer retention cohorts, and AI growth recommendations.</p>

        <!-- KPI Cards -->
        <div class="kpi-grid" *ngIf="enhancedMetrics">
          <div class="kpi-card revenue-card">
            <div class="kpi-header">
              <span class="kpi-label">Revenue This Month</span>
              <span class="kpi-icon-svg"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="1" x2="12" y2="23"></line><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path></svg></span>
            </div>
            <div class="kpi-value">\${{enhancedMetrics.revenueThisMonth | number:'1.0-0'}}</div>
            <div class="kpi-change" [class.positive]="enhancedMetrics.revenueGrowthPercent >= 0" [class.negative]="enhancedMetrics.revenueGrowthPercent < 0">
              {{enhancedMetrics.revenueGrowthPercent >= 0 ? '↑' : '↓'}} {{enhancedMetrics.revenueGrowthPercent | number:'1.1-1'}}% vs last month
            </div>
          </div>
          <div class="kpi-card">
            <div class="kpi-header">
              <span class="kpi-label">Total Customers</span>
              <span class="kpi-icon-svg"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg></span>
            </div>
            <div class="kpi-value">{{enhancedMetrics.totalCustomers}}</div>
            <div class="kpi-sub">{{enhancedMetrics.activeCustomers}} active this month</div>
          </div>
          <div class="kpi-card warn-card" *ngIf="enhancedMetrics.inactiveCustomers > 0">
            <div class="kpi-header">
              <span class="kpi-label">Inactive 60+ Days</span>
              <span class="kpi-icon-svg warn-icon"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path></svg></span>
            </div>
            <div class="kpi-value">{{enhancedMetrics.inactiveCustomers}}</div>
            <div class="kpi-sub">Target for AI Chat reactivation</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-header">
              <span class="kpi-label">Bookings This Month</span>
              <span class="kpi-icon-svg"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line></svg></span>
            </div>
            <div class="kpi-value">{{enhancedMetrics.bookingsThisMonth}}</div>
            <div class="kpi-sub">Avg \${{enhancedMetrics.averageOrderValue | number:'1.0-0'}}/session</div>
          </div>
          <div class="kpi-card ai-card" *ngIf="enhancedMetrics.aiActionsExecuted > 0">
            <div class="kpi-header">
              <span class="kpi-label">AI Actions Executed</span>
              <span class="kpi-icon-svg ai-icon"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"></polygon></svg></span>
            </div>
            <div class="kpi-value">{{enhancedMetrics.aiActionsExecuted}}</div>
            <div class="kpi-sub">\${{enhancedMetrics.aiInfluencedRevenue | number:'1.0-0'}} AI-influenced revenue</div>
          </div>
          <div class="kpi-card">
            <div class="kpi-header">
              <span class="kpi-label">Lead Conversion Rate</span>
              <span class="kpi-icon-svg"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline><polyline points="17 6 23 6 23 12"></polyline></svg></span>
            </div>
            <div class="kpi-value">{{enhancedMetrics.conversionRatePercentage}}%</div>
            <div class="kpi-sub">{{enhancedMetrics.confirmedBookings}} confirmed bookings</div>
          </div>
        </div>

        <!-- Customer Segments -->
        <div class="segments-card" *ngIf="snapshot">
          <h3 class="card-title">Customer Retention Cohorts</h3>
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
              <span class="seg-label">Inactive 60–89 days <span class="badge badge-rose">Primary Target</span></span>
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
            <button class="btn-saas btn-primary" (click)="setTab('ai-chat'); quickSend('Reactivate my inactive customers')">
              Ask AI to reactivate {{snapshot.inactiveCustomers60Days}} inactive customers →
            </button>
          </div>
        </div>

        <!-- Opportunity Cards -->
        <div class="opp-section" *ngIf="opportunities.length > 0">
          <h3 class="card-title">AI-Identified Growth Opportunities</h3>
          <div class="opp-cards-vertical">
            <div class="opp-card-v" *ngFor="let opp of opportunities" [class]="'opp-v-'+opp.priority">
              <div class="opp-v-left">
                <span class="opp-v-icon">{{opp.icon}}</span>
                <div>
                  <div class="opp-v-title">{{opp.title}}</div>
                  <div class="opp-v-desc">{{opp.description}}</div>
                  <div class="opp-v-revenue" *ngIf="opp.estimatedRevenue">Est Revenue: {{opp.estimatedRevenue}}</div>
                </div>
              </div>
              <button class="btn-saas btn-secondary" (click)="setTab('ai-chat'); quickSend(opp.actionLabel + ' — ' + opp.title)">
                {{opp.actionLabel}} →
              </button>
            </div>
          </div>
        </div>

        <!-- AI Campaign Results -->
        <div class="campaign-results" *ngIf="enhancedMetrics && enhancedMetrics.totalCampaignsSent > 0">
          <h3 class="card-title">AI Campaign Performance</h3>
          <div class="campaign-stats">
            <div class="cs-item"><span class="cs-v">{{enhancedMetrics.totalCampaignsSent}}</span><span class="cs-l">Campaigns Sent</span></div>
            <div class="cs-item"><span class="cs-v">{{enhancedMetrics.totalCampaignBookings}}</span><span class="cs-l">Bookings Generated</span></div>
            <div class="cs-item"><span class="cs-v">\${{enhancedMetrics.totalCampaignRevenue | number:'1.0-0'}}</span><span class="cs-l">Campaign Revenue</span></div>
            <div class="cs-item"><span class="cs-v">\${{enhancedMetrics.aiInfluencedRevenue | number:'1.0-0'}}</span><span class="cs-l">Total AI Impact</span></div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           AI OPERATIONS LOG
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='ai-ops'" class="ai-ops-section">
        <div class="section-header">
          <div>
            <h2 class="section-title">AI Agent Audit &amp; Operations Log</h2>
            <p class="section-sub">Full audit trail of every AI agent decision, approval, and execution step.</p>
          </div>
          <button class="btn-saas btn-secondary" (click)="loadAIOperations()">Refresh Audit Log</button>
        </div>

        <div *ngIf="aiOperations.length === 0" class="empty-state">
          <div class="empty-icon">🤖</div>
          <h3>No AI actions logged yet</h3>
          <p>Use the AI Business Chat to generate and execute autonomous business plans.</p>
          <button class="btn-saas btn-primary" (click)="setTab('ai-chat')">Open AI Business Chat →</button>
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
                <span class="badge" [class.badge-emerald]="op.status==='Completed'" [class.badge-amber]="op.status==='Proposed'||op.status==='AwaitingApproval'" [class.badge-rose]="op.status==='Rejected'">
                  {{op.status}}
                </span>
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
           OVERVIEW
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='overview'">
        <h2 class="section-title">Business Overview &amp; Metrics</h2>
        <p class="section-sub">Real-time status of lead volume, conversion, and bookings.</p>
        <div class="metrics-grid" *ngIf="metrics">
          <div class="metric-card">
            <span class="m-icon-svg"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg></span>
            <div class="m-value">{{metrics.totalLeads}}</div>
            <div class="m-label">Total Leads</div>
          </div>
          <div class="metric-card">
            <span class="m-icon-svg"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path></svg></span>
            <div class="m-value">{{metrics.pendingBookingRequests}}</div>
            <div class="m-label">Pending Requests</div>
          </div>
          <div class="metric-card">
            <span class="m-icon-svg"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg></span>
            <div class="m-value">{{metrics.confirmedBookings}}</div>
            <div class="m-label">Confirmed Bookings</div>
          </div>
          <div class="metric-card">
            <span class="m-icon-svg"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline></svg></span>
            <div class="m-value">{{metrics.conversionRatePercentage}}%</div>
            <div class="m-label">Conversion Rate</div>
          </div>
          <div class="metric-card">
            <span class="m-icon-svg"><svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 2a2 2 0 0 1 2 2v2a2 2 0 0 1-2 2 2 2 0 0 1-2-2V4a2 2 0 0 1 2-2z"></path><rect x="4" y="8" width="16" height="12" rx="2"></rect></svg></span>
            <div class="m-value">{{metrics.totalAIInteractions}}</div>
            <div class="m-label">AI Interactions</div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           BOOKING REQUESTS
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='requests'">
        <div class="section-header">
          <div>
            <h2 class="section-title">Booking Requests Queue</h2>
            <p class="section-sub">Review, confirm, or reject lead requests.</p>
          </div>
          <div class="filter-tabs">
            <button [class.active]="statusFilter==='ALL'" (click)="statusFilter='ALL'">All ({{allRequests.length}})</button>
            <button [class.active]="statusFilter==='PENDING'" (click)="statusFilter='PENDING'">Pending ({{countByStatus('PENDING')}})</button>
            <button [class.active]="statusFilter==='CONFIRMED'" (click)="statusFilter='CONFIRMED'">Confirmed ({{countByStatus('CONFIRMED')}})</button>
            <button [class.active]="statusFilter==='CONFLICT'" (click)="statusFilter='CONFLICT'">Conflicts ({{countByStatus('CONFLICT')}})</button>
            <button [class.active]="statusFilter==='REJECTED'" (click)="statusFilter='REJECTED'">Rejected ({{countByStatus('REJECTED')}})</button>
          </div>
        </div>
        
        <div *ngIf="filteredRequests.length===0" class="empty-state">
          <p>No requests matching '{{statusFilter}}'</p>
          <button class="btn-saas btn-secondary" (click)="statusFilter='ALL'">Show All ({{allRequests.length}})</button>
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
              <button class="btn-approve btn-saas btn-primary" id="btn-book-approve-{{req.id}}" [disabled]="processingId===req.id" (click)="approveRequest(req.id)">
                {{processingId===req.id ? '⏳ Processing...' : 'Approve & Confirm'}}
              </button>
              <button class="btn-reject btn-saas btn-ghost" [disabled]="processingId===req.id" (click)="rejectRequest(req.id)">✕ Reject</button>
              <button class="btn-retry-email btn-saas btn-secondary" *ngIf="req.emailDeliveryStatus==='Failed'" [disabled]="processingId===req.id" (click)="retryEmail(req.id)">Retry Email</button>
            </div>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           SERVICES
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='services'">
        <div class="section-header">
          <div>
            <h2 class="section-title">Services &amp; Pricing Setup</h2>
            <p class="section-sub">Configure service catalog, rates, and durations.</p>
          </div>
          <button class="btn-saas btn-primary" (click)="showServiceForm=!showServiceForm">+ Add Service</button>
        </div>
        <div class="service-form card" *ngIf="showServiceForm">
          <h3>New Service Definition</h3>
          <div class="form-grid">
            <div class="form-group"><label>Name *</label><input [(ngModel)]="newServiceName" placeholder="e.g. Personal Training Session" /></div>
            <div class="form-group"><label>Price ($) *</label><input type="number" [(ngModel)]="newServicePrice" /></div>
            <div class="form-group"><label>Duration (min) *</label><input type="number" [(ngModel)]="newServiceDuration" /></div>
            <div class="form-group"><label>Description</label><textarea [(ngModel)]="newServiceDesc" rows="2"></textarea></div>
          </div>
          <div class="form-actions">
            <button class="btn-saas btn-primary" (click)="createService()">Create Service</button>
            <button class="btn-saas btn-secondary" (click)="showServiceForm=false">Cancel</button>
          </div>
        </div>
        <div class="services-list">
          <div *ngFor="let s of services" class="service-row">
            <div class="service-info">
              <span class="svc-name">{{s.name}}</span>
              <span class="svc-meta">\${{s.price}} • {{s.durationMinutes}} min</span>
              <span class="svc-desc">{{s.description}}</span>
            </div>
            <button class="btn-saas btn-danger" (click)="deleteService(s.id)">Delete</button>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           AVAILABILITY
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='availability'">
        <h2 class="section-title">Working Hours &amp; Availability Rules</h2>
        <p class="section-sub">Define working windows for calendar slot calculation.</p>
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
           CALENDAR
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='calendar'">
        <h2 class="section-title">Google Calendar Integration</h2>
        <p class="section-sub">Sync live availability and create real calendar invites.</p>
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
            <button class="btn-saas btn-primary" (click)="connectGoogleCalendar()">🔗 Connect Google Calendar</button>
          </div>
        </div>
      </section>

      <!-- ═══════════════════════════════════════════════════════════════════
           SETTINGS
           ═══════════════════════════════════════════════════════════════════ -->
      <section *ngIf="activeTab==='settings'">
        <h2 class="section-title">Business &amp; AI Settings</h2>
        <p class="section-sub">Configure profile details and AI communication preferences.</p>
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
          <button class="btn-saas btn-primary" (click)="saveSettings()">Save Settings</button>
        </div>
      </section>

    </main>
  </div>

  <!-- Business Onboarding Modal -->
  <div class="modal-overlay" *ngIf="showBusinessForm">
    <div class="modal-card">
      <h3>Create New Business Profile</h3>
      <div class="form-group modal-field">
        <label>Business Name *</label>
        <input [(ngModel)]="newBusinessName" placeholder="e.g. Alpha Yoga Studio" />
      </div>
      <div class="form-group modal-field">
        <label>Description</label>
        <textarea [(ngModel)]="newBusinessDesc" placeholder="e.g. Boutique yoga studio" rows="2"></textarea>
      </div>
      <div class="form-group modal-field">
        <label>Location</label>
        <input [(ngModel)]="newBusinessLoc" placeholder="e.g. Bangalore, India" />
      </div>
      <div class="form-group modal-field">
        <label>Contact Email *</label>
        <input type="email" [(ngModel)]="newBusinessEmail" placeholder="e.g. contact@alphayoga.com" />
      </div>
      <div class="form-actions modal-actions">
        <button class="btn-saas btn-primary" (click)="createBusiness()" [disabled]="!newBusinessName || !newBusinessEmail">Create Business</button>
        <button class="btn-saas btn-secondary" (click)="showBusinessForm = false">Cancel</button>
      </div>
    </div>
  </div>
</div>
  `,
  styles: [`
/* ═══════════════════════════════════════════════════════════════════════════
   WORKPILOT AI DASHBOARD — CLEAN ENTERPRISE SAAS DESIGN (80/15/5 Ratio)
   ═══════════════════════════════════════════════════════════════════════════ */

:host { display: block; font-family: var(--font-sans); }

.dashboard-page {
  min-height: 100vh;
  background: var(--bg-canvas);
  color: var(--text-main);
  display: flex;
  flex-direction: column;
}

/* ── Top Nav Header ──────────────────────────────────────────────────────── */
.top-nav {
  background: var(--bg-surface);
  border-bottom: 1px solid var(--border-subtle);
  padding: 0 24px;
  height: 56px;
  display: flex;
  align-items: center;
  gap: 16px;
  position: sticky;
  top: 0;
  z-index: 100;
}

.brand { display: flex; align-items: center; gap: 10px; flex-shrink: 0; }
.brand-logo-glow {
  width: 32px;
  height: 32px;
  background: var(--ai-gradient);
  border-radius: var(--radius-sm);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  box-shadow: var(--shadow-ai-glow);
}

.title { font-size: 16px; font-weight: 700; color: #ffffff; font-family: var(--font-display); letter-spacing: -0.2px; }
.ai-badge {
  background: var(--ai-gradient-glow);
  color: #a5b4fc;
  border: 1px solid rgba(99, 102, 241, 0.3);
  font-size: 10px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: var(--radius-sm);
  vertical-align: middle;
  margin-left: 4px;
}

.header-center { display: flex; gap: 8px; flex: 1; justify-content: center; }
.snap-pill {
  background: var(--bg-canvas);
  border: 1px solid var(--border-medium);
  color: var(--text-muted);
  font-size: 12px;
  font-weight: 500;
  padding: 4px 10px;
  border-radius: var(--radius-full);
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.snap-pill.revenue { border-color: rgba(16,185,129,0.25); color: var(--success-emerald); background: var(--success-bg); }
.snap-pill.warn { border-color: rgba(245,158,11,0.25); color: var(--warning-amber); background: var(--warning-bg); }

.header-right { display: flex; align-items: center; gap: 12px; margin-left: auto; flex-shrink: 0; }
.btn-pub-link {
  color: var(--text-muted);
  text-decoration: none;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-sm);
  transition: all 0.15s ease;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.btn-pub-link:hover { background: var(--bg-surface-hover); color: #ffffff; border-color: var(--border-strong); }

.business-badge { display: flex; align-items: center; gap: 8px; }
.business-selector {
  background: var(--bg-canvas);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-sm);
  color: var(--text-main);
  padding: 5px 10px;
  font-size: 12px;
  outline: none;
  font-weight: 600;
  cursor: pointer;
}
.btn-create-biz {
  background: var(--bg-surface-hover);
  border: 1px solid var(--border-medium);
  color: var(--text-muted);
  padding: 5px 10px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 12px;
  font-weight: 600;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  transition: all 0.15s ease;
}
.btn-create-biz:hover { background: #28354d; color: #ffffff; }

/* ── Layout & Sidebar ────────────────────────────────────────────────────── */
.layout-body { display: flex; flex: 1; overflow: hidden; height: calc(100vh - 56px); }

.sidebar {
  width: 230px;
  flex-shrink: 0;
  background: var(--bg-surface);
  border-right: 1px solid var(--border-subtle);
  padding: 16px 10px;
  display: flex;
  flex-direction: column;
  gap: 3px;
  overflow-y: auto;
}

.sidebar-section-label {
  font-size: 10px;
  font-weight: 700;
  color: var(--text-dim);
  letter-spacing: 0.08em;
  padding: 12px 10px 6px;
  text-transform: uppercase;
}

.sidebar button {
  width: 100%;
  padding: 9px 12px;
  background: transparent;
  border: 1px solid transparent;
  color: var(--text-muted);
  text-align: left;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 10px;
  transition: all 0.15s ease;
}
.sidebar button:hover { background: var(--bg-surface-hover); color: var(--text-main); }
.sidebar button.active { background: var(--bg-canvas); color: #ffffff; font-weight: 600; border-color: var(--border-medium); border-left: 3px solid var(--ai-primary); }

.ai-nav-icon { color: var(--ai-primary); }
.nav-new-badge {
  background: var(--ai-gradient);
  color: #ffffff;
  font-size: 9px;
  font-weight: 700;
  padding: 1px 5px;
  border-radius: 4px;
  margin-left: auto;
}
.badge-count {
  background: var(--danger-rose);
  color: #ffffff;
  font-size: 10px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: var(--radius-full);
  margin-left: auto;
}

.content-area { flex: 1; overflow-y: auto; padding: 24px 32px; background: var(--bg-canvas); }
.section-title { font-size: 20px; font-weight: 700; color: #ffffff; margin: 0 0 4px; font-family: var(--font-display); }
.flex-title { display: flex; align-items: center; gap: 10px; }
.ai-hero-tag { background: var(--ai-gradient-glow); color: #a5b4fc; border: 1px solid rgba(99, 102, 241, 0.3); font-size: 11px; font-weight: 600; padding: 2px 8px; border-radius: var(--radius-full); }
.section-sub { color: var(--text-muted); font-size: 13.5px; margin: 0 0 24px; }
.section-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 20px; }

/* ═══════════════════════════════════════════════════════════════════════════
   AI BUSINESS CHAT (VISUAL HERO DESIGN - 5% AI EMPHASIS)
   ═══════════════════════════════════════════════════════════════════════════ */
.hero-chat-container {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 56px - 48px);
  background: var(--bg-surface);
  border: 1px solid var(--border-medium);
  border-radius: var(--radius-lg);
  padding: 20px;
  box-shadow: 0 0 25px rgba(99, 102, 241, 0.06);
}

.glowing-hero-status {
  background: var(--ai-gradient-glow);
  border: 1px solid rgba(99, 102, 241, 0.3);
  color: #c7d2fe;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 14px;
  border-radius: var(--radius-full);
  display: flex;
  align-items: center;
  gap: 8px;
}

.opportunity-strip { margin-bottom: 16px; }
.opp-label { font-size: 11.5px; font-weight: 600; color: var(--text-muted); margin-bottom: 10px; text-transform: uppercase; letter-spacing: 0.05em; display: flex; align-items: center; gap: 6px; }
.opp-cards { display: flex; gap: 12px; overflow-x: auto; padding-bottom: 4px; }
.opp-card {
  flex-shrink: 0;
  width: 240px;
  background: var(--bg-card);
  border: 1px solid var(--border-subtle);
  border-radius: var(--radius-md);
  padding: 12px;
  cursor: pointer;
  transition: all 0.15s ease;
}
.opp-card:hover { border-color: var(--ai-primary); background: var(--bg-card-hover); transform: translateY(-2px); }
.opp-card-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.opp-icon { font-size: 18px; }
.opp-action { font-size: 11px; font-weight: 600; color: var(--ai-primary); }
.opp-title { font-size: 13px; font-weight: 600; color: #ffffff; }
.opp-desc { font-size: 11.5px; color: var(--text-muted); margin-top: 4px; line-height: 1.4; }
.opp-revenue { font-size: 11px; color: var(--success-emerald); font-weight: 600; margin-top: 6px; }

.hero-chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 12px 4px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.chat-welcome { text-align: center; margin: auto; max-width: 540px; padding: 20px; }
.welcome-icon-glow { width: 64px; height: 64px; margin: 0 auto 16px; background: var(--ai-gradient); border-radius: 50%; display: flex; align-items: center; justify-content: center; color: #ffffff; box-shadow: var(--shadow-ai-glow); }
.chat-welcome h3 { font-size: 18px; color: #ffffff; margin: 0 0 8px; font-family: var(--font-display); }
.chat-welcome p { color: var(--text-muted); font-size: 13.5px; margin: 0 0 20px; }
.starter-chips { display: flex; flex-wrap: wrap; gap: 8px; justify-content: center; }
.chip { background: var(--bg-card); border: 1px solid var(--border-medium); color: var(--text-main); padding: 8px 14px; border-radius: var(--radius-full); cursor: pointer; font-size: 12.5px; font-weight: 500; display: inline-flex; align-items: center; gap: 6px; transition: all 0.15s ease; }
.chip:hover { border-color: var(--ai-primary); background: var(--bg-card-hover); color: #ffffff; }

.chat-message { display: flex; }
.chat-message.owner-msg { justify-content: flex-end; }
.chat-message.ai-msg { justify-content: flex-start; }
.msg-bubble { max-width: 650px; padding: 12px 16px; border-radius: var(--radius-md); font-size: 13.5px; line-height: 1.5; }
.owner-bubble { background: var(--ai-gradient); color: #ffffff; border-radius: var(--radius-md) var(--radius-md) var(--radius-sm) var(--radius-md); }
.msg-ai-wrapper { display: flex; gap: 12px; max-width: 92%; }
.ai-avatar { width: 34px; height: 34px; background: var(--bg-canvas); border: 1px solid var(--ai-primary); border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 16px; flex-shrink: 0; box-shadow: 0 0 10px rgba(99,102,241,0.2); }
.msg-ai-content { flex: 1; display: flex; flex-direction: column; gap: 6px; }

.agent-chain { display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 4px; }
.agent-step { font-size: 11px; font-weight: 600; padding: 2px 8px; border-radius: var(--radius-full); background: var(--bg-canvas); border: 1px solid var(--border-medium); color: var(--text-muted); display: inline-flex; align-items: center; gap: 4px; }
.agent-step.success { border-color: rgba(16,185,129,0.3); color: var(--success-emerald); }

.hero-ai-bubble { background: var(--bg-card); border: 1px solid var(--border-medium); border-left: 3px solid var(--ai-primary); color: var(--text-main); }
.msg-time { font-size: 11px; color: var(--text-dim); margin-top: 6px; text-align: right; }

.hero-action-card {
  margin-top: 12px;
  background: var(--bg-canvas);
  border: 1px solid var(--ai-primary);
  border-radius: var(--radius-md);
  padding: 16px;
  box-shadow: 0 4px 16px rgba(99, 102, 241, 0.12);
}
.plan-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 12px; }
.plan-hero-badge { font-size: 10.5px; font-weight: 700; color: var(--ai-primary); text-transform: uppercase; letter-spacing: 0.05em; display: block; margin-bottom: 2px; }
.plan-title { font-size: 14px; font-weight: 700; color: #ffffff; }
.plan-risk-badge { font-size: 11px; font-weight: 600; padding: 3px 8px; border-radius: var(--radius-full); }
.risk-badge-low { background: var(--success-bg); color: var(--success-emerald); border: 1px solid rgba(16,185,129,0.3); }
.risk-badge-medium { background: var(--warning-bg); color: var(--warning-amber); border: 1px solid rgba(245,158,11,0.3); }
.risk-badge-high { background: var(--danger-bg); color: var(--danger-rose); border: 1px solid rgba(244,63,94,0.3); }

.plan-metrics { display: grid; grid-template-columns: repeat(4, 1fr); gap: 10px; margin-bottom: 12px; background: var(--bg-surface); padding: 10px; border-radius: var(--radius-sm); border: 1px solid var(--border-subtle); }
.plan-metric { display: flex; flex-direction: column; }
.pm-label { font-size: 11px; color: var(--text-dim); }
.pm-value { font-size: 13.5px; font-weight: 700; color: #ffffff; font-family: var(--font-mono); }
.pm-value.green { color: var(--success-emerald); }

.plan-details { margin-bottom: 8px; font-size: 12.5px; }
.plan-details summary { cursor: pointer; font-weight: 600; color: var(--text-muted); outline: none; }
.plan-detail-text { margin: 6px 0 0 12px; color: var(--text-main); font-size: 12.5px; line-height: 1.4; }

.plan-actions { display: flex; gap: 10px; margin-top: 14px; }
.plan-status-done { margin-top: 10px; color: var(--success-emerald); font-weight: 600; font-size: 12.5px; display: flex; align-items: center; gap: 6px; }
.plan-status-rejected { margin-top: 10px; color: var(--danger-rose); font-weight: 600; font-size: 12.5px; }

.hero-chat-input-area { margin-top: 12px; border-top: 1px solid var(--border-subtle); padding-top: 12px; }
.chat-input-row { display: flex; gap: 10px; }
.chat-textarea { flex: 1; resize: none; border-radius: var(--radius-sm); }
.chat-hint { font-size: 11px; color: var(--text-dim); margin-top: 6px; }

/* ── KPI & Insights ──────────────────────────────────────────────────────── */
.kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 14px; margin-bottom: 24px; }
.kpi-card { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 16px; }
.kpi-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
.kpi-label { font-size: 12px; color: var(--text-muted); font-weight: 500; }
.kpi-icon-svg { color: var(--text-dim); }
.kpi-icon-svg.warn-icon { color: var(--warning-amber); }
.kpi-icon-svg.ai-icon { color: var(--ai-primary); }
.kpi-value { font-size: 24px; font-weight: 700; color: #ffffff; font-family: var(--font-display); line-height: 1.2; }
.kpi-change { font-size: 11.5px; font-weight: 600; margin-top: 6px; }
.kpi-change.positive { color: var(--success-emerald); }
.kpi-change.negative { color: var(--danger-rose); }
.kpi-sub { font-size: 11.5px; color: var(--text-dim); margin-top: 4px; }

.segments-card { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 20px; margin-bottom: 24px; }
.card-title { font-size: 15px; font-weight: 600; color: #ffffff; margin: 0 0 16px; font-family: var(--font-display); }
.segment-bars { display: flex; flex-direction: column; gap: 12px; }
.segment-row { display: grid; grid-template-columns: 220px 1fr 40px; gap: 12px; align-items: center; font-size: 12.5px; }
.seg-label { color: var(--text-main); }
.seg-bar-bg { height: 8px; background: var(--bg-canvas); border-radius: 4px; overflow: hidden; }
.seg-bar { height: 100%; border-radius: 4px; }
.seg-active { background: var(--success-emerald); }
.seg-warn { background: var(--warning-amber); }
.seg-danger { background: var(--danger-rose); }
.seg-critical { background: #991b1b; }
.seg-count { font-weight: 700; color: #ffffff; font-family: var(--font-mono); text-align: right; }
.segments-cta { margin-top: 16px; }

.opp-section { margin-bottom: 24px; }
.opp-cards-vertical { display: flex; flex-direction: column; gap: 10px; }
.opp-card-v { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px 18px; display: flex; justify-content: space-between; align-items: center; }
.opp-v-left { display: flex; gap: 14px; align-items: flex-start; }
.opp-v-icon { font-size: 20px; }
.opp-v-title { font-size: 13.5px; font-weight: 600; color: #ffffff; }
.opp-v-desc { font-size: 12px; color: var(--text-muted); margin-top: 2px; }
.opp-v-revenue { font-size: 11.5px; color: var(--success-emerald); font-weight: 600; margin-top: 4px; }

.campaign-results { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 20px; }
.campaign-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; }
.cs-item { display: flex; flex-direction: column; }
.cs-v { font-size: 20px; font-weight: 700; color: #ffffff; font-family: var(--font-mono); }
.cs-l { font-size: 11.5px; color: var(--text-dim); margin-top: 2px; }

/* ── Operations & Queue ─────────────────────────────────────────────────── */
.ops-timeline { display: flex; flex-direction: column; gap: 16px; }
.ops-item { display: flex; gap: 16px; background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 16px; }
.ops-time-col { width: 80px; flex-shrink: 0; font-size: 11.5px; color: var(--text-dim); }
.ops-connector { width: 12px; display: flex; justify-content: center; }
.ops-dot { width: 10px; height: 10px; border-radius: 50%; background: var(--border-strong); margin-top: 4px; }
.ops-dot.dot-completed { background: var(--success-emerald); }
.ops-dot.dot-rejected { background: var(--danger-rose); }
.ops-dot.dot-proposed { background: var(--warning-amber); }
.ops-content { flex: 1; }
.ops-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px; }
.ops-agents { display: flex; gap: 4px; }
.ops-agent-badge { font-size: 11px; background: var(--bg-canvas); border: 1px solid var(--border-subtle); padding: 2px 6px; border-radius: 4px; color: var(--text-muted); }
.ops-intent { font-weight: 600; color: #ffffff; font-size: 13.5px; margin-bottom: 4px; }
.ops-reasoning { font-size: 12.5px; color: var(--text-muted); line-height: 1.4; }
.ops-metrics { display: flex; gap: 10px; margin-top: 8px; }
.ops-metric-chip { font-size: 11.5px; background: var(--bg-canvas); padding: 3px 8px; border-radius: 4px; color: var(--success-emerald); font-weight: 600; }
.ops-estimated { display: flex; gap: 8px; margin-top: 8px; }
.est-chip { font-size: 11.5px; background: var(--bg-canvas); padding: 3px 8px; border-radius: 4px; color: var(--info-cyan); }
.risk-chip { font-size: 11.5px; padding: 3px 8px; border-radius: 4px; font-weight: 600; }
.risk-low { color: var(--success-emerald); }
.risk-medium { color: var(--warning-amber); }
.risk-high { color: var(--danger-rose); }

/* Overview & Requests */
.metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 14px; }
.metric-card { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 16px; }
.m-icon-svg { color: var(--ai-primary); margin-bottom: 8px; display: block; }
.m-value { font-size: 24px; font-weight: 700; color: #ffffff; font-family: var(--font-display); }
.m-label { font-size: 12px; color: var(--text-muted); margin-top: 4px; }

.filter-tabs { display: flex; gap: 6px; }
.filter-tabs button { background: var(--bg-surface); border: 1px solid var(--border-subtle); color: var(--text-muted); padding: 6px 12px; border-radius: var(--radius-sm); font-size: 12px; font-weight: 600; cursor: pointer; }
.filter-tabs button.active { background: var(--ai-primary); color: #ffffff; border-color: var(--ai-primary); }

.request-cards { display: flex; flex-direction: column; gap: 12px; margin-top: 16px; }
.request-card { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 16px; }
.request-card.pending-border { border-left: 4px solid var(--warning-amber); }
.request-card.approved-border { border-left: 4px solid var(--success-emerald); }
.request-card.rejected-border { border-left: 4px solid var(--danger-rose); }
.request-card.conflict-border { border-left: 4px solid var(--danger-rose); }

.req-header { display: flex; justify-content: space-between; align-items: flex-start; }
.req-title { font-size: 14px; font-weight: 700; color: #ffffff; margin: 0 0 4px; }
.req-service { font-size: 12px; color: var(--info-cyan); font-weight: 600; }
.status-badge-pill { font-size: 11px; font-weight: 600; padding: 3px 8px; border-radius: var(--radius-full); }
.pill-pending { background: var(--warning-bg); color: var(--warning-amber); }
.pill-approved { background: var(--success-bg); color: var(--success-emerald); }
.pill-rejected { background: var(--danger-bg); color: var(--danger-rose); }
.pill-conflict { background: var(--danger-bg); color: var(--danger-rose); }

.req-body { margin: 12px 0; font-size: 13px; color: var(--text-muted); }
.req-body p { margin: 4px 0; }
.req-actions { display: flex; gap: 10px; margin-top: 12px; }

/* Forms & Cards */
.card { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 20px; }
.service-form { margin-bottom: 20px; }
.form-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 14px; }
.form-grid .full { grid-column: 1 / -1; }
.form-group { display: flex; flex-direction: column; gap: 6px; }
.form-group label { font-size: 12px; font-weight: 600; color: var(--text-muted); }
.form-actions { display: flex; gap: 10px; margin-top: 16px; }

.services-list { display: flex; flex-direction: column; gap: 10px; margin-top: 16px; }
.service-row { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 14px 18px; display: flex; justify-content: space-between; align-items: center; }
.service-info { display: flex; flex-direction: column; }
.svc-name { font-weight: 700; color: #ffffff; font-size: 14px; }
.svc-meta { font-size: 12px; color: var(--success-emerald); font-weight: 600; margin: 2px 0; }
.svc-desc { font-size: 12px; color: var(--text-muted); }

.availability-list { display: flex; flex-direction: column; gap: 8px; }
.avail-row { background: var(--bg-surface); border: 1px solid var(--border-subtle); border-radius: var(--radius-sm); padding: 12px 16px; display: flex; justify-content: space-between; align-items: center; font-size: 13px; }
.avail-day { font-weight: 600; color: #ffffff; width: 120px; }
.avail-time { color: var(--text-main); font-family: var(--font-mono); }
.avail-buffer { color: var(--text-dim); }
.avail-status { color: var(--text-dim); }
.avail-status.active-rule { color: var(--success-emerald); font-weight: 600; }

.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.7); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.modal-card { background: var(--bg-surface); border: 1px solid var(--border-medium); border-radius: var(--radius-lg); padding: 24px; width: 100%; max-width: 480px; }
.modal-card h3 { margin: 0 0 16px; font-size: 18px; color: #ffffff; }
.modal-field { margin-bottom: 12px; }
.modal-actions { margin-top: 20px; }
.empty-state { text-align: center; padding: 40px 20px; color: var(--text-muted); }
.empty-icon { font-size: 36px; margin-bottom: 10px; }

.typing-indicator { display: flex; gap: 4px; padding: 8px 12px; }
.typing-indicator span { width: 6px; height: 6px; background: var(--ai-primary); border-radius: 50%; animation: pulseGlow 0.8s infinite alternate; }
.typing-indicator span:nth-child(2) { animation-delay: 0.2s; }
.typing-indicator span:nth-child(3) { animation-delay: 0.4s; }
  `]
})
export class OwnerDashboardComponent implements OnInit, OnDestroy, AfterViewChecked {
  @ViewChild('chatContainer') chatContainer?: ElementRef;

  businessId = '11111111-1111-1111-1111-111111111111';
  allBusinesses: Business[] = [];
  business: Business | null = null;
  services: ServiceItem[] = [];
  availability: AvailabilityRule[] = [];
  allRequests: BookingRequest[] = [];
  metrics: DashboardMetrics | null = null;
  snapshot: BusinessSnapshot | null = null;
  enhancedMetrics: EnhancedMetrics | null = null;

  activeTab = 'ai-chat';

  // Owner Chat state
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
