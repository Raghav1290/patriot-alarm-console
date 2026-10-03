import { useEffect, useState } from 'react'
import { api, JOB_FLOW, type Job, type JobStatus } from '../api'
import { ErrorBanner, formatTime } from './shared'

const columns: { status: JobStatus; title: string; next: string | null }[] = [
  { status: 'Dispatched', title: 'Dispatched', next: 'Mark en route' },
  { status: 'EnRoute', title: 'En route', next: 'Mark on site' },
  { status: 'OnSite', title: 'On site', next: 'Complete job' },
  { status: 'Completed', title: 'Completed', next: null },
]

interface Props {
  refreshKey: number
}

export function JobBoard({ refreshKey }: Props) {
  const [jobs, setJobs] = useState<Job[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.listJobs().then(setJobs).catch((e) => setError(e.message))
  }, [refreshKey])

  async function advance(job: Job) {
    const next = JOB_FLOW[JOB_FLOW.indexOf(job.status) + 1]
    setError(null)
    try {
      await api.advanceJob(job.id, next)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Something went wrong')
    }
  }

  return (
    <section>
      <ErrorBanner message={error} onDismiss={() => setError(null)} />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {columns.map((col) => {
          const items = jobs.filter((j) => j.status === col.status)
          return (
            <div key={col.status} className="flex min-h-64 flex-col rounded-lg bg-slate-100 p-3">
              <h2 className="mb-3 flex items-center justify-between text-sm font-semibold text-slate-700">
                {col.title}
                <span className="rounded-full bg-white px-2 py-0.5 text-xs text-slate-600 ring-1 ring-slate-200">{items.length}</span>
              </h2>

              <div className="flex flex-col gap-3">
                {items.map((job) => (
                  <article key={job.id} className="rounded-md bg-white p-3 shadow-sm ring-1 ring-slate-200">
                    <div className="flex items-start justify-between gap-2">
                      <div className="font-medium text-slate-900">{job.siteName ?? 'Unknown site'}</div>
                    </div>
                    <p className="mt-1 text-sm text-slate-600">{job.alarmDescription}</p>
                    <p className="mt-2 text-xs text-slate-500">Officer: {job.officerName}</p>
                    <dl className="mt-2 grid grid-cols-2 gap-x-2 text-xs text-slate-500">
                      <dt>Dispatched</dt>
                      <dd className="tabular-nums">{formatTime(job.dispatchedAtUtc)}</dd>
                      <dt>En route</dt>
                      <dd className="tabular-nums">{formatTime(job.enRouteAtUtc)}</dd>
                      <dt>On site</dt>
                      <dd className="tabular-nums">{formatTime(job.onSiteAtUtc)}</dd>
                    </dl>

                    {col.next && (
                      <button
                        onClick={() => advance(job)}
                        className="mt-3 w-full rounded-md bg-slate-900 px-3 py-1.5 text-xs font-semibold text-white hover:bg-slate-700"
                      >
                        {col.next}
                      </button>
                    )}
                  </article>
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}
