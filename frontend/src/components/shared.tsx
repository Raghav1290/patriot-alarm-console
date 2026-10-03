import type { AlarmStatus, JobStatus, Priority } from '../api'

const priorityStyles: Record<Priority, string> = {
  Critical: 'bg-red-600 text-white',
  High: 'bg-orange-500 text-white',
  Medium: 'bg-amber-300 text-amber-950',
  Low: 'bg-sky-200 text-sky-900',
  Info: 'bg-slate-200 text-slate-700',
}

const statusStyles: Record<AlarmStatus, string> = {
  New: 'bg-red-100 text-red-800 ring-red-300',
  Acknowledged: 'bg-amber-100 text-amber-900 ring-amber-300',
  Dispatched: 'bg-blue-100 text-blue-900 ring-blue-300',
  Cleared: 'bg-emerald-100 text-emerald-900 ring-emerald-300',
}

export function PriorityBadge({ priority }: { priority: Priority }) {
  return (
    <span className={`inline-flex rounded px-2 py-0.5 text-xs font-bold uppercase tracking-wide ${priorityStyles[priority]}`}>
      {priority}
    </span>
  )
}

export function StatusBadge({ status }: { status: AlarmStatus }) {
  return (
    <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${statusStyles[status]}`}>
      {status}
    </span>
  )
}

export function JobStatusLabel({ status }: { status: JobStatus }) {
  const labels: Record<JobStatus, string> = {
    Dispatched: 'Dispatched',
    EnRoute: 'En route',
    OnSite: 'On site',
    Completed: 'Completed',
  }
  return <span className="text-sm text-slate-600">{labels[status]}</span>
}

export function formatTime(utc: string | null): string {
  if (!utc) return '-'
  return new Date(utc).toLocaleTimeString('en-NZ', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

export function ErrorBanner({ message, onDismiss }: { message: string | null; onDismiss: () => void }) {
  if (!message) return null
  return (
    <div role="alert" className="mb-4 flex items-start justify-between rounded-md border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-800">
      <span>{message}</span>
      <button onClick={onDismiss} className="ml-4 font-semibold hover:underline">
        Dismiss
      </button>
    </div>
  )
}
