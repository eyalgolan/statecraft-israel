### Removed

- The website (`site/`, the Astro pages in English and Hebrew) is gone, with its npm dependencies, their Dependabot
  updates and its build checks (owner, 31 Oct 2026). GitHub Pages now serves only the game: the latest release's
  web build at the same address, https://eyalgolan.github.io/statecraft-israel/demo/, and the root page redirects
  there. A release without its Pages build now fails the deploy instead of publishing a site without the game.
