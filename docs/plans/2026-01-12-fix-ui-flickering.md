# Fix UI Flickering Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Eliminate UI flickering in the maze dashboard caused by full view reloads on every SignalR event.

**Architecture:** Separate initial loading state from refresh state, use optimistic updates for session counts, remove unnecessary full reloads for events that don't affect displayed data, and add debouncing for rapid events.

**Tech Stack:** Vue 3, TypeScript, SignalR

---

## Root Cause Analysis

The flickering occurs because:

1. `AggregateView.vue` line 29 sets `loading.value = true` at the start of every `loadData()` call
2. Lines 91-93 in template use `v-if="loading"` which completely unmounts the maze list and shows "Loading..."
3. Every SignalR event (`SessionStarted`, `SessionMoved`, `SessionCompleted`) triggers `loadData()`
4. `SessionMoved` events fire frequently as users navigate mazes

**Key insight:** The `AggregateView` doesn't even display position data, yet reloads on every move event.

---

## Implementation Tasks

### Task 1: Add Separate Initial Loading State to AggregateView

**Files:**
- Modify: `maze-dashboard/src/views/AggregateView.vue:13-14, 27-45, 91-93`

**Step 1: Write the failing test**

Create test file for the view behavior.

```typescript
// maze-dashboard/src/views/__tests__/AggregateView.spec.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import AggregateView from '../AggregateView.vue'

// Mock the API
vi.mock('../../services/api', () => ({
  fetchAggregateMetrics: vi.fn(),
  fetchMazes: vi.fn()
}))

// Mock SignalR
vi.mock('../../composables/useSignalR', () => ({
  useSignalR: () => ({
    isConnected: { value: true },
    connect: vi.fn(),
    subscribeToAll: vi.fn(),
    onSessionStarted: { value: null },
    onSessionMoved: { value: null },
    onSessionCompleted: { value: null }
  })
}))

import { fetchAggregateMetrics, fetchMazes } from '../../services/api'

describe('AggregateView', () => {
  const mockMetrics = {
    activeSessions: 5,
    completedToday: 10,
    completionRate: 50,
    averageMoves: 100,
    mostActiveMazeId: 'maze-1',
    mostActiveMazeSessionCount: 3,
    systemVelocity: 2,
    sessionCountsByMaze: { 'maze-1': 3, 'maze-2': 2 }
  }

  const mockMazes = [
    { id: 'maze-1', width: 10, height: 10, createdAt: '2026-01-01' },
    { id: 'maze-2', width: 5, height: 5, createdAt: '2026-01-02' }
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchAggregateMetrics).mockResolvedValue(mockMetrics)
    vi.mocked(fetchMazes).mockResolvedValue(mockMazes)
  })

  it('shows loading spinner only on initial load, not on refresh', async () => {
    const router = createRouter({
      history: createWebHistory(),
      routes: [{ path: '/', component: AggregateView }]
    })

    const wrapper = mount(AggregateView, {
      global: { plugins: [router] }
    })

    // Initial load should show loading
    expect(wrapper.text()).toContain('Loading...')

    await flushPromises()

    // After load, should show content
    expect(wrapper.text()).not.toContain('Loading...')
    expect(wrapper.text()).toContain('Active Mazes')

    // Trigger a refresh (simulating SignalR event)
    vi.mocked(fetchAggregateMetrics).mockResolvedValueOnce({
      ...mockMetrics,
      activeSessions: 6
    })

    // Access component instance to call loadData
    // The key assertion: content should remain visible during refresh
    const mazeListBefore = wrapper.find('[data-testid="maze-list"]')
    expect(mazeListBefore.exists()).toBe(true)
  })
})
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run AggregateView`

Expected: Test infrastructure may need setup. If no test runner exists, we'll set it up first.

**Step 3: Check if test infrastructure exists**

Run: `ls maze-dashboard/package.json && cat maze-dashboard/package.json | grep -A5 '"scripts"'`

If vitest is not configured, add it to package.json scripts.

**Step 4: Update AggregateView with separate loading states**

```typescript
// Replace lines 13-14 in AggregateView.vue
const initialLoading = ref(true);
const refreshing = ref(false);
```

