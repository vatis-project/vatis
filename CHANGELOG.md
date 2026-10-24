# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
### Changed
- Changed D-ATIS replacements and prepend/append text to be held until Apply or OK is pressed, instead of being saved immediately.
### Fixed
- Fixed D-ATIS text replacements leaving stray punctuation behind and not removing trailing punctuation for plain-text patterns.
### Added
- Added customizable text and voice templates for the trend forecast, allowing the spoken "TREND" prefix to be changed or removed.

## v4.1.0-beta.20
### Added
- Added a JSON-only `disconnectIfUnrecorded` option to automatically disconnect a recorded voice ATIS if it hasn't been recorded within 5 minutes of connecting.
- Added support for double-clicking the main window to open the mini-window.
- Added a `--import-profile` command-line argument to import a profile from a file path.
- Added a `syncAtisLetter` property to the `configureAtis` WebSocket message.
- Added support for the `setAtisLetter` WebSocket message and broadcasting connection errors to all WebSocket clients.
- Added options to speak wind speed in group format and use digit formatting for wind speed in text templates.
- Added a negative prefix option for temperature and dewpoint text templates.
- Added per-variable text and voice formatting options for altimeter, temperature, and dewpoint.
- Added text ATIS variables for visibility direction, RVR tendency, and convective clouds.
- Added customizable visibility unit words, a fraction voice table, and cloud altitude formatting.
- Added customizable spoken phrases and text for runway visual range.
- Added custom text and voice formatting for CAVOK.
- Added custom separators for airport conditions and NOTAMs.
- Added an "Include" option for static airport conditions and NOTAMs, allowing entries to appear only when specific ATC stations are online or offline (e.g., `LAX_TWR`, `EPWW_*_CTR`).
- Added conditional weather rules for static airport conditions and NOTAMs, allowing entries to appear only when METAR conditions match a specified expression (e.g., `ceiling < 200ft AND (rvr < 550m OR visibility < 100m)`).
- Added runway condition reporting from METAR runway state groups.
- Added remark wind information to the ATIS.
- Added an `[AUTO]` variable for automatic observations, a `[METAR]` variable for raw METAR data, a per-preset threshold QFE variable, and a `{feet}` cloud token.
- Added digit-count formatting for the transition level variable.
- Added an Undetermined Cloud type for cloud layers with unknown heights.
- Added an option to randomize the ATIS letter on connect.
- Added an option to apply a contraction to all ATIS stations.
- Added a profile option to alternate between voices.
- Added support for WAV voice packs in stitched voice ATIS.
- Added automatic loading of native D-ATIS. Thanks to Leftos and Josh G. for their work on this.
- Added a voice ATIS recording indicator and magnetic wind toggle to the mini-window.
- Added a keyboard shortcut to open the application data folder.
- Added underlining for contractions and speech tokens in the ATIS editors.
- Added the airport name to the Record ATIS script.
- Added the ability to override the METAR of a connected ATIS by right-clicking the METAR box.
- Added regex pattern replacements with functions (`$group`, `$digits`, `$spell`, `$phonetic`) for modifying written and spoken ATIS.
- Added a text/voice script toggle to the Record ATIS dialog.
- Added support for visibility in kilometers in the METAR decoder.

### Changed
- Improved application startup and profile loading performance.
- Updated the application icon.
- Standardized scrollbar styling throughout the application.
- Updated button icons to use SVG assets.
- Improved taxiway and runway identifier parsing for spoken ATIS.
- Included the full text ATIS in the IDS JSON and added an empty update on disconnect.
- Updated network traffic encoding to Latin-1.
- Preserved user-edited profile names during automatic profile updates.
- Improved error reporting when navdata downloads fail.
- Redesigned the release notes window to organize changes into titled sections.
- Updated Windows machine ID handling to remove whitespace and null terminators.

