# Voice Command Examples

Supported Tier 1 intents for Nicaraguan Spanish field conditions.
All commands are transcribed via Whisper (`whisper-1`, `language=es`) and parsed by GPT-4o against the farm's live roster.

Supported intents: `create_animal`, `create_note`, `move_animal`, `move_lot`, `register_newborn`.

---

## Animal identification

An animal can be referenced by **any** of three identifiers. Preference in practice: name or visual tag first, CUIA as fallback (it's long to say aloud).

| Identifier | Roster field | How it's spoken | Example |
|---|---|---|---|
| Name | `name` | Natural name, often with article | "la bonita", "el rey del monte" |
| Visual tag (arete) | `earTag` | Number, usually 4 digits | "el arete 2315", "la dos tres uno cinco" |
| CUIA | `cuia` | Full number, 10 digits in two groups | "la CUIA cero uno cuatro cinco seis dos dos siete cuatro" |

GPT-4o matches the spoken identifier against all three fields before resolving `animalId`.

### Real roster examples

| Name | Visual tag | CUIA |
|---|---|---|
| La bonita | 2315 | 01456 2274 |
| La llorona | 2319 | 01456 2266 |
| Muerta | 2487 | 01178 3511 |
| La tica | 2299 | 01311 2504 |
| El rey del monte | 2298 | 01456 2241 |

---

## `create_animal`

Register a new animal on the farm. Used both for adult animals being added to the herd and for calves that are offspring of a known mother.

```
"Registrar vaca colorada arete 017683344, se llama la milagro, está en el lote forro y pertenece a Carla y Jaime."
"Registrar vaca CUIA 017683344, la bonita, lote parido, pertenece a Carla."
"Registrar toro arete 2315, el rey del monte, lote sementales, pertenece a Jaime."
"Registrar ternero hijo de la 017683344, lote terneros, pertenece a Jaime, nació el 22 de mayo de 2020."
"Registrar ternera hija de la bonita, lote terneras, nació ayer, pertenece a Carla."
```

**Resolved entities:**

| Field | Description |
|---|---|
| `animalName` | The name given to the animal, e.g. "la milagro", "la bonita" |
| `earTag` | Visual tag or CUIA spoken at registration, e.g. "017683344", "2315" |
| `sex` | Derived from spoken type: vaca/ternera → female, toro/ternero → male |
| `color` | Coat color if mentioned, e.g. "colorada", "negro", "pinto" |
| `lotId` | Resolved from the lot name mentioned |
| `ownerNames` | One or more owners as a string array, e.g. `["Carla", "Jaime"]` |
| `motherId` | Resolved from mother's CUIA, visual tag, or name (optional, for calves) |
| `birthDate` | ISO 8601 date, resolved from relative ("ayer", "hoy") or explicit ("22 de mayo de 2020") |

> `motherId` is validated against the roster. `ownerNames` are passed as plain strings — owner resolution happens server-side.

---

## `create_note`

Attach a free-text note to an animal.

```
"Nota para la bonita: está coja de la pata trasera derecha."
"Apuntá para el arete 2315: tiene garrapatas en el cuello."
"Ponle una nota a la llorona: la vi muy quieta hoy, hay que revisarla."
"Nota para la CUIA cero uno cuatro cinco seis dos dos siete cuatro: revisar al mediodía."
```

**Resolved entities:** `animalId`, `noteText`

---

## `move_animal`

Move a single animal to a different lot.

```
"Mover la bonita al lote Norte."
"Pasá el rey del monte al lote de los sementales."
"Mové el arete 2487 al lote Potrero Grande."
"Llevar la tica al lote de las vacas secas."
```

**Resolved entities:** `animalId`, `lotId`

---

## `move_lot`

Move an entire lot to a different paddock (potrero).

```
"Mover el lote Norte al potrero La Ceiba."
"Pasá el lote de las vacas al potrero nuevo."
"Mové el lote Sementales al potrero del río."
"Llevar el lote 3 al potrero El Tigre."
```

**Resolved entities:** `lotId`, `targetPaddockId`

---

## `register_newborn`

Register a calf born from a known mother. Newborns do not have an ear tag yet — the tag is assigned later.

```
"La vaca Lucero tuvo ternero macho el 22 de mayo."
"La 2903 tuvo hembra hoy."
"La Milagro tuvo ternero macho colorado ayer."
"La bonita parió hembra esta mañana."
```

**Resolved entities:** `motherId`, `sex`, `color` (optional coat color, e.g. "colorado", "negro", "pinto"), `birthDate` (ISO 8601 date resolved from relative expressions like "hoy", "ayer", or explicit dates like "el 22 de mayo")

---

## `unknown`

Returned when the command doesn't match a supported intent or confidence drops below 0.5 after entity validation.

```
"¿Cuántas vacas tenemos en el lote Norte?"   → unknown
"El precio del maíz subió."                  → unknown
"[inaudible / background noise only]"        → unknown (empty transcript)
```

---

## Confidence & Validation Rules

| Situation | Outcome |
|---|---|
| Entity ID not found in roster | ID set to `null`, confidence `− 0.2` |
| Confidence drops below `0.5` | Intent downgraded to `unknown` |
| Empty transcript | `unknown`, no GPT-4o call made |
| Malformed GPT-4o JSON | `unknown`, job marked `completed` |
| Whisper or S3 failure | Job marked `failed` |
| GPT-4o timeout (> 5 s) | Job marked `failed` |
