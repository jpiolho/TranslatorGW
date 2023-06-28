# TranslatorGW
<p align="center">A mod for Guild Wars 1 that allows you to use non-official custom languages</p>
<p align="center">This mod uses <a href="https://github.com/Reloaded-Project/Reloaded-II">Reloaded-II</a></p>

Note that this mod is in the alpha stage

# How to install
1. Download and install [Reloaded-II](https://github.com/Reloaded-Project/Reloaded-II) if you don't have it
2. [**Click here**](https://jpiolho.github.io/QuakeReloaded/installmod.html?username=jpiolho&repo=TranslatorGW&file=TranslatorGW{tag}.7z&latestVersion=1)

# Quick-start with example language
This is an example of how to download and load a custom language:
1. Download example Portuguese.csv <a href="https://raw.githubusercontent.com/jpiolho/TranslatorGW/master/Examples/Portuguese.csv" download>here</a>, it contains a translated login screen
2. Place the .csv file in the same folder as your Guild Wars instalation (where gw.exe is located)
3. Within Reloaded-II, edit the mod configuration and set a language override. Choose any language and type in: `Portuguese,Portuguese.csv`. Save and continue
4. Start Guild Wars with Reloaded and change to the language that you chose to override.

<p align="center">
  <img width="256" height="256" alt="Logo" src="https://github.com/jpiolho/TranslatorGW/blob/main/Docs/portuguese_example.jpg">
</p>


# How to create a language file
* In general, you can just see how the example language file is created and just add a string in each line. However I'd recommend going to the mod configurations and enable "Save strings to SQLite" and set a "SQLite Path".
* Afterwards, when you play the game, whatever translation string the game decides to load will be saved into the sqlite database.
* Load this SQLite database in a SQLite viewer. If you don't know any, I'd recommend using Visual Studio Code with SQLTools SQLite extension
* In this SQLite database you can locate the StringIds and original text. Use this as a reference to add your own strings in the language csv

## Language token syntax
Guild Wars translation engine contains special tokens to help with the grammar in different languages.

### Plurals
* `[s]`: For example, `Key[s]`. Whenever there's a single item, `Key` will be shown. If there's 2 or more, then `Keys` will be shown
* `["Cães"]`: For example, `Cão` (Singular for Dog in Portuguese), will be translated into `Cães` if there's more than 1. **Note** If your string contains a dash in the middle, only the text touching the - will be translated into the plural.

### Brackets
* `[lbracket]` to add a `[`
* `[rbracket]` to add a `]`