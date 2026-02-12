<a name="readme-top"></a>

![GitHub tag (with filter)](https://img.shields.io/github/v/tag/K4ryuu/K4-Lottery-SwiftlyS2?style=for-the-badge&label=Version)
![GitHub Repo stars](https://img.shields.io/github/stars/K4ryuu/K4-Lottery-SwiftlyS2?style=for-the-badge)
![GitHub issues](https://img.shields.io/github/issues/K4ryuu/K4-Lottery-SwiftlyS2?style=for-the-badge)
![GitHub](https://img.shields.io/github/license/K4ryuu/K4-Lottery-SwiftlyS2?style=for-the-badge)
![GitHub all releases](https://img.shields.io/github/downloads/K4ryuu/K4-Lottery-SwiftlyS2/total?style=for-the-badge)
[![Discord](https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://dsc.gg/k4-fanbase)

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">KitsuneLab©</h1>
  <h3 align="center">K4-Lottery</h3>
  <a align="center">A lottery system for Counter-Strike 2 servers. Players buy tickets, and at configurable intervals a winner is randomly selected to receive a percentage of the total pot.</a>

  <p align="center">
    <br />
    <a href="https://github.com/K4ryuu/K4-Lottery-SwiftlyS2/releases/latest">Download</a>
    ·
    <a href="https://github.com/K4ryuu/K4-Lottery-SwiftlyS2/issues/new?assignees=K4ryuu&labels=bug&projects=&template=bug_report.md&title=%5BBUG%5D">Report Bug</a>
    ·
    <a href="https://github.com/K4ryuu/K4-Lottery-SwiftlyS2/issues/new?assignees=K4ryuu&labels=enhancement&projects=&template=feature_request.md&title=%5BREQ%5D">Request Feature</a>
  </p>
</div>

### Support My Work

I create free, open-source Counter-Strike 2 plugins for the community. If you'd like to support my work, consider becoming a sponsor!

#### 💖 GitHub Sponsors

Support this project through [GitHub Sponsors](https://github.com/sponsors/K4ryuu) with flexible options:

- **One-time** or **monthly** contributions
- **Custom amount** - choose what works for you
- **Multiple tiers available** - from basic benefits to priority support or private project access

Every contribution helps me dedicate more time to development, support, and creating new features. Thank you! 🙏

<p align="center">
  <a href="https://github.com/sponsors/K4ryuu">
    <img src="https://img.shields.io/badge/sponsor-30363D?style=for-the-badge&logo=GitHub-Sponsors&logoColor=#EA4AAA" alt="GitHub Sponsors" />
  </a>
</p>

⭐ **Or support me for free by starring this repository!**
---

## Features

- **Ticket Purchases** - Players buy tickets with in-game currency
- **Configurable Intervals** - Set draw frequency in days
- **Winner Percentage** - Configure how much of the pot the winner receives
- **Max Tickets** - Limit how many tickets each player can buy per draw
- **Automatic Draws** - System automatically processes draws at scheduled times
- **Draw History** - View recent lottery winners
- **Top Winners** - Leaderboard of all-time top winners
- **Offline Rewards** - Winners receive their prize even if offline
- **Economy Integration** - Works with Economy plugin
- **Multi-Server Safe** - Safe to use across multiple servers with shared database
- **Cryptographically Secure** - Uses secure random number generation for fair winner selection

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Dependencies

- [**SwiftlyS2**](https://github.com/swiftly-solution/swiftlys2): Server plugin framework for Counter-Strike 2
- **Database**: One of the following supported databases:
  - **MySQL / MariaDB** - Recommended for production
  - **PostgreSQL** - Full support
  - **SQLite** - Great for single-server setups
- [**Economy**](https://github.com/SwiftlyS2-Plugins/Economy): Required for ticket purchases and payouts

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Installation

1. Install [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) on your server
2. Configure your database connection in SwiftlyS2's `database.jsonc`
3. [Download the latest release](https://github.com/K4ryuu/K4-Lottery-SwiftlyS2/releases/latest)
4. Extract to your server's `swiftlys2/plugins/` directory
5. Configure the plugin files (see Configuration section)
6. Restart your server - database tables will be created automatically

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Configuration

### `config.json` - Lottery Settings

| Option               | Description                                    | Default     |
| -------------------- | ---------------------------------------------- | ----------- |
| `DatabaseConnection` | Database connection name (from database.jsonc) | `"host"`    |
| `WalletKind`         | Economy wallet type to use                     | `"credits"` |
| `TicketPrice`        | Cost per ticket                                | `100`       |
| `MaxTicketsPerPlayer`| Maximum tickets per player per draw            | `10`        |
| `DrawIntervalDays`   | Days between draws                             | `7`         |
| `WinnerPercentage`   | Percentage of pot the winner receives          | `80.0`      |
| `DrawTime`           | Time of day for draw (24h format)              | `"20:00"`   |

### `commands.json` - Command Customization

Customize command names and aliases for all lottery commands.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Commands

| Command                  | Description                    |
| ------------------------ | ------------------------------ |
| `!lottery`               | Show help / lottery commands   |
| `!lottery buy [amount]`  | Buy lottery tickets            |
| `!lottery info`          | View current lottery info      |
| `!lottery top`           | View top all-time winners      |
| `!lottery history`       | View recent draw history       |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Multi-Server Support

The plugin is designed to be **safe for multi-server environments** with a shared database:

- **Single Draw Processing** - Each draw is processed exactly once, even if multiple servers are running. The database ensures only the first server to complete a draw succeeds.
- **Race Condition Protection** - Built-in safeguards prevent duplicate ticket purchases or double-processing of draws.
- **Shared Lottery Pool** - All servers contribute to and draw from the same lottery pool when using a shared database.

> **Recommended**: Use MySQL/MariaDB or PostgreSQL for multi-server setups. SQLite is only suitable for single-server configurations.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Database

The plugin uses automatic schema management with FluentMigrator. Tables are created automatically on first run.

### Supported Databases

| Database        | Status  | Notes                                      |
| --------------- | ------- | ------------------------------------------ |
| MySQL / MariaDB | ✅ Full | Recommended for multi-server setups        |
| PostgreSQL      | ✅ Full | Alternative for existing Postgres setups   |
| SQLite          | ✅ Full | Perfect for single-server, no setup needed |

### Database Tables

- `k4_lottery_draws` - Draw records (dates, pot, winner info)
- `k4_lottery_tickets` - Player tickets per draw
- `k4_lottery_history` - All-time player statistics

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## Translations

All messages are fully customizable via the `translations/` folder. The plugin ships with English (`en.jsonc`) by default.

To add a new language:

1. Copy `en.jsonc` to your language code (e.g., `hu.jsonc`, `de.jsonc`)
2. Translate all values
3. The plugin will automatically use the player's preferred language

<p align="right">(<a href="#readme-top">back to top</a>)</p>

---

## License

Distributed under the GPL-3.0 License. See [`LICENSE.md`](LICENSE.md) for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
