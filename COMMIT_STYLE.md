# Commit Style

## Subject

Format: `<Prefix>: <Summary>`

| Prefix      | Use for                                                      |
| ----------- | ------------------------------------------------------------ |
| `Feat:`     | New functionality, such as a new endpoint or feature         |
| `Fix:`      | Bug fixes                                                    |
| `Refactor:` | Code restructuring that doesn't change behavior              |
| `Chore:`    | Maintenance, such as renames, config changes and small cleanups |
| `Docs:`     | Documentation only                                           |

- Start the summary with a capital letter and an imperative verb (`Implement`, `Update`, `Add`).
- Don't end the summary with a period.

## Description

- Leave it out if the subject says everything.
- Keep a short description to a sentence or two.
- If the description would be long, write it as a bulleted list with `-`.

## Co-Authored-By

Commits written by an AI agent end with a `Co-Authored-By` trailer, separated from the rest of the message by a blank line. Use the name only and leave out the `<no-reply>` email address.

```
Co-Authored-By: Claude Opus 5.5
```

## Examples

```
Fix: Return 404 when deleting a sale that doesn't exist

Co-Authored-By: Claude Opus 5.5
```

```
Chore: Update some Product feature's codes

- Update some namings
- Add product and product price deletion handling in relation with other entities

Co-Authored-By: Claude Opus 5.5
```
