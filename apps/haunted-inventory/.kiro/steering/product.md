# HauntedInventory - Product Guide

## Overview

HauntedInventory is a small business web app built on .NET 10: ASP.NET Core, Blazor (static SSR), EF Core with SQLite and Docker. It was generated from the Crypt `dotnet new` template; the `Items` feature is the reference implementation for new features.

## Theme

The UI has a Halloween theme that is:
- **Tasteful**: Spooky but professional
- **Dark UI**: Dark backgrounds with accent colors
- **Accessible**: WCAG AA contrast compliance

## Halloween Naming Convention

**UI Copy Only**: Spooky naming should only appear in user-facing copy (UI labels, messages, headers).

**Never in Code**: Code identifiers (classes, methods, variables, namespaces) must use clear, professional naming:
- ✅ `ItemsService` (not `GhostService`)
- ✅ `GetItemsAsync` (not `SummonItemsAsync`)
- ✅ `ApplicationDbContext` (not `CryptContext`)

## UI Styling

The app uses a dark theme (`src/HauntedInventory.Web/wwwroot/css/app.css`):
- Background: `#0f0f1a` (dark navy/black)
- Sidebar: `#1a1a2e`
- Accent: `#e94560` (red/pink)
- Text: `#eaeaea` (light gray)

Keep new colors at WCAG AA contrast (4.5:1 for body text).

## What's Included

- ASP.NET Core 10 with Blazor Web App (static SSR)
- EF Core 10 with SQLite (the only configured provider; see README for switching to PostgreSQL)
- Minimal APIs under `/api` (no authentication yet)
- xUnit integration tests against a temporary SQLite database file
- Docker multi-stage build running as the non-root `app` user
- docker-compose.yml for local development
- Kiro integration (.kiro folder with steering, hooks, agents)
