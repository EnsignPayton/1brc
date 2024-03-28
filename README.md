# One Billion Row Challenge

Okay, let's do this.

## Round 1 - Single Thread

|Time|Command|
|---|---|
|13.191s|`dotnet run -c Debug --project OneBee -- ../data/data_100m.txt`|
|11.170s|`dotnet run -c Release --project OneBee -- ../data/data_100m.txt`|
|11.564s|`dotnet publish -c Release OneBee && OneBee/bin/Release/net8.0/win-x64/publish/OneBee.exe ..data/data_100m.txt`|
|104.404s|`cargo run -- ../data/data_100m.txt`|
|9.614s|`cargo run -r -- ../data/data_100m.txt`|

All I can say is wow. The difference between debug and release Rust is massive. Rust is outperforming C# at its best, but just barely.

