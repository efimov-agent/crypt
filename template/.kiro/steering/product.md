# Crypt Template - Product Guide

## Overview

Crypt is a Kiro-ready .NET 10 skeleton template for small business web apps. It generates a complete project structure with ASP.NET Core, Blazor, EF Core, and Docker support.

## Theme

The template has a Halloween theme that is:
- **Tasteful**: Spooky but professional
- **Dark UI**: Dark backgrounds with accent colors
- **Accessible**: WCAG AA contrast compliance

## Halloween Naming Convention

**UI Copy Only**: Spooky naming should only appear in user-facing copy (UI labels, messages, headers). 

**Never in Code**: Code identifiers (classes, methods, variables, namespaces) must use clear, professional naming:
- ✅ `ItemsService` (not `GhostService`)
- ✅ `GetItemsAsync` (not `SummonItemsAsync`)
- ✅ `ApplicationDbContext` (not `CryptContext`)

## Example App Names

The template can generate two distinct apps:
- **haunted-inventory**: Stock tracking for costume/prop rental shop (items, rentals, overdue "haunting" alerts)
- **seance-scheduler**: Booking of time slots for sessions with hosts (hosts, slots, bookings)

## Template Parameters

When generating a project:
- `--name` or `-n`: Project name (used for namespaces and folder names)
- `--title`: Display title for the application
- `--description`: Project description
- `--author`: Author name

Example:
```bash
dotnet new crypt -n HauntedInventory --title "Haunted Inventory" --desc "Stock tracking for rental shop"
```

## UI Styling

The generated app includes a dark theme with:
- Background: `#0f0f1a` (dark navy/black)
- Sidebar: `#1a1a2e`
- Accent: `#e94560` (red/pink)
- Text: `#eaeaea` (light gray)

These colors meet WCAG AA contrast requirements.

## What's Included

- ASP.NET Core 10 with Blazor Web App (static SSR)
- EF Core 10 with SQLite (default) and PostgreSQL (optional)
- Minimal APIs under `/api`
- xUnit tests with in-memory database
- Docker multi-stage build with non-root user
- docker-compose.yml for local development
- Kiro integration (.kiro folder with steering, hooks, agents)