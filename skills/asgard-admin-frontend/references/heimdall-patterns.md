# Heimdall Frontend Patterns

## Current Reference Stack

Use the Heimdall frontend as the reference implementation:

- `@umijs/max` for app framework, routes, layout, DVA, model, initial state, and access support.
- Ant Design and `@ant-design/pro-components` for operational admin UI.
- `axios` through the shared request instance.
- `oidc-client-ts` and `react-oidc-context` for browser OIDC session handling.
- An explicit project-selected API client strategy; Asgard.TsGen output under `src/services` is supported but optional.

Expected scripts:

```json
{
  "dev": "set PORT=3000&& max dev",
  "build": "max build",
  "lint": "eslint . --ext .ts,.tsx",
  "typecheck": "tsc --noEmit"
}
```

## API Client Pattern

Generated controllers are imported as default controller objects:

```ts
import tenantController from '@/services/controller/TenantController';
import type { TenantInfoDto, TenantInfoVo } from '@/services/models/TenantController';
```

Call generated methods with their structured parameter object:

```ts
yield call([tenantController, tenantController.Update], {
  path: { id: payload.id },
  body: payload.values,
});
```

Typical parameter keys are:

- `path` for route parameters.
- `query` for query string parameters.
- `body` for JSON request bodies.
- `form` for generated form-data uploads.

## Response Helpers

Use shared helpers to normalize Asgard responses:

```ts
import { getErrorMessage, unwrapPageResponse, unwrapResponse } from '@/utils/http';
```

Expected response shapes:

- `ResponseBase<T>`: `{ code, message, data }`
- `PageResponseBase<T>`: `{ code, message, data, dataCount, totalCount, page, size, totalPages }`
- `CursorResponseBase<T>`: `{ code, message, data, dataCount, hasMore, nextCursor, lastId }`

If a cursor helper is missing in a project, add a shared `unwrapCursorResponse` helper before introducing page-local parsing.

## DVA Model Pattern

For reusable CRUD screens, create a model with:

- `namespace`
- typed `State`
- `list`
- `pagination`
- `filters`
- optional `error`
- `fetch`, `submit`, `remove`, and status/change effects
- `save`, `setFilters`, and `setPagination` reducers

Keep API calls in effects when more than one component needs the data or when table state should survive page rerenders. Page-local async helpers are acceptable for highly local modal-only operations.

## Page Pattern

Use this default page structure for operational tables:

```tsx
function DomainPage({ dispatch, domain, loading, submitting }: DomainPageProps) {
  const { message, modal } = App.useApp();
  const [open, setOpen] = useState(false);
  const [editingRecord, setEditingRecord] = useState<DomainVo | undefined>();

  useEffect(() => {
    dispatch({ type: 'domain/fetch' });
  }, [dispatch, domain.filters.name, domain.pagination.current, domain.pagination.pageSize]);

  const columns: ProColumns<DomainVo>[] = [
    { title: '名称', dataIndex: 'name', render: (_, record) => renderText(record.name) },
  ];

  return (
    <PageContainer header={{ title: '领域名称' }}>
      <ProTable<DomainVo>
        rowKey="id"
        columns={columns}
        dataSource={domain.list}
        loading={loading}
        search={false}
      />
      <DrawerForm<DomainDto>
        open={open}
        loading={submitting}
        onOpenChange={setOpen}
      >
        <ProFormText name="name" label="名称" rules={[{ required: true, message: '请输入名称' }]} />
      </DrawerForm>
    </PageContainer>
  );
}
```

Use compact, operational screens. Avoid marketing-style hero layouts for admin consoles.

## Tenant Workspace Pattern

Tenant workspace pages should:

- Read `tenantId` from route params.
- Refuse to load scoped data when `tenantId` is missing.
- Pass `tenantId` explicitly in `path`, `query`, or `body` according to the generated controller signature.
- Keep workspace navigation under `/dashboard/tenants/:tenantId/...`.
- Hide workspace pages from the top-level menu with `hideInMenu: true` on the parent route.

## Auth And Permissions

Global app setup should:

- Load the current OIDC user in `getInitialState`.
- Wrap the root with Ant Design `ConfigProvider`, `App`, and the auth provider.
- Redirect unauthenticated users away from non-public routes.
- Attach access tokens in the shared request interceptor.
- Attempt token renew once on 401 before redirecting to login.

UI permission helpers should read Asgard-compatible `permissions` and `roles` claims from the user profile or access token. For UI gating, use helpers such as `hasAnyPermission(user, ['platform.admin'])`.

Do not put backend trust decisions in frontend code. Frontend checks hide or disable UI affordances; backend AsgardAuth and resource-boundary checks enforce security.

## Optional TsGen Troubleshooting

Only use this checklist when the project has opted into TsGen and generation is incomplete:

1. Is the target controller marked `[AsgardTsGen]`?
2. Is the plugin/assembly loaded by the current host?
3. Did MVC discover the controller?
4. Does the controller return `Response<T>`, `PageResponse<T>`, `CursorResponse<T>`, or supported stream/file contracts?
5. Was TsGen rerun after the backend change?
6. Were stale frontend imports removed after regeneration?

Do not silently mix a manual wrapper with TsGen output for the same endpoint. Fix generation, explicitly exclude the endpoint, or deliberately migrate the project to another shared client strategy.
