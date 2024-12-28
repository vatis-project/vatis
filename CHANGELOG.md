# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## v4.1.0-beta.3
### Changed
- Improved styling for ATIS letter input mode.
- Disabled letter input mode (Shift + Double-Click) for observed ATIS.
- isNewAtis is now cleared upon network disconnection.
### Fixed
- Resolved an issue with frequency and QNH parsing when regional language settings use a comma as the decimal separator.
- Fixed missing "CAVOK" in text ATIS responses.
- Addressed an issue where duplicate ATIS station tabs could appear.
- Resolved an issue where * and {} were not being stripped from text ATIS responses.
- Fixed an overflow exception when saving ATIS frequency.

## v4.1.0-beta.2
### Added
- Add support for manually typing ATIS Letter: Shift + Double-Click the ATIS letter box to toggle typing mode. Press Enter to accept the letter.
### Changed
- Sort profiles alphabetically in the Profile window.
- Sort stations alphabetically by identifier in the compact window.
### Fixed
- Fixed broken dropdown menus.
- Resolved frequency parsing issue when regional language setting uses comma for decimal separator.
- Fixed message box centering issues on parent window.
- Display error message if there’s an issue loading an ATIS station.
- Fixed issue with static airport and NOTAM definitions not saving to profile
- Fixed topmost positioning for static airport and NOTAM definition dialogs.
- Fixed issue with transition level text and voice templates not saving to profile
- Fixed ICAO visibility voice and text templates.
- Corrected an issue with randomly disappearing METARs.
- Updated regex to improve runway ID matching with ^ parsing.
- Ensured observed ATIS stations are updated on the UI thread.
- Prevented changing ATIS letters for observed stations.

## v4.1.0-beta.1
- Initial release