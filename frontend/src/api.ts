export type Priority = 'Info' | 'Low' | 'Medium' | 'High' | 'Critical'
export type AlarmStatus = 'New' | 'Acknowledged' | 'Dispatched' | 'Cleared'
export type JobStatus = 'Dispatched' | 'EnRoute' | 'OnSite' | 'Completed'

export interface AlarmEvent {
  id: number
  accountNumber: string
  siteName: string | null
  receivedAtUtc: string
  eventCode: string
  description: string
  zone: string
  group: string
  priority: Priority
  status: AlarmStatus
  jobId: number | null
  officerName: string | null
  jobStatus: JobStatus | null
}

export interface Job {
  id: number
  alarmEventId: number
  alarmDescription: string
  siteName: string | null
  officerName: string
  status: JobStatus
  dispatchedAtUtc: string
  enRouteAtUtc: string | null
  onSiteAtUtc: string | null
  completedAtUtc: string | null
  notes: string | null
}

export interface AuditEntry {
  id: number
  atUtc: string
  actor: string
  action: string
  details: string
}

// Operator and officer names are placeholders for the demo
export const OPERATOR = 'Operator 1'
export const OFFICERS = ['Officer Chen', 'Officer Patel', 'Officer Smith']

export const JOB_FLOW: JobStatus[] = ['Dispatched', 'EnRoute', 'OnSite', 'Completed']

async function request<T>(method: 'GET' | 'POST', url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new Error(data?.error ?? `Request failed (${response.status})`)
  }

  return response.status === 204 ? (undefined as T) : response.json()
}

export const api = {
  listEvents: (status?: AlarmStatus) =>
    request<AlarmEvent[]>('GET', status ? `/api/events?status=${status}` : '/api/events'),

  acknowledge: (id: number) =>
    request<void>('POST', `/api/events/${id}/acknowledge`, { actor: OPERATOR }),

  dispatch: (id: number, officerName: string) =>
    request<void>('POST', `/api/events/${id}/dispatch`, { actor: OPERATOR, officerName }),

  clear: (id: number) => request<void>('POST', `/api/events/${id}/clear`, { actor: OPERATOR }),

  listJobs: () => request<Job[]>('GET', '/api/jobs'),

  advanceJob: (id: number, status: JobStatus) =>
    request<void>('POST', `/api/jobs/${id}/status`, { actor: OPERATOR, status }),

  listAudit: () => request<AuditEntry[]>('GET', '/api/audit'),
}
