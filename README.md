# Discord-ImageSync

Trying ot learn something new. Needs tons of code refactoring as it is my first non-Unity C# project.

<img src="https://cdn.discordapp.com/attachments/282208855289495554/668862076013379584/ss22.gif"><img src="https://cdn.discordapp.com/attachments/282208855289495554/668861658466091008/ss2.gif">

ImageSync will attempt to get your token automatically (via local storage/leveldb).<br>Token will fail to be grabbed if you have a really old installation of Discord, as it stores the token in a diferent way and I couldn't be bothered to handle that. If thats you, then just put the token manually or reinstall Discord ¯\\_(ツ)_/¯ (but please note that those leveldb files seem to be generated on app close, rather than startup).<br><br>

Requires [.NET Core Runtime](https://dotnet.microsoft.com/download) in order to run. Works only on Windows and with user tokens.<br>
Using some of (partially modified) [Tyrrrz/DiscordChatExporter](https://github.com/Tyrrrz/DiscordChatExporter)'s parsers and discord api request functions. 
