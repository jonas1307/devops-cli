# devops-cli

CLI for interacting with Azure DevOps via REST API, without depending on the `az` CLI.

## Requirements

- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Azure DevOps Personal Access Token (see [PAT permissions](#pat-permissions) below)

## Installation

```powershell
dotnet publish DevOps.Console/DevOps.Console.csproj -c Release -o ./publish
```

Add the `publish` directory to your `PATH`, or copy the executable to a directory already on your `PATH`.

## Configuration

```powershell
devops config -o https://dev.azure.com/myorg -p <PAT>
devops config -P MyProject              # set default project
devops config -T "MyProject Team"       # set default team (for iteration and area resolution)
devops config -e your@email.com         # set email manually if auto-detection fails
devops config --show                    # display current configuration
devops config --reset                   # remove all configuration
devops config --refresh-cache           # force re-fetch of iteration and area on next create
```

When `--org` or `--pat` is provided, the CLI automatically fetches your display name and email from Azure DevOps. The email is used to resolve `--assigned-to me`.

| Option | Alias | Description |
|---|---|---|
| `--org` | `-o` | Azure DevOps organization URL |
| `--pat` | `-p` | Personal Access Token |
| `--project` | `-P` | Default project (used when `--project` is omitted from other commands) |
| `--team` | `-T` | Default team for resolving the active iteration and area path (defaults to `{Project} Team`) |
| `--email` | `-e` | Your email address, used to resolve `--assigned-to me`. Set manually if auto-detection fails |
| `--show` | | Display the current configuration (PAT masked) |
| `--reset` | | Remove all local configuration and cache |
| `--refresh-cache` | | Force re-fetch of iteration and area path on next `create` |

---

## Work Item Commands

### `get` — Get work item details

```powershell
devops get -i 1234
devops get -i 1234 -p AnotherProject
```

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Work item ID (required) |
| `--project` | `-p` | Project name (uses default if configured) |

---

### `mine` — List work items assigned to me

Shortcut for `list --assigned-to me`. The `ASSIGNED TO` column is omitted since all items belong to the current user.

```powershell
devops mine
devops mine -s Active
devops mine -t Bug
devops mine -s "In Progress" -t Task
devops mine -p 1234            # only children of work item 1234
```

| Option | Alias | Description |
|---|---|---|
| `--project` | `-P` | Project name (uses default if configured) |
| `--state` | `-s` | Filter by state |
| `--type` | `-t` | Filter by work item type |
| `--query` | `-q` | Additional WIQL WHERE clause |
| `--parent` | `-p` | Filter by parent work item ID |

---

### `list` — List work items

```powershell
devops list
devops list -s Active
devops list -t Bug -a me
devops list -P MyProject -s "In Progress" -t Task
devops list -p 1234            # only children of work item 1234
devops list -q "[System.IterationPath] UNDER 'MyProject\\Sprint 1'"
```

| Option | Alias | Description |
|---|---|---|
| `--project` | `-P` | Project name (uses default if configured) |
| `--state` | `-s` | Filter by state (e.g., `Active`, `Closed`, `Resolved`) |
| `--type` | `-t` | Filter by work item type (e.g., `Task`, `Bug`, `User Story`) |
| `--assigned-to` | `-a` | Filter by assignee. Use `me` for the current user |
| `--query` | `-q` | WIQL WHERE clause for advanced filtering |
| `--parent` | `-p` | Filter by parent work item ID |

---

### `create` — Create a work item

On creation, the active iteration and the team's default area path are resolved automatically. Use `--iteration` or `--area` to override. The resolved values are cached for the rest of the month.

```powershell
devops create -t "Fix login bug"
devops create -t "New auth endpoint" --type "User Story" -s Active -a me -P 2
devops create -t "Implement service" -e 6 -y Development -R 1234
devops create -t "Backlog item" -I "MyProject\Backlog"
devops create -t "Task with custom field" -f "Custom.SomeField=value" -f "Custom.Another=other"
devops create -t "Análise Técnica" -R 1234 --normalize   # title becomes "PBI 1234 - Análise Técnica"
```

| Option | Alias | Description |
|---|---|---|
| `--title` | `-t` | Work item title (required) |
| `--project` | `-p` | Project name (uses default if configured) |
| `--type` | | Work item type (default: `Task`) |
| `--state` | `-s` | Initial state (e.g., `New`, `Active`) |
| `--assigned-to` | `-a` | Assignee email or display name. Use `me` for the current user |
| `--description` | `-d` | Description |
| `--priority` | `-P` | Priority from 1 (highest) to 4 (lowest) |
| `--iteration` | `-I` | Iteration path. Defaults to the active sprint of the configured team |
| `--area` | `-A` | Area path. Defaults to the team's default area |
| `--estimate` | `-e` | Estimated work in hours (`Custom.EstimateWork`) |
| `--activity-type` | `-y` | Activity type (`Microsoft.VSTS.Common.Activity`), e.g., `Development`, `Testing`, `Design` |
| `--field` | `-f` | Custom field in `Key=Value` format. Repeatable for multiple fields |
| `--related-id` | `-R` | ID of the work item to relate to |
| `--relation-type` | `-r` | Relation type (default: `parent`). See table below |
| `--normalize` | | Prefix the title with the parent type and ID (e.g., `PBI 1234 - <title>`). Requires a parent relation |

**Relation types:**

| Value | Description |
|---|---|
| `parent` | The new item is a child of the specified item |
| `child` | The new item is a parent of the specified item |
| `related` | Generic relation |
| `blocks` | The new item blocks the specified item |
| `blocked-by` | The new item is blocked by the specified item |

---

### `update` — Update a work item

Only fields explicitly provided are updated. No field has a default that causes unintended writes.

```powershell
devops update -i 1234 -s Closed
devops update -i 1234 -t "New title" -P 1
devops update -i 1234 -a me -e 8 -y Testing
devops update -i 1234 -c "Dependency resolved."
devops update -i 1234 -I "MyProject\Sprint 4"
devops update -i 1234 -R 5678 --relation-type blocks
devops update -i 1234 -f "Custom.ReviewEstimate=2"
```

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Work item ID (required) |
| `--project` | `-p` | Project name (uses default if configured) |
| `--title` | `-t` | New title |
| `--state` | `-s` | New state |
| `--assigned-to` | `-a` | New assignee. Use `me` for the current user |
| `--description` | `-d` | New description |
| `--priority` | `-P` | New priority (1–4) |
| `--iteration` | `-I` | New iteration path |
| `--area` | `-A` | New area path |
| `--estimate` | `-e` | Estimated work in hours (`Custom.EstimateWork`) |
| `--activity-type` | `-y` | Activity type (e.g., `Development`, `Testing`, `Design`) |
| `--field` | `-f` | Custom field in `Key=Value` format. Repeatable |
| `--comment` | `-c` | Add a comment to the work item history |
| `--related-id` | `-R` | ID of the work item to relate to |
| `--relation-type` | `-r` | Relation type (default: `related`). See `create` for valid values |

---

### `normalize` — Normalize task titles with a parent prefix

Renames Tasks whose title follows the `[Role] Description` pattern (e.g., created by third parties) to `<PARENT_TYPE> <PARENT_ID> - [Role] Description`. The parent type is abbreviated: `Product Backlog Item` becomes `PBI`; any other type is uppercased (e.g., `Bug` -> `BUG`). Tasks already normalized or without a parent are skipped. By default only Tasks assigned to the current user are processed.

```powershell
devops normalize                      # normalize my tasks
devops normalize --dry-run            # preview without applying
devops normalize -s Active            # only tasks in a given state
devops normalize -p 1234              # only children of work item 1234
devops normalize -a any               # all tasks, regardless of assignee
```

| Option | Alias | Description |
|---|---|---|
| `--project` | `-P` | Project name (uses default if configured) |
| `--state` | `-s` | Filter by state |
| `--assigned-to` | `-a` | Filter by assignee. Use `me` (default) or `any` for all |
| `--parent` | `-p` | Restrict to children of a specific parent ID |
| `--dry-run` | `-n` | Preview changes without applying them |

---

### `comment` — Add a comment to a work item

```powershell
devops comment -i 1234 "Dependency resolved, ready for review."
devops comment -i 1234 "Blocked by infra team." -p AnotherProject
```

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Work item ID (required) |
| `message` | | Comment text (positional, required) |
| `--project` | `-p` | Project name (uses default if configured) |

---

### `state` — Change the state of one or more work items

Shortcut for `update --state`. Fetches the current state first and shows the full transition in the output. Multiple IDs are processed in parallel.

```powershell
devops state -i 1234 -s "In Progress"
devops state -i 1234 5678 9012 -s "Closed"
devops state -i 1234 -s "Done" -p AnotherProject
```

Output: `Work item #1234: To Do -> In Progress`

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Work item ID (required). Multiple IDs space-separated: `-i 1 2 3` |
| `--state` | `-s` | Target state (required, e.g. `In Progress`, `Closed`, `Done`) |
| `--project` | `-p` | Project name (uses default if configured) |

---

### `open` — Open a work item in the browser

```powershell
devops open -i 1234
devops open -i 1234 -p AnotherProject
```

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Work item ID (required) |
| `--project` | `-p` | Project name (uses default if configured) |

---

## Pipeline Commands

### `pipelines` — List available pipelines

```powershell
devops pipelines
devops pipelines -n "deploy"
devops pipelines -p AnotherProject
```

| Option | Alias | Description |
|---|---|---|
| `--project` | `-p` | Project name (uses default if configured) |
| `--name` | `-n` | Filter by pipeline name (partial match) |

---

## PAT Permissions

Go to **User Settings → Personal Access Tokens → New Token** and grant the following scopes:

| Scope | Permission | Used by |
|---|---|---|
| Work Items | Read & Write | All work item commands |
| Build | Read | `pipelines` |