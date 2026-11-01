# LAN headset voice and escape unlock verification

2026-11-01, Unity 6000.4.8f1, Windows.

- 30 audio buffer and role checks passed: B → A in every stage, A → B only in Escaped, no self audio in any stage, invalid frame size rejection, prebuffer, decoded amplitude, duplicate/out-of-order rejection, underrun silence, packet-loss silence, sequence wrap, long-gap recovery and disconnect flush.
- 43 existing puzzle and GPU grayscale checks passed.
- 36 Editor-host + standalone Development Player checks passed using real loopback networking. Verified that solving only the color puzzle preserves restrictions; solving both puzzles hides both accessories, disables A's grayscale, reveals the clue layer, opens A's microphone, and allows actual A → B AudioSource output while B → A continues. Restart restores both accessories, grayscale, clue masking and one-way voice, closes A capture, and stops B playback. Old-round packets are rejected even while two-way voice is unlocked. Also verified mute, disconnect and reconnect. B → A output peak: 0.08631229.
- The available Windows microphone also opened on B and produced transmitted packets before the synthetic test. No microphone audio was saved.
- Inspected the 1280 × 720 lobby UI. Reserved space for microphone settings above the connection card and adjusted dropdown contrast.
- Final Windows release build succeeded with 0 errors and 486 warnings. Observed warnings concern the installed inference package's ConvGeneric compute-shader variants. Output: `Builds/FacilityEscape-Voice/FacilityEscape.exe`; copyable archive: `Builds/FacilityEscape-Voice.zip`.

Run scripts: `TestMultiEscapeVoiceBuffer.Run`, `TestMultiEscapeVoiceLan.Host`, `TestMultiEscapeVoiceLan.Run`. The integration runner expects a Development Player launched with `-escape-join 127.0.0.1 -escape-lan-probe <project>/artifacts/voice-client.json`. Wait for its report file before starting the integration runner. Test tone commands only exist in the opt-in development probe.

Physical two-laptop Wi-Fi latency and the user's actual pair of headsets have not been listened to. Use the same updated build on both PCs, choose each headset microphone, and select each headset as the Windows output. Before escape only B → A voice is allowed; after both puzzles both directions open automatically. User mute preferences persist. Environment and puzzle sounds remain audible throughout.

An initial development build hit stale Editor serialization metadata after editing the probe. A clean script compilation corrected it. The later build succeeded; its one reported error was the CLI request timing out while shader compilation continued. The actual test executable completed the integration checks above. Final release build status is recorded separately in `Voice-Build-release.txt`.