### Fixed
- Fixed an error dialog appearing on Linux when a desktop service, such as the global menu or input method, is unavailable.
- Fixed tooltips closing unexpectedly in dialog windows on macOS.
- Fixed dialogs drifting off-center from their parent windows when resized.
- Fixed AtisHub connecting without a VATSIM token and subscription errors going unnoticed.
- Fixed integration with vatSys.
- Fixed forced disconnect logic.
- Fixed the ATIS being republished and re-subscribed after an automatic AtisHub reconnect.
- Fixed TREND information being omitted from the ATIS when the METAR contains an RMK section, and incorrect TREND parsing with multiple present weather groups.
- Fixed METAR decoding when an unavailable group follows visibility or when an RVR value is unavailable.
- Fixed directional minimum visibility in text and voice ATIS.
- Fixed a race condition in `getStations` after re-entering a profile.
- Fixed repeated microphone permission prompts on macOS.
- Fixed message box and modal dialog centering and height issues.
- Fixed popping noises at the start and end of sandbox audio playback.
- Fixed the sandbox ATIS refresh not selecting a random letter from the configured code range.
- Fixed ATIS letter input and unintended letter shifting while typing.
- Fixed window position restoration when opening the application.
- Fixed local development configurations for VS Code and Rider.

## v4.1.0-beta.19
### Fixed
- Fixed an issue that caused the "Sort" stations window to crash the application.

## v4.1.0-beta.18
### Added
- Added support for double-tapping the mini-window to quickly open the main UI.
- Added a right-click menu option to save updates made to the airport conditions and NOTAMs text to the ATIS profile.
### Changed
- Updated mini-window controls to be accessible via a right-click context menu instead of requiring the CTRL key.
- Changed the behavior of the airport conditions and NOTAMs text boxes to automatically sync with the connected ATIS.
### Fixed
- Fixed an issue where the voice server heartbeat timer continued running after ATIS was disconnected.
- Temporarily resolved DPI scaling issues on Windows by downgrading Avalonia.
- Fixed a bug where the Record ATIS button remained in a blinking state.
- Fixed a "value cannot be null" exception triggered when connecting or disconnecting ATIS.
- Fixed an issue in ATIS station sorting logic that caused ordinals to be calculated incorrectly.

## v4.1.0-beta.17
### Added
- Added airport conditions and NOTAM text to the WebSocket ATIS response message.
- Added the ability to toggle wind and altimeter display in the mini-window.
- Added `getActiveProfile` and `getContractions` WebSocket message support.
- Added voice server heartbeat functionality.
- Added independent save/apply support for airport conditions and NOTAMs.
- Added a variable to display ATIS type (SPECI or METAR) in the text ATIS.
- Added customization options for RVR tendency values in text ATIS.
- Added tooltip to the ATIS letter in the mini-window that displays the text ATIS.
- Added support for customizing TREND and WS formatting in text ATIS.
- Added notification sound for shared ATIS connection updates and a user setting to mute these updates.
- Added support for prefixing ceiling layers in the text ATIS.
- Added configuration to define which cloud layer types can be ceiling layers.
- Added the ability to show/hide individual ATIS stations in the mini-window.
### Changed
- Updated exit dialog in the mini-window to be centered on screen.
- Enhanced tooltip descriptions for external ATIS generator URL fields.
- Improved change tracking logic for ATIS configuration updates.
- Modified in-app notification display behavior for better user visibility.
- Updated mini-window controls to appear only when `CTRL` is pressed.
- Disabled mouse wheel input on the main UI preset dropdown to prevent accidental changes.
- Enhanced "Record ATIS" button to highlight blue during broadcast and blink when a new recording is needed.
### Fixed
- Fixed an issue where special parsing characters were not removed from the text ATIS.
- Fixed a crash caused by an invalid application configuration.
- Fixed ATIS disconnection behavior during local configuration changes.
- Fixed a missing closing statement variable when no text was defined.

