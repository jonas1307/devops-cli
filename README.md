# devops-cli

CLI for interacting with Azure DevOps via REST API, without depending on the `az` CLI.

## Requirements

- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Authentication via **either** a Microsoft Entra ID account **or** an Azure DevOps Personal Access Token (see [Authentication](#authentication) below)

## Installation

Install as a [.NET Global Tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) from NuGet:

```powershell
dotnet tool install -g azure-devops-cli
```

Update or uninstall:

```powershell
dotnet tool update -g azure-devops-cli
dotnet tool uninstall -g azure-devops-cli
```

The command is invoked as `devops`.

### Build from source

```powershell
dotnet publish DevOps/DevOps.csproj -c Release -o ./publish
```

Add the `publish` directory to your `PATH`, or copy the executable to a directory already on your `PATH`.

## Configuration

Pick one authentication method:

```powershell
# Option A — Microsoft Entra ID (interactive browser sign-in)
devops config -o https://dev.azure.com/myorg
devops config --login

# Option B — Personal Access Token
devops config -o https://dev.azure.com/myorg -p <PAT>
```

Then the shared settings:

```powershell
devops config -P MyProject              # set default project
devops config -T "MyProject Team"       # set default team (for iteration and area resolution)
devops config -e your@email.com         # set email manually if auto-detection fails
devops config --show                    # display current configuration
devops config --logout                  # sign out of Entra ID (clear the cached token)
devops config --reset                   # remove all configuration and sign out
devops config --refresh-cache           # force re-fetch of iteration and area on next create
```

When you sign in (`--login`) or provide a `--pat`, the CLI automatically fetches your display name and email from Azure DevOps. The email is used to resolve `--assigned-to me`.

| Option | Alias | Description |
|---|---|---|
| `--org` | `-o` | Azure DevOps organization URL |
| `--login` | `-l` | Sign in interactively with Microsoft Entra ID |
| `--logout` | | Sign out and clear the cached Entra ID token |
| `--tenant` | | Entra ID tenant ID or domain to sign in against (defaults to your home tenant) |
| `--pat` | `-p` | Personal Access Token |
| `--project` | `-P` | Default project (used when `--project` is omitted from other commands) |
| `--team` | `-T` | Default team for resolving the active iteration and area path (defaults to `{Project} Team`) |
| `--email` | `-e` | Your email address, used to resolve `--assigned-to me`. Set manually if auto-detection fails |
| `--border` | | Table border style for list output: `minimal` (default), `square`, or `markdown` |
| `--show` | | Display the current configuration (auth mode and masked secret) |
| `--reset` | | Remove all local configuration and cache, and sign out |
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
| `--output` | `-o` | Output format: `json` or `csv`. Defaults to a detailed view |

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
| `--top` | `-n` | Maximum number of work items to fetch (default: 50) |
| `--output` | `-o` | Output format: `json` or `csv`. Defaults to a table |

---

### `list` — List work items

```powershell
devops list
devops list -s Active
devops list -t Bug -a me
devops list -P MyProject -s "In Progress" -t Task
devops list -p 1234            # only children of work item 1234
devops list -n 200             # fetch up to 200 items instead of the default 50
devops list -s Active -o json  # machine-readable output for scripting
devops list -q "[System.IterationPath] UNDER 'MyProject\\Sprint 1'"
```

Queries fetch up to `--top` items (default 50). When more match than were fetched, the footer says so — for example `Showing 50 of 312 work items - use --top to fetch more.` Items are retrieved in batches of 200 behind the scenes, which is the maximum the Azure DevOps batch endpoint accepts.

| Option | Alias | Description |
|---|---|---|
| `--project` | `-P` | Project name (uses default if configured) |
| `--state` | `-s` | Filter by state (e.g., `Active`, `Closed`, `Resolved`) |
| `--type` | `-t` | Filter by work item type (e.g., `Task`, `Bug`, `User Story`) |
| `--assigned-to` | `-a` | Filter by assignee. Use `me` for the current user |
| `--query` | `-q` | WIQL WHERE clause for advanced filtering |
| `--parent` | `-p` | Filter by parent work item ID |
| `--top` | `-n` | Maximum number of work items to fetch (default: 50) |
| `--output` | `-o` | Output format: `json` or `csv`. Defaults to a table |

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
| `--top` | | Maximum number of tasks to fetch (default: 200) |

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

### `runs` — List recent runs of a pipeline

Shows the most recent runs of a pipeline, newest first. Use the pipeline ID from `pipelines`.

```powershell
devops runs -i 42
devops runs -i 42 -t 25
devops runs -i 42 -p AnotherProject
```

Output columns: `ID`, `NAME`, `STATE` (e.g. `inProgress`, `completed`), `RESULT` (e.g. `succeeded`, `failed`; `-` while still running), `CREATED`.

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Pipeline (definition) ID (required). See `pipelines` |
| `--project` | `-p` | Project name (uses default if configured) |
| `--top` | `-t` | Number of most recent runs to show (default: 10) |

---

### `run` — Queue a new pipeline run

Triggers a new run of a pipeline. Without `--branch`, it runs the pipeline's default branch.

```powershell
devops run -i 42
devops run -i 42 -b main
devops run -i 42 -b refs/heads/release/1.0 -p AnotherProject
```

On success it prints the new run ID, its state, and a link to follow it in the browser.

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Pipeline (definition) ID (required). See `pipelines` |
| `--project` | `-p` | Project name (uses default if configured) |
| `--branch` | `-b` | Branch to run (`main` or `refs/heads/main`). Defaults to the pipeline's default branch |

---

## Pull Request Commands

Pull requests belong to a **repository**, specified with `--repo` (required for `pr-create`, optional filter for `pr-list`). `pr-get` and `pr-open` work by PR ID at the organization level, so they need neither project nor repo.

### `pr-list` — List pull requests

```powershell
devops pr-list
devops pr-list -r MyRepo -s active
devops pr-list -r MyRepo -t main
devops pr-list -s all -n 50
```

| Option | Alias | Description |
|---|---|---|
| `--project` | `-p` | Project name (uses default if configured) |
| `--repo` | `-r` | Repository name. If omitted, lists across all repos in the project |
| `--status` | `-s` | `active` (default), `completed`, `abandoned`, or `all` |
| `--target` | `-t` | Filter by target branch (e.g., `main`) |
| `--top` | `-n` | Maximum number of PRs to show (default: 25) |

---

### `pr-get` — Show pull request details

```powershell
devops pr-get -i 123
```

Shows status, source/target branches, author, reviewers with their votes, the web URL, and the description.

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Pull request ID (required) |

---

### `pr-create` — Create a pull request

```powershell
devops pr-create -r MyRepo -s feature/login -t main --title "Add login"
devops pr-create -r MyRepo -s feature/x -t main --title "WIP" -d "Details..." --draft
```

Branches accept either the short name (`main`) or the full ref (`refs/heads/main`).

| Option | Alias | Description |
|---|---|---|
| `--repo` | `-r` | Repository name (required) |
| `--source` | `-s` | Source branch (required) |
| `--target` | `-t` | Target branch (required) |
| `--title` | | Pull request title (required) |
| `--description` | `-d` | Pull request description |
| `--draft` | | Create as a draft |
| `--project` | `-p` | Project name (uses default if configured) |

---

### `pr-open` — Open a pull request in the browser

```powershell
devops pr-open -i 123
```

| Option | Alias | Description |
|---|---|---|
| `--id` | `-i` | Pull request ID (required) |

---

## Authentication

The CLI supports two authentication methods. Your choice is stored in `config.json` as the active auth mode and switching is just a matter of re-running `config`.

### Microsoft Entra ID (recommended)

```powershell
devops config -o https://dev.azure.com/myorg
devops config --login
```

Signs in through an interactive browser flow (MSAL), using the well-known public client of the Azure CLI — no app registration is required. The token is scoped to Azure DevOps (`499b84ac-1321-427f-aa17-267ca6975798/.default`) and cached securely (DPAPI on Windows, the keychain on macOS, an encrypted file on Linux), then refreshed silently. Your effective permissions are those your account already has in the organization.

> The Azure CLI (`az`) does **not** need to be installed. The sign-in is performed by MSAL inside the CLI; only the Azure CLI's public *client ID* is reused as an identifier. (This does require that the "Microsoft Azure CLI" application itself is allowed in your Entra tenant.)

If your account is a guest in another tenant or your organization enforces Conditional Access, pass the tenant explicitly:

```powershell
devops config --login --tenant contoso.onmicrosoft.com
```

Entra sign-in requires the organization policy *"Allow access via Microsoft Entra authentication"* to be enabled (on by default for Entra-backed organizations).

**Session lifetime.** Access tokens are refreshed silently, so you normally sign in once and stay authenticated for weeks — the token cache survives terminal restarts and reboots. A new interactive sign-in is only needed after long inactivity, a credential change, or when your organization's Conditional Access policy requires it. In those cases a regular command stops with a clear message asking you to run `devops config --login` again; the CLI never opens a browser unexpectedly during other commands (keeping it safe for scripts and CI).

### Personal Access Token

```powershell
devops config -o https://dev.azure.com/myorg -p <PAT>
```

Go to **User Settings → Personal Access Tokens → New Token** and grant the following scopes:

| Scope | Permission | Used by |
|---|---|---|
| Work Items | Read & Write | All work item commands |
| Build | Read | `pipelines` |

The PAT is stored encrypted (DPAPI) on Windows and in an owner-only file (`600`) on Linux/macOS.