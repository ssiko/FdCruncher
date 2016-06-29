# FdCruncher

Part of the DFS cruncher process. Run this against a data file (Tab-delimited) to crunch the optimal FanDuel NBA team. SampleInput.tab contains the sample format for data.

### Key Fields:

_**Metric**_ - Expected player total points. Usually a moving average of players past production.

_**Modifier**_ - Value used to weight metric, which is meant to represent their output for that day. Things such as pace, home v away, expected minutes vs average minutes, etc can be used to weight how you expect the player to perform.

_**Include**_ - Include this player in every team chosen

_**Exclude**_ - Exclude this player from team selection

This program will select the top X scoring teams, but is only as good as the data passed to it.