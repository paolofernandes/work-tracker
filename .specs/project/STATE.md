# Project State

## Status
Phase: **Tasks complete** — ready for Execute

## Decisions
- Tech stack: .NET WinForms, SQLite — switched from MAUI; WinForms is native fit for tray apps
- Window title scanning: scan OS window titles (no Chrome extension needed for v1)
- Prompt delay past window: still fires after delay, does not skip
- Multiple entries per day: supported via multiple Start/Finish cycles
- CSV export format: `Data, Hora de início, Esforço, Observação, ID da Atividade, E-mail` (Portuguese headers, Artia format)
- ID da Atividade is user-configurable monthly in Settings
- Prompts fire on workdays only (Mon–Fri), never weekends

## Blockers
- None currently

## Deferred Ideas (post-v1)
- Artia API direct upload
- Chrome extension for more reliable tab detection
- Mobile companion app
- Dark/light theme

## Lessons
- (empty)

## Todos
- [x] User to provide Artia CSV sample — done, REQ-009 updated
- [ ] Confirm app name before Design phase
