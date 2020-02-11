# Discord Image Sync 

<img src="https://cdn.discordapp.com/attachments/366003455049072642/675811709054615583/unknown.png"><br>


Trying to learn something new. Needs tons of code refactoring as it is my first non-Unity C# project.<br>ImageSync downloads all **attachments** and **embeds**, that includes stuff like:
* direct links to images or videos (as long as they embed within Discord)
* twitter links
* gfycat links 
* etc

<br>

The app will attempt to get your token automatically (via local storage/leveldb).<br>Token will fail to be grabbed if you have a really old Discord installation, as it stores the token in a diferent way and I couldn't be bothered to handle that. If thats you, then just put the token manually into config.json file or reinstall Discord ¯\\_(ツ)_/¯ (but please note that those leveldb files seem to be generated on app close, rather than startup). I am using some of (partially modified) [Tyrrrz/DiscordChatExporter](https://github.com/Tyrrrz/DiscordChatExporter)'s parsers and discord api request functions. 
<br> 

## This app is within Discord TOS gray area

**Use at your own risk**, however realisticly speaking you're pretty safe when using it.<br>It might sounds like something that spams API with tons of requests, but it's made in such a way that it doesn't request getting new messages until it finished downloading images from the previous bulk. This means there should be at least a good few seconds between API calls, essentially making it no different from a regular Discord client.

## Download

Compiled portable version is on [github releases page](https://github.com/Peacerekam/Discord-ImageSync/releases).<br>
Requires [.NET Core Runtime](https://dotnet.microsoft.com/download) in order to run. Works only on Windows and with user tokens.<br>


<br>

## Regular use:

<br><img src="https://cdn.discordapp.com/attachments/282208855289495554/668863558980730900/ssssss1.gif"><br><br>

## Can be used from the taskbar

<br><img src="https://cdn.discordapp.com/attachments/282208855289495554/668863915190648832/opt.gif">
