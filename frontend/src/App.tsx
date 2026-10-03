import { useState } from 'react'
import { AlarmFeed } from './components/AlarmFeed'
import { AuditLog } from './components/AuditLog'
import { JobBoard } from './components/JobBoard'
import { useLiveRefresh } from './useLiveRefresh'

type Tab = 'alarms' | 'dispatch' | 'audit'

const tabs: { id: Tab; label: string }[] = [
  { id: 'alarms', label: 'Alarm feed' },
  { id: 'dispatch', label: 'Dispatch board' },
  { id: 'audit', label: 'Audit log' },
]

function App() {
  const [tab, setTab] = useState<Tab>('alarms')
  const refreshKey = useLiveRefresh()

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-4 py-4 sm:px-6">
          <div>
            <h1 className="text-lg font-bold">Patriot Alarm Console</h1>
            <p className="text-xs text-slate-500">Demo operator screen · live updates over SignalR</p>
          </div>
          <nav className="flex gap-1 rounded-lg bg-slate-100 p-1">
            {tabs.map((t) => (
              <button
                key={t.id}
                onClick={() => setTab(t.id)}
                className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
                  tab === t.id ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                {t.label}
              </button>
            ))}
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-4 py-6 sm:px-6">
        {tab === 'alarms' && <AlarmFeed refreshKey={refreshKey} />}
        {tab === 'dispatch' && <JobBoard refreshKey={refreshKey} />}
        {tab === 'audit' && <AuditLog refreshKey={refreshKey} />}
      </main>
    </div>
  )
}

export default App
