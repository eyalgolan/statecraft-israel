### Added

- A language offered to the player must name itself: `Translations.CheckDeclaredLanguage` refuses a
  `translations/<lang>/ui.yaml` with no `ui.lang`, and one whose `ui.lang` names a different language from its
  directory (#1606). The second is the case worth the check — a `ui.yaml` copied from another language with its code
  left behind passes every shape check, and `ui.lang` is what the player reads to pick the Hebrew face and the
  language branches in `EventCardPresenter` and `SharePresenter`, so the failure is silently the wrong language
  rather than an error. Documented in `docs/translations.md`; no player code changes yet.
