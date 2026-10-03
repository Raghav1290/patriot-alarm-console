import { useEffect, useState } from 'react'
import { api, type AuditEntry } from '../api'
import { formatTime } from './shared'

interface Props {
  refreshKey: number
}

export function AuditLog({ refreshKey }: Props) {
  const [entries, setEntries] = useState<AuditEntry[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.listAudit().then(setEntries).catch((e) => setError(e.message))
  }, [refreshKey])

  if (error) {
    return <p className="text-sm text-red-700">{error}</p>
  }

  return (
    <div className="overflow-x-auto rounded-lg bg-white shadow-sm ring-1 ring-slate-200">
      <table className="min-w-full divide-y divide-slate-200 text-sm">
        <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500">
          <tr>
            <th className="px-4 py-3">Time</th>
            <th className="px-4 py-3">Actor</th>
            <th className="px-4 py-3">Action</th>
            <th className="px-4 py-3">Details</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {entries.map((e) => (
            <tr key={e.id}>
              <td className="whitespace-nowrap px-4 py-2 tabular-nums text-slate-600">{formatTime(e.atUtc)}</td>
              <td className="px-4 py-2 text-slate-800">{e.actor}</td>
              <td className="px-4 py-2 font-mono text-xs text-slate-600">{e.action}</td>
              <td className="px-4 py-2 text-slate-700">{e.details}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
