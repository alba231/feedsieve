# Feed Aggregator — Architecture & Implementation Plan

> Solution architecture and phased delivery plan for a cross-platform feed aggregator (RSS, HTML watcher, Telegram, Discord, Email) built on .NET MAUI + Firebase.

---

## 1. Decisions Locked In

| Area | Decision | Rationale |
|---|---|---|
| Client | .NET MAUI (iOS, Android, Windows, macOS) | Single C# codebase, matches your stack |
| Future web/extension | Angular SPA + TS port of core domain | Decoupled from MAUI core |
| Backend (Phase 1–2) | Firebase: Auth (Google) + Firestore | Generous free tier, real-time sync |
| Backend (Phase 3+) | Optional Azure Functions for scheduled fetch / push | Free tier; only if push notifications are added |
| Storage model | **Hybrid metadata** — cloud stores metadata + filter result + content fingerprint; full body cached locally per device | Cost-efficient, copyright-safe, link-rot resilient |
| Filter semantics | Filters mark items (include / exclude / priority); excluded items are **hidden by default but viewable on demand** | Matches your stated UX |
| Provider extensibility | **Compile-time** plugins via DI; each provider is its own project | Type-safe, App Store-compliant on iOS |
| Auth | MAUI `WebAuthenticator` → Google OAuth (PKCE) → Firebase `signInWithIdp` | Standard, no extra hosting |
| IaC | Vanilla Terraform, single Firebase project, dev/prod via tfvars | Terragrunt not justified at this scale |
| Anon → Authed migration | New user: copy local to cloud. Existing user: merge with confirmation dialog | Per your direction |

---

## 2. High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                         MAUI App (per device)                        │
│                                                                      │
│  ┌────────────────┐    ┌────────────────┐    ┌─────────────────┐     │
│  │   Pages /      │───▶│   ViewModels   │───▶│  App Services   │     │
│  │   XAML Views   │    │                │    │                 │     │
│  └────────────────┘    └────────────────┘    └────────┬────────┘     │
│                                                       │              │
│              ┌────────────────────────────────────────┴─────┐        │
│              ▼                                              ▼        │
│   ┌──────────────────┐                          ┌──────────────────┐ │
│   │ Provider Registry│                          │   Sync Engine    │ │
│   │ (DI, compile-    │                          │ (online/offline, │ │
│   │  time plugins)   │                          │  pending queue)  │ │
│   └────────┬─────────┘                          └────────┬─────────┘ │
│            │                                             │           │
│            ▼                                             ▼           │
│   ┌──────────────────┐                          ┌──────────────────┐ │
│   │ Filter Pipeline  │                          │  Local Storage   │ │
│   │ (include/exclude │                          │  (SQLite cache)  │ │
│   │  /priority)      │                          │                  │ │
│   └──────────────────┘                          └──────────────────┘ │
└──────────────────────────────────────────┬───────────────────────────┘
                                           │ HTTPS (REST)
                                           ▼
┌──────────────────────────────────────────────────────────────────────┐
│                              Firebase                                │
│   ┌──────────────────┐         ┌──────────────────────────────────┐  │
│   │  Identity Plat.  │         │       Firestore                  │  │
│   │  (Google OAuth)  │         │  /users/{uid}/sources            │  │
│   │                  │         │  /users/{uid}/items   (metadata) │  │
│   │                  │         │  /users/{uid}/rules              │  │
│   └──────────────────┘         └──────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

**Key flows:**

- **Anonymous:** App writes to local SQLite only. No network beyond fetching from external sources.
- **Authenticated:** App writes locally first, then syncs metadata to Firestore. Other devices pull deltas.
- **Race condition safety:** Item IDs are deterministic SHA-256 hashes of `(sourceId, canonicalKey)`. Concurrent writes from two devices use Firestore conditional `create` — loser silently no-ops.

---

## 3. Solution Layout

