# Issue tracker: GitHub

Issues und Spezifikationen liegen in GitHub Issues:
https://github.com/TobioBobi/CamControl

Verwende die GitHub CLI (`gh`) aus diesem Repository.

## Konventionen

- Vor dem ersten Erstellen eines Issues das Ziel mit
  `gh repo view --json nameWithOwner` prüfen.
- Issues ausschließlich in TobioBobi/CamControl erstellen.
- Erstellen: `gh issue create --title "..." --body-file <datei>`.
  Mehrzeilige Texte über eine UTF-8-Datei übergeben.
- Lesen: `gh issue view <nummer> --json number,title,body,labels,comments`.
- Auflisten: `gh issue list --state open --json number,title,labels`.
- Kommentare: `gh issue comment <nummer> --body-file <datei>`.
- Labels: `gh issue edit <nummer> --add-label "..."` beziehungsweise
  `--remove-label "..."`.
- Unteraufgaben über native GitHub Sub-Issues verknüpfen.
  Falls nicht verfügbar: `Part of #<nummer>` im Kind-Issue und
  eine Aufgabenliste im Eltern-Issue verwenden.
- Abhängigkeiten über native GitHub Issue Dependencies erfassen.
  Falls nicht verfügbar: `Blocked by: #<nummer>` im Issue verwenden.
  Ein Issue ist erst startklar, wenn alle Blocker geschlossen sind.
- Beim Schließen zuerst das Ergebnis kommentieren, dann
  `gh issue close <nummer>` ausführen.
- Nach dem Merge eines abschließenden PRs nach develop prüfen,
  ob das zugehörige Issue geschlossen ist, und es nötigenfalls schließen.

## Pull requests as a triage surface

**PRs as a request surface: no.**

## Anweisungen aus Skills

„Publish to the issue tracker“ bedeutet: ein GitHub Issue erstellen.
„Fetch the relevant ticket“ bedeutet: das zugehörige GitHub Issue
einschließlich Kommentaren, Labels und Abhängigkeiten lesen.

Die Regeln aus AGENTS.md gelten weiterhin, insbesondere die
ausdrückliche Nutzerfreigabe vor dem Erstellen neuer Pull Requests.
