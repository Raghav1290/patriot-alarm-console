# Patriot Alarm Console

A demo alarm receiving and dispatch system. Alarm panels send Contact ID messages over TCP. The receiver stores each alarm, operators acknowledge and dispatch it to a patrol officer, and every change appears live on every open console.

Built with ASP.NET Core 8, Entity Framework Core, SignalR, React, TypeScript and Tailwind CSS.

## What it does

- **Alarm receiver**: a TCP listener parses Contact ID messages (the 16-digit format used by alarm panels), stores them, and replies with ACK or NAK.
- **Alarm feed**: operators filter alarms by status, acknowledge them, dispatch an officer, and clear them once the job is complete.
- **Dispatch board**: jobs move through Dispatched → En route → On site → Completed, with timestamps at each step.
- **Audit log**: every receiver event, acknowledgement, dispatch, status change and clear is recorded.
- **Live updates**: SignalR pushes changes to all connected consoles, so no page refresh is needed.

## Project layout

```
backend/
  PatriotAlarm.Api/        ASP.NET Core Web API
    Receiver/              TCP listener and Contact ID parser
    Services/              Alarm and dispatch rules
    Controllers/           REST endpoints
    Hubs/                  SignalR hub
    Data/                  EF Core DbContext
    Domain/                Entities and enums
  PatriotAlarm.Tests/      xUnit tests
frontend/                  React + Vite + Tailwind operator console
tools/send-alarm.mjs       Sends a test alarm to the receiver
```

## Running it locally

Requirements: .NET 8 SDK and Node.js 20+.

```bash
# 1. Start the API (HTTP on :5080, alarm receiver on TCP :5050)
cd backend/PatriotAlarm.Api
dotnet run

# 2. Start the console (in a second terminal)
cd frontend
npm install
npm run dev        # open the URL Vite prints, usually http://localhost:5173

# 3. Send test alarms (in a third terminal)
node tools/send-alarm.mjs 1001 130 01 001   # burglary, Demo Medical Centre, zone 1
node tools/send-alarm.mjs 1002 110 00 003   # fire, Demo Warehouse
```

The database (`patriot-alarm.db`) is created on first run with three demo sites: accounts 1001, 1002 and 1003. The data is fictional.

Run the tests:

```bash
cd backend
dotnet test
```

## Design notes

- **Rules in one place.** `AlarmService` holds the business rules, for example: a job can only move forward one step, and an alarm can't be cleared while its job is open. Controllers only translate HTTP to service calls.
- **Errors map to status codes.** Missing records return 404. Rule violations return 409 with a readable message.
- **Audit on every change.** Each state change writes an audit entry in the same database transaction as the change itself.
- **Receiver isolation.** Each received message gets its own DI scope, so one bad message can't leave shared state behind.
- **SQLite for now.** The app uses EF Core, so switching to SQL Server means changing the provider and connection string.

## Current limits

- The Contact ID checksum digit is read but not validated yet.
- The database is created with `EnsureCreated`. EF migrations should replace this before real use.
- There is no authentication. Operator and officer names are fixed placeholders.
- Only Contact ID over raw TCP is supported. Other protocols such as SIA would need their own parsers.