```
FeedAggregator.sln
├── src/
│   ├── FeedAggregator.Core/                    # Pure .NET, no MAUI deps
│   │   ├── Domain/
│   │   │   ├── Entities/                       # Source, Item, Rule, FilterResult
│   │   │   ├── Identity/                       # ItemIdGenerator (SHA-256)
│   │   │   └── Filters/                        # Rule matcher
│   │   ├── Providers/
│   │   │   ├── ISourceProvider.cs
│   │   │   ├── SourceConfiguration.cs          # abstract record
│   │   │   ├── FetchResult.cs
│   │   │   └── ProviderRegistry.cs
│   │   ├── Sync/
│   │   │   ├── ISyncEngine.cs
│   │   │   └── PendingMutation.cs
│   │   └── Abstractions/                       # IClock, IHttpClientFactory wrappers
│   │
│   ├── FeedAggregator.Providers.Rss/           # Reference implementation (Phase 1)
│   ├── FeedAggregator.Providers.HtmlWatcher/   # Phase 3
│   ├── FeedAggregator.Providers.Telegram/      # Phase 3
│   ├── FeedAggregator.Providers.Discord/       # Phase 3
│   ├── FeedAggregator.Providers.Email/         # Phase 3
│   │
│   ├── FeedAggregator.Infrastructure.Firebase/ # REST client, repos, auth
│   │   ├── FirebaseRestClient.cs
│   │   ├── FirestoreRepository.cs
│   │   ├── FirebaseAuthService.cs
│   │   └── TokenRefreshHandler.cs              # DelegatingHandler
│   │
│   ├── FeedAggregator.Infrastructure.Storage/  # SQLite cache
│   │   ├── LocalDbContext.cs                   # EF Core + SQLite
│   │   └── Migrations/
│   │
│   └── FeedAggregator.App/                     # MAUI head
│       ├── MauiProgram.cs                      # DI registration
│       ├── Pages/
│       ├── ViewModels/                         # CommunityToolkit.Mvvm
│       └── Views/Providers/
│           ├── Rss/AddRssSourcePage.xaml
│           └── HtmlWatcher/AddHtmlWatcherPage.xaml
│
├── tests/
│   ├── FeedAggregator.Core.Tests/
│   ├── FeedAggregator.Providers.Rss.Tests/
│   └── FeedAggregator.Infrastructure.Firebase.Tests/
│
├── infra/
│   └── firebase/
│       ├── main.tf
│       ├── variables.tf
│       ├── outputs.tf
│       └── environments/
│           ├── dev.tfvars
│           └── prod.tfvars
│
└── docs/
    └── architecture.md  (this file)
```

**Why this split:**
- `Core` has no MAUI dependency → reusable in a future Angular world (via gRPC bridge or TS port).
- Each provider is its own project → adding one is mechanical, deleting one is safe.
- Infrastructure projects are swappable (Firebase today, something else tomorrow).

---

## 4. Provider Framework Contracts

```csharp
namespace FeedAggregator.Core.Providers;

/// <summary>
/// Contract every external system provider must implement.
/// Registered via DI at app startup; discovered through <see cref="ProviderRegistry"/>.
/// </summary>
public interface ISourceProvider
{
    /// <summary>Stable identifier, e.g. "rss", "html-watcher", "telegram".</summary>
    string ProviderId { get; }

    /// <summary>User-facing name shown in the "Add source" wizard.</summary>
    string DisplayName { get; }

    SourceProviderCapabilities Capabilities { get; }

    /// <summary>The MAUI ContentPage type used to configure a new source of this type.</summary>
    Type ConfigurationPageType { get; }

    /// <summary>Concrete config record type (e.g. <c>RssSourceConfiguration</c>).</summary>
    Type ConfigurationType { get; }

    /// <summary>
    /// Fetch new items. Implementations MUST honor <paramref name="ct"/> and use
    /// <paramref name="context"/>.LastFingerprint to short-circuit when nothing changed.
    /// </summary>
    Task<FetchResult> FetchAsync(
        SourceConfiguration config,
        FetchContext context,
        CancellationToken ct);
}

[Flags]
public enum SourceProviderCapabilities
{
    None                   = 0,
    SupportsScheduledFetch = 1 << 0,
    SupportsPushIngest     = 1 << 1, // e.g. Discord webhooks, Telegram bot updates
    RequiresAuth           = 1 << 2,
    SupportsBodyFetch      = 1 << 3, // can return full body, not just metadata
}
```

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RssSourceConfiguration),         "rss")]
[JsonDerivedType(typeof(HtmlWatcherSourceConfiguration), "html-watcher")]
[JsonDerivedType(typeof(TelegramSourceConfiguration),    "telegram")]
public abstract record SourceConfiguration
{
    public required string Id           { get; init; }
    public required string ProviderId   { get; init; }
    public required string DisplayName  { get; init; }
    public TimeSpan?       RefreshInterval { get; init; }
}

public sealed record RssSourceConfiguration : SourceConfiguration
{
    public required Uri FeedUrl { get; init; }
    public string? UserAgent    { get; init; }
}

public sealed record HtmlWatcherSourceConfiguration : SourceConfiguration
{
    public required Uri    PageUrl     { get; init; }
    public required string CssSelector { get; init; }
    public bool IgnoreWhitespace       { get; init; } = true;
    public bool IgnoreCase             { get; init; } = false;
}
```

```csharp
public sealed record FetchContext
{
    public DateTimeOffset? LastFetchedAt   { get; init; }
    public string?         LastFingerprint { get; init; }
    public string?         LastETag        { get; init; }   // for HTTP conditional GET
}

