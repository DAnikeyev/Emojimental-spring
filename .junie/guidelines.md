## Project Guidelines

- Project: `Emojimental` is a Blazor WebAssembly incremental game about bringing the warm spring.
- User-facing website content should not display letter characters. Prefer emojis and numbers for anything visible to players. EmojiBank.cs is used for this.
- All values related to game resources  and cooldowns should be stored as Stat.cs of BigDouble.cs class.
- Avoid excessive builds. If verification is needed, prefer a single targeted build rather than repeated full builds.
- Don't do unit testing for this project.