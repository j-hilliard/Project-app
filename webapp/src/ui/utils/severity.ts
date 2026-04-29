// Severity mapping utilities — import these in PM/Scheduling views.
// Never redefine these locally; violations are caught by ARCH-FE-005/006.

export function projectStatusSeverity(s: string): string {
  switch (s) {
    case 'Active': return 'success';
    case 'Monitoring': return 'info';
    case 'Planning':
    case 'Initiating': return 'secondary';
    case 'Closing':
    case 'OnHold': return 'warn';
    case 'Closed': return 'contrast';
    case 'Cancelled': return 'danger';
    default: return 'secondary';
  }
}

export function workOrderStatusSeverity(s: string): string {
  switch (s) {
    case 'Released': return 'success';
    case 'InProgress': return 'info';
    case 'Draft': return 'secondary';
    case 'Complete':
    case 'Closed': return 'contrast';
    case 'Cancelled': return 'danger';
    default: return 'secondary';
  }
}

export function planStatusSeverity(s: string): string {
  switch (s) {
    case 'Active': return 'success';
    case 'Complete': return 'info';
    case 'Archived': return 'secondary';
    default: return 'warn';
  }
}

export function workPackageStatusSeverity(s: string): string {
  switch (s) {
    case 'Active': return 'success';
    case 'InProgress': return 'info';
    case 'Complete': return 'contrast';
    case 'Draft': return 'secondary';
    default: return 'warn';
  }
}

export function fcoStatusSeverity(s: string): string {
  switch (s) {
    case 'Approved': return 'success';
    case 'Submitted': return 'warn';
    case 'Rejected': return 'danger';
    default: return 'secondary';
  }
}

export function assignmentStatusSeverity(s: string): string {
  switch (s) {
    case 'Confirmed': return 'success';
    case 'Cancelled': return 'danger';
    default: return 'info';
  }
}

export function phaseStatusSeverity(s: string): string {
  switch (s) {
    case 'Active': return 'success';
    case 'Complete': return 'contrast';
    case 'OnHold': return 'warn';
    case 'Cancelled': return 'danger';
    default: return 'secondary';
  }
}

export function taskStatusSeverity(s: string): string {
  switch (s) {
    case 'InProgress': return 'info';
    case 'Complete': return 'success';
    case 'Blocked': return 'danger';
    case 'OnHold': return 'warn';
    default: return 'secondary';
  }
}

export function stepStatusSeverity(s: string): string {
  switch (s) {
    case 'Complete': return 'success';
    case 'InProgress': return 'info';
    case 'Blocked': return 'danger';
    default: return 'secondary';
  }
}

export function milestoneStatusSeverity(s: string): string {
  switch (s) {
    case 'Achieved': return 'success';
    case 'Missed': return 'danger';
    case 'Cancelled': return 'secondary';
    default: return 'info';
  }
}

export function sourceTagSeverity(type: string): string {
  switch (type) {
    case 'Estimate': return 'success';
    case 'StaffingPlan': return 'warn';
    default: return 'info';
  }
}

export function demandStatusSeverity(s: string): string {
  const l = s?.toLowerCase();
  if (l === 'awarded') return 'success';
  if (l === 'pending' || l === 'approved') return 'warn';
  return 'secondary';
}

// Generic fallback — prefer domain-specific functions above.
export function statusSeverity(s: string): string {
  switch (s) {
    case 'Active':
    case 'Released':
    case 'Approved':
    case 'Achieved': return 'success';
    case 'InProgress':
    case 'Monitoring': return 'info';
    case 'Draft':
    case 'Planning':
    case 'Initiating':
    case 'Archived': return 'secondary';
    case 'Closing':
    case 'OnHold':
    case 'Submitted': return 'warn';
    case 'Complete':
    case 'Closed': return 'contrast';
    case 'Cancelled':
    case 'Rejected':
    case 'Missed':
    case 'Blocked': return 'danger';
    default: return 'secondary';
  }
}
