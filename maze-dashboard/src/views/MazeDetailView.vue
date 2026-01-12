<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import MazeGrid from '../components/MazeGrid.vue';
import SessionTable from '../components/SessionTable.vue';
import { useSignalR } from '../composables/useSignalR';
import { fetchMazeMetrics } from '../services/api';
import type { MazeMetrics } from '../types';

const route = useRoute();
const router = useRouter();
const mazeId = computed(() => route.params.id as string);

const metrics = ref<MazeMetrics | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const showHeatmap = ref(false);
let refreshTimer: ReturnType<typeof setTimeout> | null = null;

const {
  isConnected,
  connect,
  subscribeToMaze,
  unsubscribeFromMaze,
  onSessionStarted,
  onSessionMoved,
  onSessionCompleted
} = useSignalR();

const heatmap = computed(() => {
  if (!metrics.value) return new Map<string, number>();
  const map = new Map<string, number>();
  for (const session of metrics.value.sessions) {
    const key = `${session.currentPosition.x},${session.currentPosition.y}`;
    map.set(key, (map.get(key) || 0) + 1);
  }
  return map;
});

async function loadData() {
  try {
    metrics.value = await fetchMazeMetrics(mazeId.value);
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load data';
  } finally {
    loading.value = false;
  }
}

function scheduleRefresh() {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
  // Shorter debounce for detail view - more responsive
  refreshTimer = setTimeout(() => {
    loadData();
  }, 500);
}

function goBack() {
  router.push('/');
}

onMounted(async () => {
  await loadData();
  await connect();
  await subscribeToMaze(mazeId.value);

  onSessionStarted.value = (event) => {
    if (event.mazeId === mazeId.value) scheduleRefresh();
  };

  onSessionMoved.value = (event) => {
    if (event.mazeId === mazeId.value) scheduleRefresh();
  };

  onSessionCompleted.value = (event) => {
    if (event.mazeId === mazeId.value) scheduleRefresh();
  };
});

onUnmounted(async () => {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
  await unsubscribeFromMaze(mazeId.value);
});
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <header class="border-b border-slate-700 px-6 py-4">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-4">
          <button
            @click="goBack"
            class="text-slate-400 hover:text-white transition-colors"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <h1 class="text-xl font-bold">
            Maze {{ mazeId.slice(0, 8) }}...
            <span v-if="metrics" class="text-slate-400 font-normal">
              ({{ metrics.width }}x{{ metrics.height }})
            </span>
          </h1>
        </div>
        <div class="flex items-center gap-2">
          <div
            class="w-2 h-2 rounded-full"
            :class="isConnected ? 'bg-green-500' : 'bg-red-500'"
          />
          <span class="text-sm text-slate-400">
            {{ isConnected ? 'Live' : 'Disconnected' }}
          </span>
        </div>
      </div>
    </header>

    <main class="p-6">
      <div v-if="loading" class="text-center py-12">
        <div class="text-slate-400">Loading...</div>
      </div>

      <div v-else-if="error" class="text-center py-12">
        <div class="text-red-400">{{ error }}</div>
      </div>

      <template v-else-if="metrics">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2">
            <div class="bg-slate-800 rounded-lg border border-slate-700 p-4">
              <div class="flex items-center justify-between mb-4">
                <h2 class="font-semibold">Maze Grid</h2>
                <label class="flex items-center gap-2 text-sm">
                  <input
                    v-model="showHeatmap"
                    type="checkbox"
                    class="rounded bg-slate-700 border-slate-600"
                  />
                  Show Heatmap
                </label>
              </div>
              <div class="flex justify-center overflow-auto">
                <MazeGrid
                  :cells="metrics.cells"
                  :width="metrics.width"
                  :height="metrics.height"
                  :sessions="metrics.sessions"
                  :heatmap="heatmap"
                  :show-heatmap="showHeatmap"
                />
              </div>
            </div>
          </div>

          <div>
            <SessionTable :sessions="metrics.sessions" />
          </div>
        </div>
      </template>
    </main>
  </div>
</template>
