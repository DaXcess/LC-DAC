# DAC - A Lethal Company AntiCheat

> This mod is an experimental side-project of mine, and I'm not sure whether or not I will actually make an official mod out of this.
> For an already existing public AntiCheat mod, check out [AntiCheat](https://thunderstore.io/c/lethal-company/p/chuxiaaaa/AntiCheat/).

This host-only mod attempts to prevent certain actions from being performed by cheaters within a Lethal Company lobby.

## How it works

**DAC** patches a bunch of `ServerRpc` functions, adding some extra checks to see whether or not an action that is being performed is malicious in nature.
Additionally, all RPC handlers (functions like `__rpc_handler_2465912735`) for the `ServerRpc`'s are patched so that the anticheat can properly check who **actually** executed the RPC call (instead of relying on users being honest and sending their user ID as a parameter of the RPC they're calling).

This allows the anticheat to properly distinguish between real function calls and spoofed function calls, like with chat spoofing.

If a cheat is detected, the mod will respond to it by doing one of the following:

- **Warn Only**: Log the detection to the console, but don't do anything else
- **Ignore RPC**: Prevents the `ServerRpc` from being executed, effectively cancelling it
- **Kick Player**: Kicks the executing player from the server
- **Ban Player**: Bans the executing player from the server (this list is kept in memory until the game is restarted)

## Features

- **SteamID Spoofing**: When a player connects, they send their SteamID over to the server. Players can spoof this value. DAC kicks players when it detects the spoofing.
- **Anti Kick**: Detects when players try to perform actions in the game before they have finished initialization (which is used to prevent being kicked).
- **Chat Spoof**: Detects when players try to impersonate other players in chat.
- **Incorrect Ship Lever Usage**: Detects when players try to start/stop the game when they're not supposed to.
- **Incorrect Player Damaging**: Detects when players try to damage other players using incorrect damage values, while not holding a weapon or being too far away from the damaged player.
- **Player Damage Spoofing**: Detects when a player tries to use another player to damage **another** player in the lobby.
- **Ship Object Spamming**: Detects when a player tries to move around ship objects too quickly.
- **Incorrect Ship Object Rotations**: Detects when a player tries to rotate ship objects on an invalid axis
- **Incorrectly Using Terminal Commands**: Detects when players try to execute commands from the terminal, while not being on the terminal
- **Terminal Price Spoofing**: Detects when players try to buy items or reroute to moons using invalid prices
- **Incorrect Item Placement**: Detects when players try to place items in locations too far from themselves