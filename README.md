# Keymaster

I got tired of digging through a key tool before every raid, so I made the keys come to me.

Keymaster adds two items:

- **KM Case** rides on your PMC, best in your secure container. Before each PMC raid it is loaded with the keys for the map you are
  about to play, plus the amount of each currency you set. After the raid, spare keys and excess money go back to
  your stash on their own.
- **KM Vault** is a stash item that holds your whole key collection, grouped by the map each key belongs to.

Both open in a scrollable panel that shows only what they hold.

## How to use

- Buy the **KM Case** from Therapist at loyalty level 1 (2,000,000 roubles by default) and put it on your PMC.
  The secure container is the safe place: anywhere else a death loses the case with its keys and money, and the game
  warns you when you put it there (F12, `I like to live dangerously`, turns that off). The **KM Vault** arrives in your mail on first login; keep it in your stash and put your keys in it.
  One of each per profile.
- Start a PMC raid: the case is loaded with that map's keys (vault first, then stash) and your money targets.
  Access cards move in when you press Ready at the map screen. Back at the menu, spares go home on their own.
- Take a transit to another map: the case is swapped for the next map's keys and its transit cards, and the last
  map's keys go back to the vault. Money is left as it is. Without the next map's card the game refuses the transit
  as usual.
- Right click: **Set target** on a money stack in the case, **Dump excess** on the case, **Bring on every map** on
  a key.
- Ctrl+click or drag money and keys into an open case or vault window. Money is capped at the game's raid-entry
  limits, counting money you carry elsewhere.
- Server settings, the vault holders list and a vault resend are on the Keymaster page under MOD PAGES in the SPT
  web landing page. Client settings are in F12.

The full guide, with screenshots, is on the Forge page.

## Requirements

- SPT 4.1.3 or later.
- Fika is supported. Install the client half on players only.
- Fika headless safe: the client plugin switches itself off on a headless.

## Install

Extract the release archive into your SPT root. It contains:

- `SPT_Runtime/user/mods/LennoxP90-Keymaster` (server half)
- `BepInEx/plugins/LennoxP90-Keymaster` (client half)

Restart the server, then launch the game. With only the server half installed the items still work as plain
containers in the stock window.

## Configuration

Server settings are on the Keymaster page under MOD PAGES in the SPT web landing page (admin sign-in), and in
the SPT web Config Editor. Changes apply when you save:

- the case price in roubles and the Therapist loyalty level it needs;
- whether the vault is mailed on first login, and whether it is mailed again if you lose it.

Client settings are in the F12 menu: which copy of a key goes into the case when you own several, and verbose
logging.

## Uninstall

A profile that holds Keymaster items will not load once the server half is gone. Before removing the mod, empty both
the KM Case and the KM Vault and sell them. Then delete both `LennoxP90-Keymaster` folders.

## Known limits

- Wildcard keys are never learned.
- A key you do not own a copy of is skipped; the summary reports the shortfall.
- If you lose the vault after claiming it, a new one is mailed only once the old mail would have expired.
- Other mods that rewrite container filters late in loading can undo the case's and vault's filters.

## Licence

The source is MIT. The models in `bundles/keymaster` are recoloured copies of items from Escape from Tarkov and are
not covered by that licence; see `LICENSE`.
