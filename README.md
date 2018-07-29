# EvE-Build

![Program image](https://i.imgur.com/Z6sfVCN.png)

A small tool for calculating profit margins of buildable in game items.

# How to setup
Download the source and run from Visual Studio or grab a release version and run that. The first time you run it EvE-Build will download the latest item database directly from CCP and a copy of the market categories from fuzzwork, This can take a while as the item database ~200MB in size. Once it is downloaded it will extract all files and place them into a subdirectory of the executable, the next time you start up it will use this (unless there is an update which will trigger it to download the latest version).

# How to use
Once everything has been loaded you will get a list of items of the left which you can search through, or use the market groups in the tab on the right. Prices will automatically be downloaded and updated every few minutes as long as EvE-Build is open.

# FAQ
## New items are missing
Ahh well, looks like CCP updated the item DB again. This is a simple fix, just make an issue and I'll update the file that EvE-Build looks at on startup and it should grab the new item database when it startup next time.

## Why does it take a minute to startup?
The item databse that CCP makes avaliable is quite big so it takes a while to look through it and grab all the inforamtion that is needed, This happens every time EvE-Build startup and it could be changed to cache the extracted data into a smaller file so that it can be loaded. If you feel that you want it to load faster [make an issue](https://github.com/crener/EvE-Build/issues/new) and that will probably give me the motivation to add it...

## EvE-Build is missing something
Hmm, well I don't really keep up with EvE anymore... But I'm happy to support the program so put up an issue and I'll look into adding it.