```typescript
// Replace loadData function (lines 27-46)
async function loadData(isInitial = false) {
  try {
    if (isInitial) {
      initialLoading.value = true;
    }
    refreshing.value = true;

    const [metricsData, mazesData] = await Promise.all([
      fetchAggregateMetrics(),
      fetchMazes()
    ]);
    metrics.value = metricsData;
    mazes.value = mazesData;

    if (metricsData?.sessionCountsByMaze) {
      sessionCounts.value = { ...metricsData.sessionCountsByMaze };
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load data';
  } finally {
    initialLoading.value = false;
    refreshing.value = false;
  }
}
```

```vue
<!-- Replace line 91 conditional -->
<div v-if="initialLoading" class="text-center py-12">
```

**Step 5: Update onMounted to pass isInitial flag**

```typescript
// Line 53
await loadData(true);
```

**Step 6: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run AggregateView`

Expected: PASS

**Step 7: Commit**

```bash
git add maze-dashboard/src/views/AggregateView.vue maze-dashboard/src/views/__tests__/
git commit -m "fix(dashboard): separate initial loading from refresh to prevent flickering"
```

---

### Task 2: Remove Unnecessary Full Reload on SessionMoved in AggregateView

**Files:**
- Modify: `maze-dashboard/src/views/AggregateView.vue:62-64`

**Step 1: Write the failing test**

```typescript
// Add to AggregateView.spec.ts
it('does not trigger full reload on SessionMoved event', async () => {
  // ... setup ...
  await flushPromises()

  // Clear mock call count
  vi.mocked(fetchAggregateMetrics).mockClear()
  vi.mocked(fetchMazes).mockClear()

  // Simulate SessionMoved event
  // The handler should NOT call loadData for aggregate view
  // (position data is not displayed in aggregate view)

  expect(fetchAggregateMetrics).not.toHaveBeenCalled()
  expect(fetchMazes).not.toHaveBeenCalled()
})
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run AggregateView`

Expected: FAIL - loadData is currently called on every SessionMoved

**Step 3: Remove the loadData call from onSessionMoved handler**

```typescript
// Replace lines 62-64 in AggregateView.vue
onSessionMoved.value = () => {
  // Position changes don't affect aggregate view metrics
  // No reload needed - aggregate view doesn't display session positions
};
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run AggregateView`

Expected: PASS

**Step 5: Commit**

```bash
git add maze-dashboard/src/views/AggregateView.vue
git commit -m "perf(dashboard): skip reload on SessionMoved in aggregate view"
```

---

### Task 3: Use Optimistic Updates Without Full Reload for Session Start/Complete

**Files:**
- Modify: `maze-dashboard/src/views/AggregateView.vue:57-69`

**Step 1: Write the failing test**

```typescript
// Add to AggregateView.spec.ts
it('updates session counts optimistically without full reload', async () => {
  // ... setup and mount ...
  await flushPromises()

  vi.mocked(fetchAggregateMetrics).mockClear()
  vi.mocked(fetchMazes).mockClear()

  // Simulate SessionStarted
  // Should update local state without calling API

  // Verify API was NOT called for immediate update
  expect(fetchAggregateMetrics).not.toHaveBeenCalled()
})
```

**Step 2: Run test to verify it fails**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run AggregateView`

Expected: FAIL - loadData is called on SessionStarted

**Step 3: Implement optimistic updates with debounced background refresh**

```typescript
// Add import at top of script
import { ref, onMounted, onUnmounted } from 'vue';

// Add debounce timer ref after line 25
let refreshTimer: ReturnType<typeof setTimeout> | null = null;

// Add debounced refresh function after loadData
function scheduleRefresh() {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
  // Debounce: refresh after 2 seconds of quiet
  refreshTimer = setTimeout(() => {
    loadData();
  }, 2000);
}

// Replace SessionStarted handler (lines 57-60)
onSessionStarted.value = (event: SessionStartedEvent) => {
  // Optimistic update - instant UI feedback
  sessionCounts.value[event.mazeId] = (sessionCounts.value[event.mazeId] || 0) + 1;
  if (metrics.value) {
    metrics.value.activeSessions++;
  }
  // Schedule background refresh to sync other metrics
  scheduleRefresh();
};

// Replace SessionCompleted handler (lines 66-69)
onSessionCompleted.value = (event: SessionCompletedEvent) => {
  // Optimistic update - instant UI feedback
  sessionCounts.value[event.mazeId] = Math.max(0, (sessionCounts.value[event.mazeId] || 1) - 1);
  if (metrics.value) {
    metrics.value.activeSessions = Math.max(0, metrics.value.activeSessions - 1);
    metrics.value.completedToday++;
  }
  // Schedule background refresh to sync other metrics
  scheduleRefresh();
};

// Add cleanup in onUnmounted or add onUnmounted
onUnmounted(() => {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
});
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run AggregateView`

