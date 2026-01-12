<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import MetricCard from '../components/MetricCard.vue';
import MazeList from '../components/MazeList.vue';
import { useSignalR } from '../composables/useSignalR';
import { fetchAggregateMetrics, fetchMazes } from '../services/api';
import type { AggregateMetrics, MazeSummary, SessionStartedEvent, SessionCompletedEvent } from '../types';

const router = useRouter();
const metrics = ref<AggregateMetrics | null>(null);
const mazes = ref<MazeSummary[]>([]);
const initialLoading = ref(true);
const refreshing = ref(false);
const error = ref<string | null>(null);

const {
  isConnected,
  connect,
  subscribeToAll,
  onSessionStarted,
  onSessionMoved,
  onSessionCompleted
} = useSignalR();

const sessionCounts = ref<Record<string, number>>({});
let refreshTimer: ReturnType<typeof setTimeout> | null = null;

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

function scheduleRefresh() {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
  refreshTimer = setTimeout(() => {
    loadData();
  }, 2000);
}

function handleMazeSelect(mazeId: string) {
  router.push(`/maze/${mazeId}`);
}

onMounted(async () => {
  await loadData(true);
  await connect();
  await subscribeToAll();

  onSessionStarted.value = (event: SessionStartedEvent) => {
    sessionCounts.value[event.mazeId] = (sessionCounts.value[event.mazeId] || 0) + 1;
    if (metrics.value) {
      metrics.value.activeSessions++;
    }
    scheduleRefresh();
  };

  onSessionMoved.value = () => {
    // No reload needed - aggregate view doesn't display session positions
  };

  onSessionCompleted.value = (event: SessionCompletedEvent) => {
    sessionCounts.value[event.mazeId] = Math.max(0, (sessionCounts.value[event.mazeId] || 1) - 1);
    if (metrics.value) {
      metrics.value.activeSessions = Math.max(0, metrics.value.activeSessions - 1);
      metrics.value.completedToday++;
    }
    scheduleRefresh();
  };
});

onUnmounted(() => {
  if (refreshTimer) {
    clearTimeout(refreshTimer);
  }
});
</script>

<template>
  <div class="min-h-screen bg-slate-900 text-white">
    <header class="border-b border-slate-700 px-6 py-4">
      <div class="flex items-center justify-between">
        <h1 class="text-xl font-bold">Maze Dashboard</h1>
        <div class="flex items-center gap-2">
          <div
            class="w-2 h-2 rounded-full"
            :class="isConnected ? 'bg-green-500' : 'bg-red-500'"
          />
          <span class="text-sm text-slate-400">
            {{ isConnected ? 'Live' : 'Disconnected' }}
          </span>
          <span v-if="refreshing" class="text-xs text-slate-500 ml-2">
            Syncing...
          </span>
        </div>
      </div>
    </header>

    <main class="p-6">
      <div v-if="initialLoading" class="text-center py-12">
        <div class="text-slate-400">Loading...</div>
      </div>

      <div v-else-if="error" class="text-center py-12">
        <div class="text-red-400">{{ error }}</div>
      </div>

      <template v-else-if="metrics">
        <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
          <MetricCard
            label="Active Sessions"
            :value="metrics.activeSessions"
            color="blue"
          />
          <MetricCard
            label="Completed Today"
            :value="metrics.completedToday"
            color="green"
          />
          <MetricCard
            label="Completion Rate"
            :value="`${metrics.completionRate}%`"
            color="green"
          />
          <MetricCard
            label="Avg Moves"
            :value="metrics.averageMoves"
          />
          <MetricCard
            label="Most Active"
            :value="metrics.mostActiveMazeSessionCount"
            color="purple"
          />
          <MetricCard
            label="System Velocity"
            :value="`${metrics.systemVelocity}/m`"
            color="amber"
          />
        </div>

        <MazeList
          :mazes="mazes"
          :session-counts="sessionCounts"
          :most-active-id="metrics.mostActiveMazeId"
          @select="handleMazeSelect"
        />
      </template>
    </main>
  </div>
</template>
