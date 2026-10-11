# OIDC session safety in admin frontends

Use this when changing the shared OIDC/request adapter, account switching, renewal, logout, multiple tabs, or page restoration. The concrete example is Heimdall 5.5.2; preserve each project's client and state-management conventions. Protocol behavior belongs to [identity-integration](../../identity-integration/references/heimdall-multi-account.md).

## State and commit boundaries

- Keep one application session pointer containing the auth domain and a session version that changes when the application session is replaced or ended. Store credentials separately under the application's own key; do not share one SDK userStore across unrelated applications.
- Capture session version **and credentials version/token identity** when starting API calls or renewal. Renewal can change credentials without changing the selected account; checking only the account/session ID is insufficient for stale 401 cleanup.
- Isolate a candidate callback until state, nonce, PKCE, issuer/client, ID Token and any UserInfo subject checks complete. SDK methods may write userStore before returning; use the project's candidate adapter/store rather than retroactively undoing a partial commit.
- Commit a validated candidate under the session lock only if the captured source version still matches. Cancelled or failed switches leave the original valid session intact. A callback arriving after another login or logout is stale.
- Coordinate refresh and commit across same-origin tabs, using the project's supported lock (Heimdall uses Web Locks). Re-read shared state inside the lock. Reject old successful renewals as well as old failures; localStorage writes and storage events alone are not a mutual-exclusion protocol.
- Keep AT/RT out of URLs, and all tokens out of logs, Redux/DVA display state and retry queues. A standard `id_token_hint` sent to a trusted provider's authorization/End Session endpoint is a limited protocol exception; do not propagate it into generic return URLs, UI links or referrers. Expose only the identity and status the UI needs.

## Requests and renewal

The shared request layer acquires an active session, captures its versions, attaches its AT, and performs only bounded renewal/retry. A page should not independently fetch a token or implement its own 401 handler.

Before committing renewed credentials or clearing failed credentials, acquire the session lock and match both the captured session and captured credentials. A late request using AT1 must not clear a still-current account that already renewed to AT2. A request made as A must not replay a mutation under a newly selected B; if the captured account/session changed, return control to the caller instead of silently changing its identity.

Distinguish outcomes:

| Outcome | Session handling |
|---------|------------------|
| Network failure / status unavailable | Stop the current protected operation and keep the session; allow a deliberate retry |
| Confirmed inactive session / invalid credentials | Clear only the matching captured session and credentials |
| Old request/callback after a newer login or refresh | Discard the stale result without clearing or replacing newer state |
| Explicit logout | Stop renewal and close the application session; stay on a manual recovery/login surface rather than immediately entering SSO |

For Heimdall's built-in Console, check its Bearer `/api/account/session-status` contract and match active/issuer/clientId/sessionId/subjectId before protected work, page wake and renewal when the token is usable. Expired credentials first use the server's revocation-aware renewal path, then verify status. Third-party SPAs use their own backend and available protocol capabilities; they do not call this Console-only endpoint.

## Logout and page restoration

Capture the original session, auth domain and RT/AT before local cleanup. Ordinary logout revokes those captured credentials through the original Discovery Revoke endpoint; it does not call OP End Session. A remote network failure does not restore the local login, but must be reported as unconfirmed revocation. Explicit unified logout follows the provider's targeted sid/ID Token/state flow.

Use cross-tab invalidation and page wake/restoration checks to discard ended sessions. Recheck before a protected operation even if the page displays a cached user. Do not mark native BFCache behavior verified merely because a test dispatched a synthetic `pageshow` event.

## Validation

Test through the actual shared adapter/request integration where feasible:

- Successful switch A→B; cancellation, expired interaction and failed candidate validation preserve A.
- Logout or login B during A's in-flight callback/renewal/401; A cannot overwrite or clear B.
- Refresh AT1→AT2 followed by an AT1 401; AT2 remains usable.
- Two tabs renew or log out concurrently; locks and shared state preserve the same result.
- Network failure blocks protected work without deleting the session; explicit invalidation clears it.
- Normal logout keeps the OP account usable for later login; unified logout ends the selected sid and leaves a different account usable.
- Browser restore checks ended sessions, and direct management routes retain authorized tenant context.

Heimdall's `npm run test:auth`, account-session safety and hosted-login/MFA tests exercise actual adapters; use the scripts available in the target version. Run TypeScript checks and relevant browser flows for changes, and state whether native BFCache and real multi-tab behavior were observed.

Verified sources: [oidcClient.ts](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/fe/src/services/oidcClient.ts), [request.ts](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/fe/src/services/request.ts), [client migration](https://github.com/BenLampson/Asgard.Heimdall/blob/v5.5.2/docs/oidc-client-migration.md).
