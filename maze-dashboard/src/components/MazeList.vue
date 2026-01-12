<script setup lang="ts">
import type { MazeSummary } from '../types';

defineProps<{
  mazes: MazeSummary[];
  sessionCounts: Record<string, number>;
  mostActiveId?: string | null;
}>();

defineEmits<{
  select: [mazeId: string];
}>();
</script>

<template>
  <div class="bg-slate-800 rounded-lg border border-slate-700">
    <div class="p-4 border-b border-slate-700">
      <h2 class="text-lg font-semibold text-white">Active Mazes</h2>
    </div>
    <div class="divide-y divide-slate-700">
      <div
        v-for="maze in mazes"
        :key="maze.id"
        class="p-4 flex items-center justify-between hover:bg-slate-700/50 cursor-pointer transition-colors"
        @click="$emit('select', maze.id)"
      >
        <div>
          <div class="text-white font-medium">
            Maze {{ maze.id.slice(0, 8) }}...
            <span
              v-if="maze.id === mostActiveId"
              class="ml-2 text-xs bg-amber-500/20 text-amber-400 px-2 py-0.5 rounded"
            >
              Most Active
            </span>
          </div>
          <div class="text-slate-400 text-sm">
            {{ maze.width }}x{{ maze.height }}
          </div>
        </div>
        <div class="flex items-center gap-4">
          <div class="text-slate-400">
            {{ sessionCounts[maze.id] || 0 }} sessions
          </div>
          <svg
            class="w-5 h-5 text-slate-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 5l7 7-7 7"
            />
          </svg>
        </div>
      </div>
      <div
        v-if="mazes.length === 0"
        class="p-4 text-slate-400 text-center"
      >
        No mazes available
      </div>
    </div>
  </div>
</template>
