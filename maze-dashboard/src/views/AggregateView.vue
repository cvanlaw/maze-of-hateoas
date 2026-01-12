<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import MetricCard from '../components/MetricCard.vue';
import MazeList from '../components/MazeList.vue';
import { useSignalR } from '../composables/useSignalR';
import { fetchAggregateMetrics, fetchMazes } from '../services/api';
import type { AggregateMetrics, MazeSummary } from '../types';

const router = useRouter();
const metrics = ref<AggregateMetrics | null>(null);
const mazes = ref<MazeSummary[]>([]);
const loading = ref(true);
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

async function loadData() {
  try {
    loading.value = true;
    const [metricsData, mazesData] = await Promise.all([
      fetchAggregateMetrics(),
      fetchMazes()
    ]);
    metrics.value = metricsData;
    mazes.value = mazesData;
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load data';
  } finally {
    loading.value = false;
  }
}

function handleMazeSelect(mazeId: string) {
  router.push(`/maze/${mazeId}`);
}

onMounted(async () => {
  await loadData();
  await connect();
  await subscribeToAll();

  onSessionStarted.value = () => {
    loadData();
  };

  onSessionMoved.value = () => {
    loadData();
  };

  onSessionCompleted.value = () => {
    loadData();
  };
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