## v4.1.0-beta.16
### Added
- Added additional WebSocket messages: request profiles, request stations, configure ATIS, connect/disconnect ATIS, and exit application.
- Added support for defining separate text and voice ATIS generator URLs.
- Added template variable for METAR observation day of the month.
- Added ATIS station `id` to `atis` WebSocket message.
### Changed
- Improved text ATIS formatting to support user-defined line breaks.
- Enhanced FSD parse error handling by logging errors silently instead of throwing exceptions.
- Updated profile update logging to include the cache buster URL.
- Improved selected tab index behavior when network connection status changes.
### Fixed
- Fixed ALT+F4 behavior in mini-window mode to prompt for confirmation if ATISes are connected before shutting down the application.
- Fixed dialog ownership for Airport Conditions and NOTAM dialogs in the ATIS sandbox.
- Fixed dependency injection memory leaks.
- Fixed cloud parsing when a cloud layer element is missing.
- Fixed tooltip display for the `wind_unit` template variable.
- Fixed an issue where the preset template could be unintentionally cleared.
- Fixed an issue preventing the maximum ATIS connection count from being enforced correctly.
- Fixed an issue causing the observed ATIS to disappear when attempting to connect an already connected ATIS.
- Fixed disposed cancellation token exceptions.
- Fixed null authentication token exception when connecting to an ATIS.
- Fixed issue with empty template variables not getting removed from text ATIS.

## v4.1.0-beta.15
YANKED

## v4.1.0-beta.14
### Added
- Added a confirmation dialog to prompt saving of unsaved airport conditions or NOTAMs free-text when switching presets.
### Changed
- Updated airport conditions and NOTAMs textboxes to be read-only for observed ATISes.
- Clear airport conditions and NOTAMs textboxes when an observed ATIS is disconnected.
### Fixed
- Improved asynchronous handling in the ATIS builder functions.
- Resolved an issue where an ATIS was not disconnected from the ATIS hub.
- Fixed an issue that caused airport conditions and NOTAMs free-text to not be saved.
- Fixed an issue that prevented the "SAVE CHANGES" action from appearing after clearing all text from the airport conditions or NOTAMs textboxes.

## v4.1.0-beta.13
### Added
- Added a release notes dialog that lists changes in the newly installed version, with an option to disable it for future updates.
- Introduced support for customizing the display order of ATIS station tabs.
- Implemented the ability to adjust the spoken speech rate of the voice ATIS.
### Changed
- Enhanced the display of static and free-text airport conditions and NOTAMs, ensuring correct text order in the UI.  
- Improved exception logging to provide more detailed error information.
- Optimized asynchronous method invocations and task cancellation for better performance.
- Reduced transceiver height to 10 meters.
- Updated to the latest version of the miniaudio library.
- Updated Avalonia packages to their latest versions.
- Increased the maximum number of cloud groups to six.
- Improved ATIS letter synchronization with other users; ATIS stations that recently went offline now retain the last used letter instead of appearing online (red letter).
- Improved validation to ensure FSD network connectivity before starting the ATIS build process.
### Fixed
- Fixed ATIS letter coloring for observed ATIS stations.
- Fixed an issue where unsaved airport conditions and NOTAM free-text were discarded when closing the static definition dialog.
- Fixed a typo in the TREND forecast regex expression.
- Adjusted window control alignment in mini-window mode for multi-monitor setups with different resolutions.
- Fixed an issue where the contraction list failed to refresh in the Presets configuration window.
- Fixed an application shutdown issue that prevented the disconnect confirmation prompt from appearing when the user pressed ALT+F4.
- Corrected ATIS letter incrementing/decrementing behavior to prevent invalid characters.
- Fixed an issue causing airport conditions and NOTAM free-text to duplicate.
- Fixed a miniaudio library crash that occurred after playing a notification sound.
- Fixed incorrect text position in airport conditions and NOTAMs read-only (blue) text segments, which caused improper text colorization.
- Fixed an issue with parsing NSW in TREND forecasts.

## v4.1.0-beta.12
YANKED

## v4.1.0-beta.11
YANKED

