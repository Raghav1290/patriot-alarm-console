// Sends a Contact ID test message to the alarm receiver.
//
// Usage: node tools/send-alarm.mjs [account] [eventCode] [group] [zone] [qualifier]
// Example: node tools/send-alarm.mjs 1001 130 01 001      (burglary alarm, zone 1)
//
// Message layout (16 digits): AAAA 18 Q XYZ GG ZZZ C
// The checksum digit is a placeholder because the receiver does not validate it yet.

import net from 'node:net'

const [account = '1001', eventCode = '130', group = '01', zone = '001', qualifier = '1'] = process.argv.slice(2)
const host = process.env.RECEIVER_HOST ?? '127.0.0.1'
const port = Number(process.env.RECEIVER_PORT ?? 5050)

const message = `${account}18${qualifier}${eventCode}${group}${zone}0`

const socket = net.createConnection({ host, port }, () => {
  // \x14 frame terminator, matching what the receiver accepts
  socket.write(`${message}\x14`)
})

socket.on('data', (data) => {
  const reply = data[0]
  console.log(reply === 0x06 ? `ACK: stored ${message}` : `NAK: rejected ${message}`)
  socket.end()
})

socket.on('error', (err) => {
  console.error(`Could not reach the receiver at ${host}:${port}: ${err.message}`)
  process.exitCode = 1
})
