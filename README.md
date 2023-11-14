# Table of Contents
1. [Setting up external tools](#setting-up-external-tools)
   * [Setting up Destiny Application](#setting-up-destiny-application)
   * [Setting up Discord bot](#setting-up-discord-bot)
   * [SQLite Browser](#sqlite-browser)
2. [Starting up project](#starting-up-project)
3. [Possible errors on startup](#possible-errors-on-startup)
4. [Initial settings](#initial-settings)
5. [Post setup](#post-setup)

# Setting up external tools

To make this app work properly you will need 2 main components:
- Destiny application api key
- Discord bot token

# Setting up Destiny Application

1. Visit [Application creation page](https://www.bungie.net/en/Application)
2. Click `Create New App`
3. Since we're not using any OAuth2 stuff here, we don't care about most settings
4. Get your API Key from the app, we'll be using this

# Setting up Discord bot

1. Visit [Bot creation page](https://discord.com/developers/applications)
2. Open `New Application` > `OAuth2` > `URL Generator`
3. Check: `bot`, `application.commands`, `Administrator`
4. Create link and invite bot to your server
5. Get and save your bot token in `Bot` tab

# SQLite Browser

This application uses SQLite as it's storage, so I strongly recommend users to download and use SQLite Browser

[Download Link](https://sqlitebrowser.org/dl/)

# Starting up project

1. Download source code
2. Open `/src/Atheon/Atheon/`
3. Run `RunProject.bat`

# Possible errors on startup

1. Be sure to download [Dotnet 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. Try running `dotnet restore` in console from `/src/Atheon/Atheon/` folder
3. If you're on windows, try syncing system time

# Initial settings

If your application started successfully, next step is configuration
1. Open [Default swagger page](https://localhost:7033/swagger)
2. Locate `SettingsStorage`
3. Open `/api/SettingsStorage/SetDiscordToken/{reload}`, replace `"string"` with `"your_discord_bot_token"`, quotation marks are important to keep, set `reload` to true
4. Open `/api/SettingsStorage/SetBungieApiKey`, replace `"string"` with `"your_destiny_api_key"`, quotation marks are important to keep
5. Open `/api/SettingsStorage/SetDestinyManifestPath/{reload}`, replace `"string"` with path to an existing folder on your drive, quotation marks are important to keep, set `reload` to true
   * This folder will be used to store destiny 2 latest data that's crucial to this app
   * Make sure that all slashes are forward, apparently this may be an issue if not
6. Close application and start again, look for warnings in console logs

# Post setup

1. Make sure that bot is online and running
2. Get your Destiny 2 clan Id:
   * Open https://www.bungie.net/7/en/Destiny
   * Go to `COMMUNITY` > `MY CLAN`
   * Look at page address
   * `https://www.bungie.net/en/ClanV2/Chat?groupId={clan_id}`, `clan_id` will be the id you're looking for
3. Run discord commands:
   * `/settings clan-add` to add clan with your id
   * Other commands for more settings
4. If you're adding your clan first time, after some time message will pop up that clan is ready to work with
5. Have fun using this bot!