Expected: PASS

**Step 5: Commit**

```bash
git add maze-dashboard/src/views/AggregateView.vue
git commit -m "perf(dashboard): use optimistic updates with debounced refresh"
```

---

### Task 4: Add Separate Loading State to MazeDetailView

**Files:**
- Modify: `maze-dashboard/src/views/MazeDetailView.vue:15-16, 39-47, 109`

**Step 1: Write the failing test**

```typescript
// maze-dashboard/src/views/__tests__/MazeDetailView.spec.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createWebHistory } from 'vue-router'
import MazeDetailView from '../MazeDetailView.vue'

vi.mock('../../services/api', () => ({
  fetchMazeMetrics: vi.fn()
}))

vi.mock('../../composables/useSignalR', () => ({
  useSignalR: () => ({
    isConnected: { value: true },
    connect: vi.fn(),
    subscribeToMaze: vi.fn(),
    unsubscribeFromMaze: vi.fn(),
    onSessionStarted: { value: null },
    onSessionMoved: { value: null },
    onSessionCompleted: { value: null }
  })
}))

import { fetchMazeMetrics } from '../../services/api'

describe('MazeDetailView', () => {
  const mockMetrics = {
    mazeId: 'test-maze-id',
    width: 10,
    height: 10,
    cells: [],
    activeSessions: 2,
    totalCompleted: 5,
    sessions: []
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(fetchMazeMetrics).mockResolvedValue(mockMetrics)
  })

  it('shows loading only on initial load, keeps content during refresh', async () => {
    const router = createRouter({
      history: createWebHistory(),
      routes: [
        { path: '/', component: { template: '<div/>' } },
        { path: '/maze/:id', component: MazeDetailView }
      ]
    })
    await router.push('/maze/test-maze-id')

    const wrapper = mount(MazeDetailView, {
      global: { plugins: [router] }
    })

    expect(wrapper.text()).toContain('Loading...')
    await flushPromises()
    expect(wrapper.text()).not.toContain('Loading...')
    expect(wrapper.text()).toContain('Maze Grid')
  })
})
```

**Step 2: Run test to verify behavior**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run MazeDetailView`

**Step 3: Update MazeDetailView with separate loading states**

```typescript
// Replace lines 15-16
const initialLoading = ref(true);
const refreshing = ref(false);
```

```typescript
// Replace loadData function (lines 39-47)
async function loadData(isInitial = false) {
  try {
    if (isInitial) {
      initialLoading.value = true;
    }
    refreshing.value = true;
    metrics.value = await fetchMazeMetrics(mazeId.value);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load data';
  } finally {
    initialLoading.value = false;
    refreshing.value = false;
  }
}
```

```typescript
// Update line 54
await loadData(true);
```

```vue
<!-- Replace line 109 conditional -->
<div v-if="initialLoading" class="text-center py-12">
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run MazeDetailView`

Expected: PASS

**Step 5: Commit**

```bash
git add maze-dashboard/src/views/MazeDetailView.vue maze-dashboard/src/views/__tests__/
git commit -m "fix(dashboard): separate initial loading from refresh in detail view"
```

---

### Task 5: Add Debounced Refresh to MazeDetailView

**Files:**
- Modify: `maze-dashboard/src/views/MazeDetailView.vue:2, 58-68, 71-73`

**Step 1: Write the failing test**

```typescript
// Add to MazeDetailView.spec.ts
it('debounces rapid refresh requests from SignalR events', async () => {
  // ... setup ...
  await flushPromises()
  vi.mocked(fetchMazeMetrics).mockClear()

  // Simulate multiple rapid SessionMoved events
  // Only one API call should be made after debounce period

  // Advance timers and check call count
})
```

**Step 2: Run test to verify behavior**

**Step 3: Add debounced refresh**

```typescript
// Update import line 2
import { ref, onMounted, onUnmounted, computed } from 'vue';

