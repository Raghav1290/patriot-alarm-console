import { useEffect, useState } from 'react'
import { api, OFFICERS, type AlarmEvent, type AlarmStatus } from '../api'
import { ErrorBanner, formatTime, PriorityBadge, StatusBadge } from './shared'

const filters: { label: string; value: AlarmStatus | undefined }[] = [
  { label: 'All', value: undefined },
  { label: 'New', value: 'New' },
  { label: 'Acknowledged', value: 'Acknowledged' },
  { label: 'Dispatched', value: 'Dispatched' },
  { label: 'Cleared', value: 'Cleared' },
]

interface Props {
  refreshKey: number
}

export function AlarmFeed({ refreshKey }: Props) {
  const [alarms, setAlarms] = useState<AlarmEvent[]>([])
  const [filter, setFilter] = useState<AlarmStatus | undefined>(undefined)
  const [officer, setOfficer] = useState<Record<number, string>>({})
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.listEvents(filter).then(setAlarms).catch((e) => setError(e.message))
  }, [filter, refreshKey])

  // Run an action, show any server error, then reload via the live refresh key
  async function run(action: () => Promise<void>) {
    setError(null)
    try {
      await action()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Something went wrong')
    }
  }

  return (
    <section>
      <div className="mb-4 flex flex-wrap gap-2">
        {filters.map((f) => (
          <button
            key={f.label}
            onClick={() => setFilter(f.value)}
            className={`rounded-full px-3 py-1 text-sm font-medium transition ${
              filter === f.value ? 'bg-slate-900 text-white' : 'bg-white text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50'
            }`}
          >
            {f.label}
          </button>
        ))}
      </div>

      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      {alarms.length === 0 ? (
        <p className="rounded-lg border border-dashed border-slate-300 p-8 text-center text-slate-500">
          No alarms to show. Send a test message with <code>node tools/send-alarm.mjs</code>.
        </p>
      ) : (
        <div className="overflow-x-auto rounded-lg bg-white shadow-sm ring-1 ring-slate-200">
          <table className="min-w-full divide-y divide-slate-200 text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
              <tr>
                <th className="px-4 py-3">Received</th>
                <th className="px-4 py-3">Priority</th>
                <th className="px-4 py-3">Site</th>
                <th className="px-4 py-3">Event</th>
                <th className="px-4 py-3">Zone</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {alarms.map((a) => (
                <tr key={a.id} className={a.status === 'New' ? 'bg-red-50/60' : ''}>
                  <td className="whitespace-nowrap px-4 py-3 tabular-nums text-slate-600">{formatTime(a.receivedAtUtc)}</td>
                  <td className="px-4 py-3">
                    <PriorityBadge priority={a.priority} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium text-slate-900">{a.siteName ?? 'Unknown site'}</div>
                    <div className="text-xs text-slate-500">Account {a.accountNumber}</div>
                  </td>
                  <td className="px-4 py-3 text-slate-800">
                    {a.description} <span className="text-xs text-slate-400">({a.eventCode})</span>
                  </td>
                  <td className="px-4 py-3 tabular-nums text-slate-600">{a.zone}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={a.status} />
                    {a.officerName && <div className="mt-1 text-xs text-slate-500">{a.officerName}</div>}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap items-center justify-end gap-2">
                      {a.status === 'New' && (
                        <button
                          onClick={() => run(() => api.acknowledge(a.id))}
                          className="rounded-md bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white hover:bg-slate-700"
                        >
                          Acknowledge
                        </button>
                      )}
                      {(a.status === 'New' || a.status === 'Acknowledged') && (
                        <>
                          <select
                            aria-label="Officer"
                            value={officer[a.id] ?? OFFICERS[0]}
                            onChange={(e) => setOfficer({ ...officer, [a.id]: e.target.value })}
                            className="rounded-md border border-slate-300 px-2 py-1.5 text-xs"
                          >
                            {OFFICERS.map((o) => (
                              <option key={o}>{o}</option>
                            ))}
                          </select>
                          <button
                            onClick={() => run(() => api.dispatch(a.id, officer[a.id] ?? OFFICERS[0]))}
                            className="rounded-md bg-blue-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-blue-500"
                          >
                            Dispatch
                          </button>
                        </>
                      )}
                      {a.status !== 'Cleared' && (a.jobStatus === null || a.jobStatus === 'Completed') && (
                        <button
                          onClick={() => run(() => api.clear(a.id))}
                          className="rounded-md px-3 py-1.5 text-xs font-semibold text-slate-700 ring-1 ring-slate-300 hover:bg-slate-50"
                        >
                          Clear
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
