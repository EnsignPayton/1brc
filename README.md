# One Billion Row Challenge

Okay, let's do this.

## Round 1

|Time|Command|
|---|---|
|13.191s|`dotnet run -c Debug --project OneBee -- ../data/data_100m.txt`|
|11.170s|`dotnet run -c Release --project OneBee -- ../data/data_100m.txt`|
|11.564s|`dotnet publish -c Release OneBee && OneBee/bin/Release/net8.0/win-x64/publish/OneBee.exe ..data/data_100m.txt`|
|104.404s|`cargo run -- ../data/data_100m.txt`|
|9.614s|`cargo run -r -- ../data/data_100m.txt`|

All I can say is wow. The difference between debug and release Rust is massive. Rust is outperforming C# at its best, but just barely.

## Round 2 - Optimize Temperature Parsing

Treat the temperatures as 16 bit signed integers and parse them by hand. Switch from rolling average to accumulating the total temperature in a 64 bit integer and averaging at the end.

Going forward, I'm only going to look at Release builds. Also skipping AoT because it didn't make a difference.

|Time|Command|
|---|---|
|7.327s|`dotnet run -c Release --project OneBee -- ../data/data_100m.txt`|
|8.661s|`cargo run -r -- ../data/data_100m.txt`|

.NET has overtaken Rust. Neat.

## Round 3 - UTF8 Numeric Parsing

Copy paste some code to avoid some of the string conversions. Still allocating a string for each lookup - not great

|Time|Command|
|---|---|
|6.762s|`dotnet run -c Release --project OneBee -- ../data/data_100m.txt`|

## Round 4 - Stream Parser Fix

Remove my hacky fix and do a more legitimate fix. Not sure if it improved perf or if that's just margin of error.

|Time|Command|
|---|---|
|6.586s|`dotnet run -c Release --project OneBee -- ../data/data_100m.txt`|