public sealed record FetchResult
{
    public required IReadOnlyList<RawItem> Items              { get; init; }
    public required string                 ContentFingerprint { get; init; }
    public string?                         ETag               { get; init; }
    public DateTimeOffset                  FetchedAt          { get; init; } = DateTimeOffset.UtcNow;
    public bool                            NotModified        { get; init; } // 304-equivalent
}

public sealed record RawItem
{
    /// <summary>Stable canonical key within the source (e.g., RSS guid, URL).</summary>
    public required string         CanonicalKey { get; init; }
    public required string         Title        { get; init; }
    public Uri?                    Url          { get; init; }
    public string?                 Summary      { get; init; }
    public DateTimeOffset?         PublishedAt  { get; init; }
    public IReadOnlyList<string>   Tags         { get; init; } = [];
}
```

**Deterministic ID generation** (used by sync to dedup across devices):

```csharp
public static class ItemIdGenerator
{
    public static string Compute(string sourceId, string canonicalKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalKey);

        var input = $"{sourceId}|{canonicalKey}";
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(input), hash);
        return Convert.ToHexString(hash[..12]).ToLowerInvariant(); // 24 chars, plenty
    }
}
```

**Provider registration** (in `MauiProgram.cs`):

```csharp
builder.Services
    .AddSingleton<ISourceProvider, RssSourceProvider>()
    .AddSingleton<ISourceProvider, HtmlWatcherSourceProvider>()
    // .AddSingleton<ISourceProvider, TelegramSourceProvider>()  // Phase 3
    .AddSingleton<ProviderRegistry>();
```

```csharp
public sealed class ProviderRegistry
{
    private readonly Dictionary<string, ISourceProvider> _byId;

