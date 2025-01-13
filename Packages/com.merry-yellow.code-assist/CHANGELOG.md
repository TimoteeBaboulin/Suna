List of new features, bug fixes and improvements

# Version 1.3.10
* Hotfix for type resolving
* Usability improvements for VSCode exporter
* More filters for expected error logs

# Version 1.3.9
* Hotfix for Unity sink

# Version 1.3.8
* Hotfix for Unity sink

# Version 1.3.7
* Bugfix for exporter/updater
* Bugfix for logging options

# Version 1.3.6
* VSCode readme updated

# Version 1.3.5
* JSON library switched back to Newtonsoft.JSON
* Rule xml files removed from Unity asset
* Unnessary analyzer binaries are removed from Roslyn Analyzer

# Version 1.3.4
* Stability improvements for network (mqttnet)
* Hotfix for JSON mapping

# Version 1.3.3
* Full version for VSCode is released!
* Fix for VSCode adorments when document is modified
* Binary files removed from Unity asset
* Bugfix for network (mqttnet server)
* VSCode extension stability improvements
* VSCode roslyn analyzer stability improvements

# Version 1.3.2
* Fixed cross-platform issues for Linux and macOS

# Version 1.3.1
* Updated VSCode manifest and readme

# Version 1.3.0
* Initial release for VSCode

# Version 1.2.6
* Exporting when Unity is in play mode
* Some error messages have been made more user friendly

# Version 1.2.5
* Released on itch.io https://meryel.itch.io/unity-code-assist
* Url changes

# Version 1.2.4
* Bugfix for exporter with long file paths
* Bugfix for retrieving animation and animator data
 
# Version 1.2.3
* Bugfix for exporter when overwriting files
* Bugfix for inline visuals when active game object changes

# Version 1.2.2
* Bugfix for exporter when facing race condition

# Version 1.2.1
* Typo fix for Options page
* Bugfix for retrieving animation and animator data

# Version 1.2.0
* New gpt provider: Google Gemini, can be used as an alternative to OpenAI ChatGPT
* New feature: CodeLens2Gpt. Can request gpt queries from the CodeLens of methods and classes
* New feature: Context aware gpt, gpt prompts are embedded with Unity, scene and object information
* New feature: Package, asset has relocated under Packages (from Assets), along with Unity setup menu items
* New code completions: Animation and Animator classes and Invoke/Coroutine/Broadcast methods can be auto completed
* New inline visuals: Animation and Animator classes can display inline information

# Version 1.1.12
* External binary files have been customized and minimized
* Domain reloading time have been reduced
* Stability and usability improvements for exporter/updater
* Bugfix for Feedback window

# Version 1.1.11
* Gpt completion endpoint fixed for OpenAI API changes
* Bugfix for Input Manager monitor
* Bugfix for logging to Visual Studio output window
* Bugfix for About window

# Version 1.1.10
* Version skipped for compatibility with other assets

# Version 1.1.9
* Gpt support for chat and edit
* More options added for Gpt
* Overall stability improvements
* Exporter shows file locks if update/export is unsuccessful

# Version 1.1.8
* Bugfix for non-Unity solutions

# Version 1.1.7
* Bugfix for Visual Studio freeze

# Version 1.1.6
* Gpt support added for shader files
* Stability improvements for Unity.ScriptFinder

# Version 1.1.5
* Stability and usability improvements for exporter/updater
* Bugfix for Transformer Linq and Auto Input Manager
* Usability for Transformer window, disabling it if not connected to Unity
* Enhancement for completions, sorting numerical values correctly https://github.com/merryyellow/Unity-Code-Assist/issues/6

# Version 1.1.4
* Auto Input Manager is now compatible with binary asset files
* Stability and usability improvements for Transformer windows

