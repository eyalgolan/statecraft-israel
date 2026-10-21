There is no hosted build yet. For now the game runs from source on your own machine.

## Run it yourself

1. Install the tools in [docs/dev-setup.md](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/dev-setup.md): .NET 10, Unity 6000.3.25f1 with Web Build Support, and `uv`.
2. Run `tools/build_sim_for_unity.sh`.
3. Build the web version. The command is in [docs/dev-setup.md](https://github.com/eyalgolan/statecraft-israel/blob/main/docs/dev-setup.md).
4. Run `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`, then open http://localhost:8000/.

The exact commands, the tests and the browser check are in [BUILDING.md](https://github.com/eyalgolan/statecraft-israel/blob/main/BUILDING.md).

## What you can play

Monthly turns from 15 May 1948 to 15 January 1952: the map, the Cabinet, the Budget and Defence screens, sourced events at their real dates, the “Why?” panel and autosave. Much of the GDD is still being built.

## Hosting the build on this site

What a browser build for this site needs is in the [site's README](https://github.com/eyalgolan/statecraft-israel/blob/main/site/README.md#hosting-the-game-here).