    public ProviderRegistry(IEnumerable<ISourceProvider> providers)
        => _byId = providers.ToDictionary(p => p.ProviderId, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ISourceProvider> All => _byId.Values;
    public ISourceProvider Get(string providerId) => _byId[providerId];
    public bool TryGet(string providerId, out ISourceProvider? provider)
        => _byId.TryGetValue(providerId, out provider);
}
```

---

## 5. Data Model

### 5.1 Firestore Schema (cloud, authenticated users only)

```
/users/{uid}
   email           : string
   displayName     : string?
   createdAt       : timestamp
   settings        : map { defaultRefreshInterval, theme, ... }

/users/{uid}/sources/{sourceId}
   providerId      : string                         // "rss" | "html-watcher" | ...
   displayName     : string
   configJson      : string                         // serialized SourceConfiguration
   refreshInterval : duration | null
   contentFingerprint : string?                     // last-known fingerprint
   lastFetchedAt   : timestamp?
   lastETag        : string?
   createdAt       : timestamp
   updatedAt       : timestamp

/users/{uid}/items/{itemId}                         // itemId = ItemIdGenerator.Compute(...)
   sourceId        : string
   title           : string
   url             : string?
   summary         : string?                        // short, ≤ 500 chars
   publishedAt     : timestamp?
   fetchedAt       : timestamp
   filterResult    : "include" | "exclude" | "none" // computed at fetch time
   priority        : int                            // 0=normal, higher = boosted
   matchedRuleIds  : string[]
   isRead          : bool
   isPinned        : bool
   tags            : string[]
   // NOTE: no body field — body lives only in local SQLite cache

/users/{uid}/rules/{ruleId}
   type            : "include" | "exclude" | "priority"
   scope           : "global" | "source"
   sourceId        : string?                        // when scope = "source"
   pattern         : string
   isRegex         : bool
   fields          : string[]                       // ["title","summary","url","tags"]
   priorityDelta   : int                            // when type = "priority"
   order           : int                            // deterministic application order
   createdAt       : timestamp
```

**Why `configJson` as a string blob:** Firestore doesn't enforce schema; storing the typed config as a JSON string with `$type` discriminator keeps the cloud schema stable as you add providers without writing a Firestore migration each time.

### 5.2 Security Rules (sketch)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;

      match /{document=**} {
        allow read, write: if request.auth != null && request.auth.uid == userId;
      }
    }
  }
}
```

Hardening for later: validate `providerId` against an enum, cap `pattern` length to prevent rule abuse, etc.

### 5.3 Local SQLite Schema (per device)

```sql
-- Mirrors a subset of Firestore; populated by sync engine
CREATE TABLE Sources (
    Id                 TEXT PRIMARY KEY,
    ProviderId         TEXT NOT NULL,
    DisplayName        TEXT NOT NULL,
    ConfigJson         TEXT NOT NULL,
    RefreshInterval    INTEGER NULL,        -- ticks
    ContentFingerprint TEXT NULL,
    LastFetchedAt      INTEGER NULL,
    LastETag           TEXT NULL,
    SyncState          INTEGER NOT NULL DEFAULT 0  -- 0=Synced, 1=PendingUpload, 2=PendingDelete
);

CREATE TABLE Items (
    Id             TEXT PRIMARY KEY,
    SourceId       TEXT NOT NULL,
    Title          TEXT NOT NULL,
    Url            TEXT NULL,
    Summary        TEXT NULL,
    PublishedAt    INTEGER NULL,
    FetchedAt      INTEGER NOT NULL,
    FilterResult   TEXT NOT NULL,
    Priority       INTEGER NOT NULL DEFAULT 0,
    IsRead         INTEGER NOT NULL DEFAULT 0,
    IsPinned       INTEGER NOT NULL DEFAULT 0,
    SyncState      INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (SourceId) REFERENCES Sources(Id) ON DELETE CASCADE
);
CREATE INDEX IX_Items_SourceId_FetchedAt ON Items(SourceId, FetchedAt DESC);

CREATE TABLE Rules (
    Id              TEXT PRIMARY KEY,
    Type            TEXT NOT NULL,
    Scope           TEXT NOT NULL,
    SourceId        TEXT NULL,
    Pattern         TEXT NOT NULL,
    IsRegex         INTEGER NOT NULL,
    FieldsJson      TEXT NOT NULL,
    PriorityDelta   INTEGER NOT NULL DEFAULT 0,
    "Order"         INTEGER NOT NULL,
    SyncState       INTEGER NOT NULL DEFAULT 0
);

-- Local-only: full body cache (NEVER synced to Firestore)
CREATE TABLE ItemBodies (
    ItemId      TEXT PRIMARY KEY,
    Html        TEXT NOT NULL,
    FetchedAt   INTEGER NOT NULL,
    FOREIGN KEY (ItemId) REFERENCES Items(Id) ON DELETE CASCADE
);

-- Pending mutations queue for offline support
CREATE TABLE PendingMutations (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    EntityType      TEXT NOT NULL,    -- "source" | "item" | "rule"
    EntityId        TEXT NOT NULL,
    Operation       TEXT NOT NULL,    -- "create" | "update" | "delete"
    PayloadJson     TEXT NOT NULL,
    AttemptCount    INTEGER NOT NULL DEFAULT 0,
    LastAttemptAt   INTEGER NULL,
    CreatedAt       INTEGER NOT NULL
);
```

---

## 6. Authentication & Token Refresh

```csharp
public sealed class FirebaseAuthService(
    HttpClient http,
    ISecureStorage secureStorage,
    string firebaseApiKey,
    string googleClientId,
    string redirectUri,
    ILogger<FirebaseAuthService> logger)
{
    public async Task<AuthResult> SignInWithGoogleAsync(CancellationToken ct)
    {
        // 1. Open system browser via WebAuthenticator (PKCE)
        var authResult = await WebAuthenticator.Default.AuthenticateAsync(
            new WebAuthenticatorOptions
            {
                Url = BuildGoogleAuthUrl(googleClientId, redirectUri, out var verifier),
                CallbackUrl = new Uri(redirectUri),
                PrefersEphemeralWebBrowserSession = false,
            });

        var googleIdToken = authResult.Properties["id_token"];

        // 2. Exchange Google ID token for Firebase tokens
        var resp = await http.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={firebaseApiKey}",
            new
            {
                postBody     = $"id_token={googleIdToken}&providerId=google.com",
                requestUri   = redirectUri,
                returnIdpCredential = true,
                returnSecureToken   = true,
            }, ct).ConfigureAwait(false);

        resp.EnsureSuccessStatusCode();
        var firebaseAuth = await resp.Content.ReadFromJsonAsync<FirebaseAuthResponse>(ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty Firebase auth response");

        await secureStorage.SetAsync("fb_refresh_token", firebaseAuth.RefreshToken)
            .ConfigureAwait(false);

        return new AuthResult(firebaseAuth.LocalId, firebaseAuth.IdToken,
                              firebaseAuth.RefreshToken,
                              DateTimeOffset.UtcNow.AddSeconds(firebaseAuth.ExpiresIn));
    }
}
```

A `DelegatingHandler` injects the current `IdToken` and refreshes on 401:

```csharp
public sealed class TokenRefreshHandler(IAuthTokenStore store, ITokenRefresher refresher)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await store.GetIdTokenAsync(ct).ConfigureAwait(false);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

        response.Dispose();
        var newToken = await refresher.RefreshAsync(ct).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }
}
```

---

## 7. Filter Pipeline

Rules are evaluated **in deterministic order** so the same item produces the same result on every device:

```csharp
public sealed class FilterPipeline(IReadOnlyList<Rule> orderedRules)
{
    public FilterEvaluation Evaluate(RawItem item, string sourceId)
    {
        var matched = new List<string>();
        FilterResult result = FilterResult.None;
        int priority = 0;

        foreach (var rule in orderedRules)
        {
            if (rule.Scope == RuleScope.Source && rule.SourceId != sourceId) continue;
            if (!rule.Matches(item)) continue;

            matched.Add(rule.Id);

            switch (rule.Type)
            {
                case RuleType.Exclude:
                    result = FilterResult.Exclude; // first exclude wins; later includes do NOT override
                    return new FilterEvaluation(result, priority, matched);
                case RuleType.Include when result == FilterResult.None:
                    result = FilterResult.Include;
                    break;
                case RuleType.Priority:
                    priority += rule.PriorityDelta;
                    break;
            }
        }

        return new FilterEvaluation(result, priority, matched);
    }
}
```

UX note: excluded items are kept in storage and filtered out of the default view. A toggle (`Show excluded`) reveals them with a strikethrough or muted style. Per your spec.

---

## 8. Terraform Layout (Phase 1)

```hcl
# infra/firebase/main.tf
terraform {
  required_version = ">= 1.7.0"

  required_providers {
    google      = { source = "hashicorp/google",      version = "~> 5.0" }
    google-beta = { source = "hashicorp/google-beta", version = "~> 5.0" }
  }

  backend "gcs" {
    bucket = "feed-aggregator-tfstate"
    prefix = "firebase"
    # workspace = dev / prod (set via CLI or CI)
  }
}

provider "google"      { project = var.project_id  region = var.region }
provider "google-beta" { project = var.project_id  region = var.region  user_project_override = true }

resource "google_project_service" "required" {
  for_each = toset([
    "firebase.googleapis.com",
    "identitytoolkit.googleapis.com",
    "firestore.googleapis.com",
    "cloudresourcemanager.googleapis.com",
  ])
  service            = each.value
  disable_on_destroy = false
}

resource "google_firebase_project" "this" {
  provider = google-beta
  project  = var.project_id
  depends_on = [google_project_service.required]
}

resource "google_firestore_database" "default" {
  provider                    = google-beta
  project                     = var.project_id
  name                        = "(default)"
  location_id                 = var.firestore_location  # e.g. "eur3"
  type                        = "FIRESTORE_NATIVE"
  concurrency_mode            = "OPTIMISTIC"
  app_engine_integration_mode = "DISABLED"
  depends_on                  = [google_firebase_project.this]
}

resource "google_identity_platform_config" "auth" {
  provider = google-beta
  project  = var.project_id
  sign_in {
    allow_duplicate_emails = false
    email { enabled = false  password_required = false }
  }
  depends_on = [google_firebase_project.this]
}

resource "google_identity_platform_default_supported_idp_config" "google" {
  provider      = google-beta
  project       = var.project_id
  idp_id        = "google.com"
  client_id     = var.google_oauth_client_id
  client_secret = var.google_oauth_client_secret
  enabled       = true
  depends_on    = [google_identity_platform_config.auth]
}
```

```hcl
# infra/firebase/variables.tf
variable "project_id"                 { type = string }
variable "region"                     { type = string  default = "europe-west1" }
variable "firestore_location"         { type = string  default = "eur3" }
variable "google_oauth_client_id"     { type = string }
variable "google_oauth_client_secret" { type = string  sensitive = true }
```

```hcl
# infra/firebase/environments/dev.tfvars
project_id = "feed-aggregator-dev"
```

Firestore security rules are deployed separately via the Firebase CLI (`firebase deploy --only firestore:rules`) — this is the pragmatic path; the Terraform `google_firebaserules_*` resources are awkward and have historically been buggy.

---

## 9. Phase 1 — MVP Stories

> **Goal:** Single-user, single-device, anonymous, RSS only. Local storage. No cloud.
> **Estimated effort:** 6–8 weeks solo, evenings/weekends.

### Epic 1 — Foundation

**Story 1.1 — Solution scaffolding**
- Create solution with project structure from §3.
- Add `Directory.Packages.props` for centralized NuGet versioning.
- Configure `editorconfig` and analyzer ruleset (Roslynator + Microsoft.CodeAnalysis.NetAnalyzers).
- **Acceptance:** Solution builds; `dotnet test` runs (empty); MAUI app launches a blank shell on Windows + Android emulator.

**Story 1.2 — Terraform skeleton**
- Implement `infra/firebase` from §8.
- Bootstrap GCS state bucket (manual, one-time).
- Document `terraform init`/`apply` flow in `infra/README.md`.
- **Acceptance:** `terraform plan -var-file=environments/dev.tfvars` produces clean plan; `apply` creates Firebase project successfully. *(Can be deferred until Phase 2 if anonymous-only MVP is the goal.)*

**Story 1.3 — CI pipeline**
- GitHub Actions workflow: build, test, MAUI Windows + Android build artifacts.
- Separate workflow for `terraform plan` on PRs touching `infra/**`.
- **Acceptance:** PR check is green; failing test blocks merge.

**Story 1.4 — App bootstrap**
- `MauiProgram.cs`: register logging (`Microsoft.Extensions.Logging`), configuration (`Microsoft.Extensions.Configuration`), DI.
- App-wide `IClock`, `IHttpClientFactory` (already there), `ISecureStorage` wrappers.
- Global error handler + structured logging to file via `Serilog.Sinks.File`.
- **Acceptance:** Crashes are logged to a file accessible via in-app diagnostics page; logs include correlation ID per app session.

### Epic 2 — Provider Framework

**Story 2.1 — Provider contracts**
- Implement `ISourceProvider`, `SourceConfiguration`, `FetchContext`, `FetchResult`, `RawItem` from §4.
- Implement `ProviderRegistry`.
- Unit tests for registry lookup, polymorphic JSON serialization round-trip.
- **Acceptance:** Adding a new provider is a single DI registration line; serializing a `RssSourceConfiguration` and deserializing as `SourceConfiguration` returns the original concrete type.

**Story 2.2 — Per-provider configuration UI navigation**
- "Add source" page lists all registered providers (`ProviderRegistry.All`).
- Selecting one navigates to the provider's `ConfigurationPageType` via Shell routing.
- Each config page returns a typed `SourceConfiguration` to the caller.
- **Acceptance:** With only the RSS provider registered, the picker shows one entry; with two registered, two entries appear with no MAUI app code changes.

**Story 2.3 — Deterministic ID generation**
- Implement `ItemIdGenerator.Compute`.
- Property-based tests: stability across runs, collision rate analysis on synthetic dataset.
- **Acceptance:** Same `(sourceId, canonicalKey)` always produces same ID; 1M synthetic inputs yield zero collisions in test.

### Epic 3 — RSS Provider (reference)

**Story 3.1 — RSS fetcher**
- `RssSourceProvider.FetchAsync` using `IHttpClientFactory`-named client.
- Send `If-None-Match` (ETag) and `If-Modified-Since`; return `FetchResult { NotModified = true }` on 304.
- Polly retry policy: 3 attempts with exponential backoff; surface final failure as `FetchException`.
- Honor `CancellationToken`.
- **Acceptance:** Given a fixture HTTP server returning 304, fetcher returns `NotModified=true` without parsing; given a 200 with a feed, fetcher parses and returns items.

**Story 3.2 — RSS/Atom parser**
- Use `System.ServiceModel.Syndication.SyndicationFeed`.
- Map both RSS 2.0 and Atom to `RawItem`.
- `CanonicalKey` = `<guid isPermaLink="...">` if present, else `<link>`, else hash of (title + pubDate).
- Compute `ContentFingerprint` = SHA-256 of concatenated `CanonicalKey`s, sorted.
- **Acceptance:** Tested against fixtures of 5 real feeds (BBC, Hacker News, GitHub releases, a blog, a malformed feed); no exceptions; expected item counts.

**Story 3.3 — RSS configuration page (MAUI)**
- `AddRssSourcePage.xaml` with: feed URL entry, display name entry, refresh interval picker.
- "Test" button: fetches the feed, shows item count + first item title, no save yet.
- "Save" button: returns `RssSourceConfiguration` to caller, navigates back.
- Input validation: URL well-formed, scheme is http/https.
- **Acceptance:** Manual UX walkthrough adding `https://news.ycombinator.com/rss` results in a saved source; invalid input shows inline error.

### Epic 4 — Local Storage

**Story 4.1 — SQLite schema + migrations**
- EF Core + `Microsoft.EntityFrameworkCore.Sqlite`.
- Migrations as code; auto-apply on app startup.
- DB file in `FileSystem.AppDataDirectory`.
- **Acceptance:** Fresh install creates schema; subsequent launch detects no pending migrations.

**Story 4.2 — Repository layer**
- `ISourceRepository`, `IItemRepository`, `IRuleRepository` with async CRUD.
- All writes set `SyncState = PendingUpload` (will matter in Phase 2; harmless now).
- **Acceptance:** Round-trip integration tests against in-memory SQLite pass.

**Story 4.3 — Item body cache**
- On item open, fetch body URL → store in `ItemBodies`.
- Cache eviction: LRU, max 200 MB or 30 days, configurable.
- **Acceptance:** Opening same item twice triggers one HTTP fetch; old entries are evicted under pressure.

### Epic 5 — Filter Pipeline

**Story 5.1 — Rule entity + matcher**
- `Rule` entity per §5.3.
- Matching engine: plain text (case-insensitive contains) and regex (timeout via `Regex.MatchTimeout = 1s`).
- `FilterPipeline` per §7.
- **Acceptance:** Unit tests covering: exclude wins over include, priority sums, source-scoped rule ignored on other source, malicious regex doesn't hang.

**Story 5.2 — Rule management UI**
- List/add/edit/delete rules; rule editor supports type, scope, fields, regex toggle.
- "Test rule" against a sample text.
- **Acceptance:** UX walkthrough creates an exclude rule "advertisement" matching title; an item containing "advertisement" appears with strikethrough in default view.

**Story 5.3 — Filter result display**
- Item list view: excluded items hidden by default; "Show excluded" toggle in header.
- Excluded items rendered with reduced opacity + tooltip showing matched rule name.
- Priority-boosted items pinned to top of list.
- **Acceptance:** Manual UX matches spec.

### Epic 6 — Anonymous UX

**Story 6.1 — App shell + navigation**
- MAUI Shell with: Sources tab, All Items tab, Rules tab, Settings tab.
- Empty states with onboarding hints.
- **Acceptance:** Fresh install presents an empty Sources tab with "Add your first source" CTA.

**Story 6.2 — Sources tab**
- List of configured sources with last-fetched-at, item count, last result indicator (success/error).
- Pull-to-refresh triggers fetch on all sources.
- Tap source → drill-in to that source's items.
- **Acceptance:** Adding 3 sources shows them all; pull-to-refresh updates timestamps.

**Story 6.3 — Item list**
- All Items + per-source views (same component, different filter).
- Sorted by `Priority DESC, FetchedAt DESC`.
- Card shows title, source name + favicon, age, summary preview.
- Read/unread visual distinction; pin toggle.
- **Acceptance:** 100 items render smoothly on mid-tier Android device (no jank on scroll).

**Story 6.4 — Item detail**
- Full body display (HTML rendered in MAUI `WebView` for RSS; phase 3 providers may differ).
- Body lazy-fetched on first open.
- "Open in browser" external link.
- **Acceptance:** Opening an item with no cached body fetches it; second open is offline-capable.

**Story 6.5 — Refresh strategy**
- On app open: refresh all sources whose `LastFetchedAt + RefreshInterval < Now`.
- Per-source refresh interval picker (15min / 1h / 6h / 24h / manual-only).
- Background fetch is **explicitly out of scope** for Phase 1 (will revisit in Phase 4).
- **Acceptance:** Setting source A to "manual only" prevents auto-refresh on app open; setting to 1h auto-refreshes after that window.

---

## 10. Phase 2 — Auth & Cloud Sync

> **Goal:** Sign in with Google, sync metadata to Firestore, multi-device.
> **Estimated effort:** 3–4 weeks solo.

**Epic 7 — Authentication**
- 7.1 — `WebAuthenticator` Google OAuth flow (incl. Android intent filter / iOS URL scheme / Windows registration).
- 7.2 — Firebase `signInWithIdp` exchange + `FirebaseAuthService`.
- 7.3 — Refresh token in `SecureStorage`; `TokenRefreshHandler` on `HttpClient`.
- 7.4 — Sign-out flow (clear secure storage, switch back to anonymous mode, keep local data).

**Epic 8 — Cloud Sync**
- 8.1 — `FirebaseRestClient` for Firestore REST API (CRUD + `runQuery`).
- 8.2 — `ISyncEngine`: drains `PendingMutations` queue, pulls remote changes since last cursor.
- 8.3 — Conditional create for items: REST `createDocument` returns 409 → silent no-op (race winner already wrote).
- 8.4 — Anonymous-to-authed migration UX:
  - On first sign-in, check Firestore for existing user doc.
  - **New user** → bulk-upload all local sources/items/rules to Firestore.
  - **Existing user** → confirmation dialog: "You have local data and cloud data. Merge / Keep cloud only / Keep local only / Cancel".
- 8.5 — Conflict resolution: last-write-wins via Firestore `updatedAt` server timestamp; user-toggleable fields (`isRead`, `isPinned`) use OR-merge instead of LWW (a read on any device stays read).

---

## 11. Phase 3 — Additional Providers

> **Goal:** Real multi-source value beyond RSS.
> **Estimated effort:** 1–2 weeks per provider.

**Epic 9 — HTML Watcher**
- Polls a URL, extracts content matched by CSS selector (HtmlAgilityPack), normalizes whitespace, hashes.
- New "item" emitted only when fingerprint changes; item title = `"{source} changed at {timestamp}"`.
- Diff display in item detail (use DiffPlex).
- *Open question:* selector picker UX — phase 3 ships with manual entry; visual selector in phase 4+.

**Epic 10 — Telegram (channel monitoring)**
- Use Bot API (not MTProto) — simpler, no app review, sufficient for public channels.
- User adds bot to channel, supplies channel ID. Provider polls `getUpdates` or sets webhook (latter requires Phase 4 server).
- Each Telegram message → `RawItem`.

**Epic 11 — Discord**
- Easiest path: user creates a Discord webhook → provides app a forwarding endpoint. Requires server (Phase 4).
- Phase 3 fallback: polling via bot token + read-message-history scope on a single channel.

**Epic 12 — Email**
- Pull-based via IMAP (`MailKit`) — works without server.
- Folder + sender filter as part of source config.
- Each email → `RawItem` with subject as title, body as summary.

---

## 12. Phase 4 — Server-side Capabilities (Future)

- **Azure Functions** scheduled fetcher: per authenticated user, hourly fetch of all sources marked `SupportsScheduledFetch`. Writes results to Firestore. Removes need for client to poll on app open.
- **Push notifications** via FCM when filter-matching new items arrive.
- **AI filter** (`IAiFilter`): pluggable, swappable provider (OpenAI / Anthropic / local). Two use cases:
  1. Semantic dedup (the same news from 3 sources collapses).
  2. Auto-classification ("is this likely advertisement?").
- **Angular web app** + **Chrome extension** sharing a TS port of `FeedAggregator.Core` domain types via OpenAPI/protobuf.
- **HTML watcher visual selector** (point-and-click element picker, probably in the Chrome extension).

---

## 13. Risks & Open Questions

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| 1 | MAUI Firebase ecosystem is thin — REST client is hand-rolled | High | Medium | Keep REST client small, behind `IFirebaseRestClient` interface. Allocate ~2 days for it. |
| 2 | MAUI macOS / Windows desktop targets less mature than mobile | Medium | Low | Focus Phase 1 on Windows + Android; iOS/macOS as Phase 2. |
| 3 | RSS feeds are wildly inconsistent (broken XML, weird encodings) | High | Low | Be defensive in parser; log + skip bad items rather than crash entire fetch. |
| 4 | Firestore free tier (50K reads/day) won't survive aggressive multi-device polling | Low for personal use | Medium if shared | Phase 4 Azure Functions reduce client polling; meanwhile cap polling to per-source interval. |
| 5 | Google OAuth client setup has different requirements per MAUI target (intent filter, URL scheme, AppxManifest) | Medium | Medium | Document each target setup explicitly in `docs/auth-setup.md`. |
| 6 | Anonymous-to-authed merge UX has subtle edge cases (same source URL added on both sides) | Medium | Low | Use deterministic source IDs based on `(providerId, normalized config hash)` so duplicates collide naturally. |
| 7 | iOS background fetch limitations make scheduled refresh on iOS unreliable without a server | High | Low (Phase 1 is foreground-only) | Defer to Phase 4 server-side fetch. |

**Open questions for you:**
1. **Firestore region** — `eur3` (Belgium + Netherlands), `nam5` (US multi-region), or specific single region? Affects latency from your typical user location.
2. **Item retention policy** — keep all items forever, or auto-archive older than N days? Affects Firestore storage cost long-term.
3. **MAUI target priorities** — Windows + Android first (likely fastest path), or iOS too from day one (App Store account, certs, longer iteration loop)?
4. **Push notifications timing** — Phase 4 (after providers) or earlier? Pulling this forward forces Azure Functions earlier.

---

## 14. Glossary

- **Source** — A configured external system instance (e.g., "Hacker News RSS").
- **Provider** — The plugin that knows how to talk to a class of source (e.g., RSS provider).
- **Item** — One unit of content from a source (one RSS entry, one Telegram message, one HTML diff).
- **Rule** — User-defined include / exclude / priority directive.
- **Fingerprint** — Hash of source content state used for change detection.
- **Canonical key** — Source-stable identifier for an item (RSS guid, message ID, etc.).
