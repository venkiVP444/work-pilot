import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class BusinessContextService {
  private readonly STORAGE_KEY = 'workpilot_business_id';
  private readonly DEFAULT_BUSINESS_ID = '11111111-1111-1111-1111-111111111111';

  getBusinessId(): string {
    const stored = localStorage.getItem(this.STORAGE_KEY);
    if (stored && stored.trim() && stored !== 'null' && stored !== 'undefined') {
      return stored.trim();
    }
    return this.DEFAULT_BUSINESS_ID;
  }

  setBusinessId(id: string): void {
    if (id && id.trim()) {
      localStorage.setItem(this.STORAGE_KEY, id.trim());
    } else {
      this.clearBusinessId();
    }
  }

  clearBusinessId(): void {
    localStorage.removeItem(this.STORAGE_KEY);
  }
}
