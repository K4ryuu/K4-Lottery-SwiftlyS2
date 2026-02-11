# Changelog

All notable changes to this project will be documented in this file.

## [1.0.2] - 2026-02-11

### Fixed

- **CRITICAL**: Fixed configuration binding bug in `InitializeConfigs()` method affecting **both** configs
  - **LotteryConfig**: Changed `.BindConfiguration(LotteryConfigFileName)` to `.BindConfiguration(LotteryConfigSection)`
  - **CommandsConfig**: Changed `.BindConfiguration(CommandsConfigFileName)` to `.BindConfiguration(CommandsConfigSection)`
  - This bug caused all config values to use hardcoded defaults instead of reading from `config.json` and `commands.json`
  - **Impact**: Plugin configuration was completely non-functional - ticket prices, draw intervals, wallet settings, and all commands were using defaults

## [1.0.1] - 2025-12-12

### Changed

- **Config System Overhaul**: Migrated from `IOptions<T>.Value` to `IOptionsMonitor<T>.CurrentValue` pattern
  - Enables hot-reload support for configuration changes without plugin restart
  - Aligns with K4-WeaponPurchase-SwiftlyS2 plugin architecture pattern
  - `LotteryConfig` and `CommandsConfig` are now accessed via static `IOptionsMonitor` properties

- **Plugin Architecture Improvements**:
  - Added static `Core` property to Plugin class for consistent access across services
  - Refactored `InitializeConfigs()` to use the new config registration pattern
  - Simplified service initialization flow

- **LotteryService Refactoring**:
  - Converted to primary constructor syntax
  - Added explicit `Initialize()` method for controlled initialization
  - Updated all config references to use `Plugin.Lottery.CurrentValue` and `Plugin.Commands.CurrentValue`
  - Replaced instance-based Core access with static `Plugin.Core` reference

- **Database Models**:
  - Added `[Key]` attribute to all primary key properties for better Dommel ORM compatibility
  - Models: `LotteryDraw`, `LotteryTicket`, `LotteryHistory`

### Technical Details

- Version bumped from 1.0.0 to 1.0.1
- All services now use consistent static property access pattern
- Config changes are now automatically reflected without restart (via `IOptionsMonitor`)

## [1.0.0] - Initial Release

- Initial release with lottery system features
- Ticket purchases and periodic draws
- Economy API integration
- Multi-language support
