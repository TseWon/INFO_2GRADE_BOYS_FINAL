# LAN implementation verification — 2026-10-22

Project: Unity 6000.4.8f1, MultiScene1.
Packages: Netcode for GameObjects 2.13.3; Unity Transport 2.7.2.

- 43 pure rule / snapshot serialization / GPU grayscale checks passed.
- 63 two-process checks passed using an Editor host and a standalone Development Player client on 127.0.0.1.
- Tested fixed A/B input ownership, partner avatar visibility, denied B start/input, same randomized answers and clue board, synchronized timer, actual control raycasts, wrong-color 3-second lockout, wrong-lever dial reset, dial states, simultaneous 20/40-second shocks, escape and timer hiding, frozen post-success time, two-way head rotation and movement, restart and new answers, return to chairs, 60-second failure, client departure, reconnect, and host departure.
- A third standalone client was rejected while the original two stayed connected.
- Inspected A's rendered grayscale view showing B's placeholder avatar.
- Release Windows x64 build succeeded with zero build errors. One expected Pipeline warning: no runtime automation configuration (automation is disabled in the shipped Player).
- Connected the Editor as B to the final Release Player host and confirmed Observer ownership.
- Real cross-laptop Wi-Fi and firewall/router configuration have not been tested; local tests use two independent processes over loopback.

Deliverable: Builds/FacilityEscape-LAN/FacilityEscape.exe and complete accompanying folder.
Probe code is excluded from Release builds. Temporary test processes have been stopped.