## v4.1.0-beta.10
### Added
- Added missing weather precipitation types.
- Added support for parsing the TREND group in METAR reports.
- Added an option to acknowledge new ATIS update notifications in the mini-view window.
- Added support for automatic CB (cumulonimbus) detection parsing.
### Changed
- Updated the mini-view window to dynamically reposition pin and restore controls based on the window's position.
- Updated the UI scrollbar style for improved consistency.
### Fixed
- Fixed an issue where the "Include before free-text" checkbox state was not remembered.
- Fixed an issue that caused the Recent Weather group to be missing.
- Fixed ATIS station sorting inconsistencies.
- Fixed spacing between NOTAM and closing statement elements in text ATIS.
- Fixed the minimum macOS version requirement; the minimum supported version is now macOS 12.
- Fixed an issue where undetermined cloud layers were not parsed correctly.

## v4.1.0-beta.9
### Changed
- Disabled text editing keyboard shortcuts in textareas to prevent unintended modifications. For example, pressing CTRL+D no longer deletes all text.
- Enhanced voice ATIS connection logic to automatically remove an existing voice ATIS when establishing a new one.
### Fixed
- Fixed an issue that caused duplicate ATIS subscriber notifications.
- Fixed a multi-threading issue that prevented the ATIS from being published to the ATIS hub.
- Fixed an issue where the ATIS letter was not sent to the FSD server, preventing it from appearing in the data feed.
- Fixed spacing in text ATIS for improved formatting of METAR components.
- Fixed an issue where extraneous text parsing characters were not being removed from the text ATIS.

## v4.1.0-beta.8
### Added
- Enabled sharing of Airport Conditions and NOTAMs text to the ATIS hub, allowing them to sync with other users.
- Included all current weather descriptors in the ATIS formatting configuration, enhancing the customization of both text and voice ATIS outputs.
- Added the missing wind shear element in both voice and text ATIS formats.
- Added an option for users to automatically fetch the real-world D-ATIS letter for all eligible stations upon connection, eliminating the need for the CTRL+D gesture. This option is disabled by default; it can be enabled in the Settings dialog.
- Added recent weather information into the default weather template string.
### Changed
- Reduced the size and improved the visual presentation of the compact window (mini-view) for better usability.
- Refined the contraction search and replace logic. Contractions no longer require an @ symbol prefix. The @ symbol can still be used to trigger the contraction search, but it will be automatically removed once a contraction variable is selected from the auto-completion results.
- Updated the ATIS letter color for observed stations to provide clearer differentiation from the user's connected ATIS stations.
- Updated the internal event bus to mitigate potential memory leaks and enhance multi-threading safety.
- Optimized voice server connection and token refresh logic.
- Modified text parsing to strip characters exclusively from airport conditions and NOTAM text, leaving METAR elements intact.
- Appended a Unix timestamp to the profile update URL to prevent caching issues.
- Modified the ATIS sandbox to include read-only text segments for the selected definitions.
### Fixed
- Resolved an issue where voice ATIS would not disconnect when the app was closed.
- Fixed the issue with the profile list failing to resort after renaming a profile.
- Corrected the missing ceiling calculation logic.
- Addressed a multi-threading issue that could cause the app to crash when publishing ATIS to the hub.
- Fixed an issue that allowed multiple instances of the app to run simultaneously.
- Fixed the problem with METAR not being parsed correctly when certain elements were missing.
- Corrected the audio recording and playback sample rate issues.
- Fixed lapse rate and rounding errors in the QFE calculation.
- Addressed a bug where the dropdown menu would not extend to the full width under certain window scaling settings.

## v4.1.0-beta.7
### Fixed
- Resolved an issue causing a disposed object exception during IDS updates.
- Fixed missing visibility and altimeter properties in websocket messages.
- Fixed an issue that allowed changing ATIS letter for observed stations when requesting real-world D-ATIS letter.

## v4.1.0-beta.6
### Added
- Added a profile file update serial number to the ATIS configuration window.
- Added the ability to customize voice and text templates for the NOTAM section.
- Added support for fetching real-world digital ATIS (D-ATIS) letters using the CTRL + D keyboard shortcut.
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