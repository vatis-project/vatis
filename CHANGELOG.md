# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## v4.1.0-beta.6
### Added
- Added a profile file update serial number to the ATIS configuration window.
- Added the ability to customize voice and text templates for the NOTAM section.
- Added support for fetching real-world digital ATIS (D-AITS) letters using the CTRL + D keyboard shortcut.
- Added symbols for temperature and dewpoint range (e.g., +/−) in the ATIS text template.
- Added formatting options for temperature and dewpoint template variables.
- Added ceiling and prevailing visibility data to websocket clients.
- Added support for IDS validation tokens.
### Changed
- Changed ATIS hub security to require an authentication token for connection.
- Changed the "keep on top" setting to be saved separately for the main and compact windows.
- Changed the display of selected Airport Conditions and NOTAMs in the main user interface for better clarity.
- Changed how text and voice ATIS messages are generated to improve response times and reduce websocket message delays.
### Fixed
- Fixed issues with the observation time in text and voice template variables.
- Fixed missing ATIS letter in the FSD ATIS query response.
- Fixed UI spacing around the altimeter formatting variable buttons for better alignment.
- Fixed the layout of wind data in the compact window to properly handle long wind strings.
- Fixed a potential issue with AFV token expiration by adding a semaphore to prevent concurrent token refresh calls.

## v4.1.0-beta.5
### Added
- Added web interface to development server to allow manipulating METARs for testing.
- Created a local development launch profile tailored for Visual Studio.
### Changed
- Removed the plus sign (+) correctly from navaid and airport text parsing.
- Enhanced the shutdown process to prevent leftover processes.
- Refined default settings for VSCode configurations.
- Enabled automatic profile updates during profile import.
### Fixed
- Resolved an issue where ATIS was not consistently published to ATIS Hub.
- Addressed authentication timeouts on Linux caused by malformed machine identifier string.
- Adjusted spacing between dots and NOTAM text for improved clarity.
- Fixed the NOTAM prefix displaying incorrectly when no NOTAMs were selected.
- Corrected mishandling of undetermined cloud layer variables.
- Ensured the ATIS Hub connection respects development server settings.
- Fixed an InvalidOperationException occurring when adding new ATIS stations.

## v4.1.0-beta.4
### Added
- Added WebSocket API.
- Added local development support for easier testing and debugging.
- Added template customization options for the recent weather group.
- Added "Open" button to the Profile dialog window.
- Added formatting parameter to the cloud altitude template variable.
- Added mouse drag support for various windows and dialogs.
### Changed
- Updated cloud and weather template text to display only if the respective weather data is available.
- Improved spacing after the NOTAMs section in the text ATIS.
- Default closing statement template now includes a closing period.
- Improved ATIS station tab selection state during ATIS connect/disconnect actions.
- Adjusted startup logic to prevent the app from launching from a macOS DMG volume.
- Profile dialog and Voice Record ATIS window now remember their last position.
### Fixed
- Resolved a FormatException issue when parsing negative number values.
- Corrected magnetic variation logic to respect the enabled setting.
- Fixed ATIS connection count not resetting properly after session start/end.
- Addressed initialization issues with audio device names in the native audio library.
- Improved ATIS letter edit mode to handle the escape key and quick mouse clicks more reliably.
- Fixed the ATIS letter selector to respect the correct code range.
- Fixed RVR decoding to show all RVRs in the METAR, including unlimited RVR values.
- Fixed overflow exception when saving malformed ATIS frequency.
- Fixed issue with ATIS sandbox not clearing values when switching stations.

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