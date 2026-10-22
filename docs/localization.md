# Localization

CamControl uses .NET ResourceManager with a complete English base resource and
German satellite resources. `NeutralLanguage` is `en`. There are no additional
localization packages or online translation services.

`LanguageSettings.Initialize()` runs before any application window is created.
It loads `language.json`, resolves Windows UI culture or the explicit preference,
then sets the current and default thread UI cultures. Missing, unreadable,
malformed and unsupported preferences use the Windows setting. Unsupported
Windows languages fall back to English. Regional formatting (`CurrentCulture`)
is preserved.

The settings dialog saves a pending preference and keeps the current UI language
until the next launch. It does not restart the application. Navigation uses enum
IDs independently of translated titles. User preset names, keyboard shortcuts,
camera protocol commands and existing camera settings formats are unchanged.

## Adding or editing translations

1. Add the same semantic key to `CamControl/Resources/Strings.resx` and each
   translated `Strings.<language>.resx`. Keep English complete. Add a corresponding
   property in `CamControl/Texts.cs` when introducing a key. C# uses `Texts.Key`;
   XAML uses `{x:Static local:Texts.Key}`.
2. Use complete sentences with composite format placeholders (`{0}`, `{1}`)
   through `Texts.Format`. Preserve placeholder indices and numeric formats.
   Include error messages, accessible names and help text. Do not translate
   user-provided names, control IDs or protocol values.
3. To offer another language, extend `AppLanguage`, `LanguageSettings.Resolve`,
   the settings choices and `packaging/AppxManifest.xml`. Provide a translated
   privacy document, embed it in both app and SettingsCheck projects and update
   `AppInformation.PrivacyText` selection. Add a Store listing draft as well.
4. Extend the resource completeness and UI culture checks to the new language.
   Build Debug and Release and run both test executables from the README.

English privacy content is in `docs/privacy.en.md`; German is in
`docs/privacy.md`. Both are embedded as ordinary resources and shown according
to the app UI language. Keep both versions in sync.

## Verification

SettingsCheck verifies persistence, invalid settings, Windows language variants,
explicit overrides, startup/thread culture initialization, preserved regional
formatting, exact resource key parity, usable format strings, regional/English
resource fallback and translated help/privacy.

CameraUiCheck runs the actual windows in German and English, checks language
selection/save/reopen/write failure and restart guidance, and retains the
camera lifecycle, command and layout checks. Rendered previews are written to
`artifacts/ui-preview/de-DE` and `artifacts/ui-preview/en-US`. These checks use
fake/demo transports and isolated preference files.

The MSIX manifest declares German and English. Store listing files are drafts;
they must be entered in Partner Center when preparing a release. The existing
release workflow updates German release notes only.