# Version 1.1.3
* Analyzers are working at a separate process https://github.com/merryyellow/Unity-Code-Assist/issues/20
* Inline visuals stability and performance improvements https://github.com/merryyellow/Unity-Code-Assist/issues/22 https://github.com/merryyellow/Unity-Code-Assist/issues/24
* Exporter/updater stability improvements https://github.com/merryyellow/Unity-Code-Assist/issues/19 https://github.com/merryyellow/Unity-Code-Assist/issues/23
* Transformer window stability improvements https://github.com/merryyellow/Unity-Code-Assist/issues/21
* Bugfix for Gpt busy icon positioning https://github.com/merryyellow/Unity-Code-Assist/issues/24

# Version 1.1.2
* Bugfix for Yaml file parsing of InputManager

# Version 1.1.1
* Bugfix for error handling of binary file parsing

# Version 1.1.0
* New feature: Generative AI. Use OpenAI ChatGPT within comments to complete your code
* New feature: Visual Studio menus. Access Unity Code Assist from "Extensions"->"Unity Code Assist"
* New code completions: PlayerPrefs, EditorPrefs and Input classes' methods can be auto completed
* New inline visuals: PlayerPrefs, EditorPrefs and Input classes' methods can display inline information
* New code transformer: Auto Input Manager. Converts legacy input code into the new Input Manager

# Version 1.0.0.21
* Stability improvements for Unity ScriptFinder class

# Version 1.0.0.20
* Bugfix for crash at startup https://github.com/merryyellow/Unity-Code-Assist/issues/18

# Version 1.0.0.19
* More logging for error tracking

# Version 1.0.0.18
* Overall stability improvements, nullable references enabled for codebase
* Stability improvements for communications, when reconnection occurs

# Version 1.0.0.17
* Stability improvements for Unity ScriptFinder class

# Version 1.0.0.16
* Bugfix for Visual Studio events concurrency

# Version 1.0.0.15
* Visual Studio events are reimplemented for both stability and performance
* Removal of possible Task deadlocks
* Usability improvements for exporter/updater
* Bugfix for exporter/updater, where prompts can appear twice
* Bugfix for Inline Texts, where const null identifier may raise exceptions https://github.com/merryyellow/Unity-Code-Assist/issues/16
* Bugfix for communications, where tags&layers are sent for the first time

# Version 1.0.0.14
* Usability improvements for Visual Studio Status window where project is not a Unity project
* Bugfix for communication reinitialization, where projects are closed and opened from Visual Studio https://github.com/merryyellow/Unity-Code-Assist/issues/15
* Bugfix for Visual Studio events' initialization https://github.com/merryyellow/Unity-Code-Assist/issues/14

# Version 1.0.0.13
* Bugfix for Unity where target object is neither Component nor MonoBehaviour https://github.com/merryyellow/Unity-Code-Assist/issues/13

# Version 1.0.0.12
* New Feature: Updating Unity asset from Visual Studio
* New Feature: Online error reporting of Unity errors
* Usability improvement for Visual Studio Feedback window
* Bugfix for Inline Texts where there is no class declaration https://github.com/merryyellow/Unity-Code-Assist/issues/5
* Bugfix for Code Completion where there is no class declaration https://github.com/merryyellow/Unity-Code-Assist/issues/10
* Bugfix for Visual Studio output window, where it may be unavailable

# Version 1.0.0.11
* Bugfix for Visual Studio events' initialization https://github.com/merryyellow/Unity-Code-Assist/issues/3
* Exporter now has more logs

# Version 1.0.0.10
* Bugfix for Inline Texts where leading trivia is absent https://github.com/merryyellow/Unity-Code-Assist/issues/1
* Bugfix for Visual Studio events' initialization https://github.com/merryyellow/Unity-Code-Assist/issues/2

# Version 1.0.0.9
* Minor adjustments for initialization logging

# Version 1.0.0.8
* Online analytics added
* Bugfix for Inline Texts, Visual Studio code preview screen does not raise any exception anymore

# Version 1.0.0.7
* Online error tracker added

# Version 1.0.0.6
* Exporter is always disabled for non Unity projects

# Version 1.0.0.5
* Lite version released at Visual Studio Marketplace
* Exporting Unity asset from Visual Studio
* Unity package become package independent (Removed Newtonsoft.Json dependency)

# Version 1.0.0
* First release!
* Released on Unity Asset Store