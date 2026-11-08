# Domain docs

## Vor der Codebase-Erkundung

- `GLOSSARY.md` im Repository-Stamm lesen, sofern vorhanden.
- Relevante Architekturentscheidungen unter `docs/adr/` lesen.

Fehlen diese Dateien, ohne Hinweis fortfahren. Keine leeren
Glossare oder ADRs vorsorglich anlegen. Der Skill domain-modeling
legt sie an, sobald Begriffe oder Entscheidungen geklärt werden.

## Layout

Dieses Repository verwendet single-context:

- `GLOSSARY.md`: gemeinsame Fachbegriffe
- `docs/adr/`: Architekturentscheidungen

## Begriffe verwenden

In Issues, Vorschlägen, Hypothesen und Tests die Begriffe aus
GLOSSARY.md verwenden. Fehlende Fachbegriffe bei Bedarf für
domain-modeling festhalten.

## Konflikte mit Entscheidungen

Widerspricht ein Vorschlag einem bestehenden ADR, den Konflikt
ausdrücklich nennen und begründen, warum die Entscheidung erneut
betrachtet werden sollte.