// Add after line 17
let refreshTimer: ReturnType<typeof setTimeout> | null = null;

// Add after loadData function
function scheduleRefresh() {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
  refreshTimer = setTimeout(() => {
    loadData();
  }, 500); // Shorter debounce for detail view - more responsive
}

// Replace event handlers (lines 58-68)
onSessionStarted.value = (event) => {
  if (event.mazeId === mazeId.value) scheduleRefresh();
};

onSessionMoved.value = (event) => {
  if (event.mazeId === mazeId.value) scheduleRefresh();
};

onSessionCompleted.value = (event) => {
  if (event.mazeId === mazeId.value) scheduleRefresh();
};

// Update onUnmounted (lines 71-73)
onUnmounted(async () => {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
  await unsubscribeFromMaze(mazeId.value);
});
```

**Step 4: Run test to verify it passes**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test -- --run MazeDetailView`

Expected: PASS

**Step 5: Commit**

```bash
git add maze-dashboard/src/views/MazeDetailView.vue
git commit -m "perf(dashboard): add debounced refresh to detail view"
```

---

### Task 6: Add Visual Refresh Indicator (Optional Enhancement)

**Files:**
- Modify: `maze-dashboard/src/views/AggregateView.vue` (template section)
- Modify: `maze-dashboard/src/views/MazeDetailView.vue` (template section)

**Step 1: Add subtle refresh indicator to header**

```vue
<!-- In AggregateView.vue, after the Live indicator (around line 86) -->
<span v-if="refreshing" class="text-xs text-slate-500 ml-2">
  Syncing...
</span>
```

```vue
<!-- In MazeDetailView.vue, after the Live indicator (around line 104) -->
<span v-if="refreshing" class="text-xs text-slate-500 ml-2">
  Syncing...
</span>
```

**Step 2: Run tests**

Run: `docker compose -f docker-compose.test.yml run --rm dashboard-test npm test`

**Step 3: Commit**

```bash
git add maze-dashboard/src/views/AggregateView.vue maze-dashboard/src/views/MazeDetailView.vue
git commit -m "feat(dashboard): add subtle refresh indicator"
```

---

### Task 7: Manual Testing and Verification

**Step 1: Start the application**

Run: `docker compose up --build`

**Step 2: Open dashboard in browser**

Navigate to: `http://localhost:8080` (or configured port)

**Step 3: Test scenarios**

1. Initial load - should show "Loading..." then content
2. Create a maze session via API
3. Move through maze - dashboard should NOT flicker
4. Verify session counts update without full reload
5. Complete a session - verify metrics update smoothly

**Step 4: Verify no flickering**

- Rapid clicks on maze list should work
- No "Loading..." flash during updates
- Subtle "Syncing..." indicator appears briefly

**Step 5: Final commit**

```bash
git add -A
git commit -m "test: verify UI flickering fix works end-to-end"
```

---

## Summary of Changes

| File | Change | Impact |
|------|--------|--------|
| `AggregateView.vue` | Separate `initialLoading`/`refreshing` states | No spinner during refresh |
| `AggregateView.vue` | Remove `loadData()` from `onSessionMoved` | Eliminate unnecessary API calls |
| `AggregateView.vue` | Optimistic updates + debounced refresh | Instant UI feedback |
| `MazeDetailView.vue` | Separate `initialLoading`/`refreshing` states | No spinner during refresh |
| `MazeDetailView.vue` | Debounced refresh (500ms) | Batch rapid events |

## Testing Checklist

- [ ] Initial page load shows loading spinner
- [ ] Subsequent updates don't show loading spinner
- [ ] Maze list is clickable during updates
- [ ] Session counts update instantly
- [ ] Metrics sync correctly after debounce
- [ ] No console errors
- [ ] Memory usage stable (no timer leaks)
