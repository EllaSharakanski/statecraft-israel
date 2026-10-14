# Ben-Gurion's 1948 recording on the start screen

The start screen can play David Ben-Gurion's reading of the sentence that proclaims the state, from the Kol Yisrael
broadcast of 14 May 1948. The owner asked for it (rulings of 2026-10-13 and 2026-10-14; `reports/Video at decision
moments.md`).

## The file

- **Source:** Israel State Archives, item ISA-Collections-Recordings-00113hj (catalogue file טס-169/21, rights field
  «גנזך המדינה»). It is the archive's copy of the broadcast recorded by Ralph Hellinger of Radio Doctor, as the archive
  publishes it on its own YouTube channel.
- **The excerpt:** 468.01 to 477.30 s, «אנו מכריזים בזאת על הקמת מדינה יהודית בארץ ישראל, היא מדינת ישראל». It is cut
  at those points and nothing else is changed: 48 kHz, 24-bit stereo WAV, 9.29 s.
- **Shipped as received** at `unity/Assets/StreamingAssets/audio/declaration-1948-isa-00113hj.wav`.
  - Its SHA-256, `2b949e12…5e1d`, is pinned in `DeclarationRecording.Sha256` and checked by a test.
  - The licence forbids altering the work, and nothing processes the file: no normalising, no denoising, no fades, no
    re-encoding.
  - Unity does not import it as an AudioClip asset. The game fetches the file when the button is pressed, and the
    browser (or, on desktop, Unity's WAV reader) decodes it at play time, so the only conversion is that technical
    decode.
- **Rights:** the Israel State Archives terms of use. The `ASSETS.yaml` row is `owner-verify` and records three open
  questions:
  - the recordist's 1980s claim (ע"א 474/86), moot in Israel since the term ended in 1998;
  - the uncertain US status;
  - the ISA's English terms page says "except commercial", while its Hebrew page says "including commercial".
- **Credit:** `NOTICE`, and on screen.

## On the start screen

- **A button.** "Play the recording" sits under the source line, with the credit "Recording: Israel State Archives"
  beside it. Pressing it plays the excerpt over the still painting, and the button becomes "Stop the recording".
  - It never plays by itself, and never on Continue or New Game.
  - It stops when the start screen closes.
  - It plays over the painting only, never over a moving image.
- **Captions.** While it plays, its words show in a dark strip above the panel:
  - in Hebrew, in Frank Ruhl Libre, right to left through the advanced text generator;
  - in English: "We hereby declare the establishment of a Jewish state in Eretz-Israel, to be known as the State of
    Israel."
- **Off in automated runs.** In the runs that skip the start screen (smoke, golden, shot, the profiles, units,
  borderproof), the button is not offered at all (`CardVideo.ForcedOff`).
- **Boot is unchanged.** The 2.7 MB file is not in the build's data and is fetched only when asked for.
