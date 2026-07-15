---
name: asgard-admin-frontend
description: Use when building or refactoring Asgard or Heimdall management frontend pages, Umi Max admin routes, Ant Design Pro CRUD screens, DVA models, generated TsGen API clients, OIDC login wiring, tenant workspaces, or frontend calls to Asgard APIs.
---

# Asgard Admin Frontend

## Scope

Use this for browser-based Asgard admin consoles, especially Heimdall-style management pages. The default stack is Umi Max, React, TypeScript, Ant Design, Ant Design Pro Components, DVA models, and `oidc-client-ts`. API clients may use optional Asgard.TsGen output or another project-selected shared client strategy.

For login-flow architecture and PKCE/OIDC protocol design, also use `$identity-integration`. For backend controllers that must be exposed to frontend generation, also use `$asgard-api-development` and `$asgard-dotnet-10-csharp-14`.

## Hard Rules

| Area | Rule |
|------|------|
| API calls | Follow the API client strategy selected by the current project. If it uses TsGen, call generated `src/services/controller/*` methods and reuse `src/services/models/*` types; otherwise keep calls behind the project's shared request/client layer. |
| Generated code | When TsGen is enabled, treat generated `services/common`, `services/controller`, and `services/models` as pure generated output. Do not patch business behavior into generated files. |
| Request auth | All normal HTTP calls must go through the shared request instance that attaches Bearer tokens and handles 401 renew/login redirect. |
| Response handling | Unwrap Asgard `Response<T>`, `PageResponse<T>`, and `CursorResponse<T>` with shared helpers; do not parse `code/message/data` ad hoc in every page. |
| State ownership | Put reusable list/filter/pagination/load/mutation state in a DVA model. Keep pages focused on layout, table columns, forms, and user actions. |
| Permissions | Frontend permission checks only control visibility/UX. Backend authorization and tenant/resource-boundary checks remain mandatory. |
| Tenant context | Tenant pages must carry `tenantId` from the route and pass it explicitly through the selected API client when tenant scope is required. |
| Verification | Run `npm run typecheck` for TypeScript changes. Run `npm run lint` when touching many files or shared patterns. Run `npm run build` before claiming release readiness. |

## Standard Call Chain

When the project has selected TsGen, use this chain unless the endpoint is a special streaming or browser-native API:

```text
Page action
  -> dispatch DVA effect or call a local page async helper
  -> generated controller method from src/services/controller/*
  -> shared request instance
  -> Asgard unified response
  -> unwrapResponse / unwrapPageResponse / unwrapCursorResponse helper
  -> model state update or page-local UI state
```

If the project does not use TsGen, preserve the same boundaries with its shared API client. Do not mix generated and handwritten wrappers for the same endpoint without an explicit migration reason.

## Project Layout

| Path | Responsibility |
|------|----------------|
| `config/routes.ts` | Route tree, menu labels, icons, redirects, hidden workspace routes. |
| `src/app.tsx` | Global layout, initial user state, auth redirect guard, menu filtering, providers. |
| `src/pages/*` | Page shells, tables, drawers, modals, route params, UI interactions. |
| `src/models/*` | DVA namespaces for reusable state, effects, reducers, pagination, filters. |
| `src/services/request.ts` | Shared axios instance, API base URL, token injection, renew-on-401 behavior. |
| `src/services/oidc*.ts` | OIDC user manager, login callback/logout behavior, authority/client config. |
| `src/services/common/*` | Generated/shared response and request helpers. |
| `src/services/controller/*` | Generated API client methods. |
| `src/services/models/*` | Generated DTO/VO/request type definitions. |
| `src/utils/http.ts` | Unwrap and error-normalization helpers. |
| `src/utils/auth.ts` | Claim parsing and UI permission helpers. |

## Building Pages

Default CRUD page shape:

1. Add route in `config/routes.ts` with a stable `/dashboard/...` path, Chinese menu name, and Ant Design icon.
2. Import API methods and DTO/VO types from the project's selected client layer; use generated controller/model imports only when TsGen is enabled.
3. Put reusable list state in `src/models/{domain}.ts`: `list`, `pagination`, `filters`, `error`, effects for `fetch/submit/remove/changeStatus`.
4. Render with `PageContainer`, `ProTable<TVo>`, and `DrawerForm<TDto>` unless the workflow needs a dedicated detail or wizard page.
5. Use Ant Design `App.useApp()` for message/modal APIs.
6. Keep table renderers consistent through shared format helpers such as status tags, boolean tags, text fallback, and date formatting.
7. After mutations, refresh the list through the model effect; avoid duplicating list reload logic in multiple callbacks.

Tenant workspace pages follow the parent route `/dashboard/tenants/:tenantId` and should use child paths for tabs such as `detail`, `users`, `roles`, `permissions`, `clients`, `scopes`, `oidc-keys`, `authorizations`, and `security`.

## Optional Generated Client Contract

When the current project opts into TsGen, normalize generated-client calls to this shape:

```ts
const response = yield call([tenantController, tenantController.GetAll], {
  query: {
    page: state.pagination.current,
    size: state.pagination.pageSize,
    name: state.filters.name || undefined,
  },
});

const page = unwrapPageResponse<TenantInfoVo>(response);
```

Do not:

- Build URLs by string concatenation when a generated controller method exists.
- Re-declare DTO/VO interfaces in page files.
- Reach into `response.data.data` directly across pages.
- Ignore `PageResponse` metadata and hard-code table totals.
- Store token parsing or OIDC renew logic in page components.

## Backend Coordination

If the project has chosen TsGen and a controller is missing from generated clients, check these backend prerequisites:

- Controller is loaded by the running host/plugin.
- Controller is discovered by MVC.
- Controller is explicitly marked with `[AsgardTsGen]`.
- Return types use Asgard response wrappers.
- After route, parameter, DTO, or VO changes, rerun TsGen and update frontend imports.

## Review Checklist

- If the project uses TsGen, does each generated API call reuse `services/controller` and generated model types without a parallel handwritten wrapper?
- If the project does not use TsGen, are API calls still centralized behind the selected shared request/client layer?
- When TsGen is enabled, are generated directories kept free of handwritten business edits?
- Are response wrappers unwrapped through shared helpers?
- Is repeated list/pagination/filter behavior in a model instead of scattered page state?
- Are dangerous actions confirmed with `modal.confirm` and followed by a refresh?
- Are tenant-scoped calls passing the current route tenant id explicitly?
- Are UI permission checks mirrored by backend authorization?
- Did `npm run typecheck` pass after TypeScript changes?

## References

Read `references/heimdall-patterns.md` when implementing or reviewing concrete Heimdall-style pages, models, auth wiring, or TsGen troubleshooting.
