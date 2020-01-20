# Discord Image Sync

<img src="https://cdn.discordapp.com/attachments/282208855289495554/668868257071235078/x.png"><br>

Trying to learn something new. Needs tons of code refactoring as it is my first non-Unity C# project.<br><br>
ImageSync will attempt to get your token automatically (via local storage/leveldb).<br>Token will fail to be grabbed if you have a really old installation of Discord, as it stores the token in a diferent way and I couldn't be bothered to handle that. If thats you, then just put the token manually or reinstall Discord ¯\\_(ツ)_/¯ (but please note that those leveldb files seem to be generated on app close, rather than startup).<br>

Compiled portable version is on [github releases page](https://github.com/Peacerekam/Discord-ImageSync/releases).<br>
Requires [.NET Core Runtime](https://dotnet.microsoft.com/download) in order to run. Works only on Windows and with user tokens.<br>
Using some of (partially modified) [Tyrrrz/DiscordChatExporter](https://github.com/Tyrrrz/DiscordChatExporter)'s parsers and discord api request functions. 

<br>Regular use:<br>
<img src="https://cdn.discordapp.com/attachments/282208855289495554/668863558980730900/ssssss1.gif"><br><br>Can be used from the taskbar<br><img src="https://cdn.discordapp.com/attachments/282208855289495554/668863915190648832/opt.gif">
