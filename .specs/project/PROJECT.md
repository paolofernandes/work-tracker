# WorkTracker

## Vision
A lightweight Windows tray app that prompts the user twice a day to log what they're working on, making it effortless to track hours and export them at the end of the week.

## Problem
Lack of discipline to manually open a time-tracking tool and log hours throughout the day. Work gets forgotten, logs are inaccurate, and the weekly upload to Artia becomes a guessing game.

## Goals
- Automatically prompt the user at configured times to capture work entries
- Make it frictionless: tray app, auto-start, minimal clicks
- Produce a clean weekly export (CSV) ready to upload to Artia

## Non-Goals (v1)
- Multi-user support
- Integration with Artia API
- Mobile companion
- Billing or invoicing

## Success Criteria
- User can see all entries for the day in under 5 seconds
- End-of-week CSV export is ready to upload to Artia without manual edits
- Daily prompts fire without user having to think about logging

## Tech Stack
- Platform: Windows 11
- Framework: .NET WinForms
- Database: SQLite (local)
- Distribution: single-user, local install
