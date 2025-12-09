Player HUD quick setup (UGUI Text):

- Add a Canvas with two Text objects (HealthText, AmmoText).
- Drop the new PlayerHUD script on the Canvas (or an empty under it) and assign HealthText/AmmoText fields.
- Assign your player Health component to the Health field if auto-find does not pick it up.
- Assign the WeaponAmmo component (usually on the gun/player) to the WeaponAmmo field so ammo updates and reloads show.
- Adjust the format strings if you want different labels; "INF" is shown for infinite ammo.
