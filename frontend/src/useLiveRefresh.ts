import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect, useState } from 'react'

/**
 * Returns a counter that increases whenever the server reports an alarm or job change,
 * and once more each time the connection (re)opens, so screens reload anything missed.
 * Components include it in their effect deps to reload their data.
 */
export function useLiveRefresh(): number {
  const [version, setVersion] = useState(0)

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/alarms')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    const bump = () => setVersion((v) => v + 1)
    connection.on('EventChanged', bump)
    connection.on('JobChanged', bump)
    connection.onreconnected(bump)

    // Defer the start so React's development double-mount cancels it before it begins
    const timer = window.setTimeout(() => {
      connection
        .start()
        .then(bump)
        .catch((err) => console.error('Live updates unavailable', err))
    }, 0)

    return () => {
      window.clearTimeout(timer)
      connection.stop()
    }
  }, [])

  return version
}
