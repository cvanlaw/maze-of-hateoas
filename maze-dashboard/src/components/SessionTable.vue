<script setup lang="ts">
import type { SessionSnapshot } from '../types';

defineProps<{
  sessions: SessionSnapshot[];
}>();

const sessionColors = [
  'bg-blue-500', 'bg-green-500', 'bg-amber-500', 'bg-red-500',
  'bg-purple-500', 'bg-pink-500', 'bg-cyan-500', 'bg-lime-500'
];

function getSessionColor(index: number): string {
  return sessionColors[index % sessionColors.length] ?? 'bg-blue-500';
}
</script>

<template>
  <div class="bg-slate-800 rounded-lg border border-slate-700">
    <div class="p-4 border-b border-slate-700">
      <h3 class="font-semibold text-white">
        Sessions ({{ sessions.length }} active)
      </h3>
    </div>
    <div class="max-h-64 overflow-y-auto">
      <div
        v-for="(session, index) in sessions"
        :key="session.sessionId"
        class="px-4 py-2 border-b border-slate-700/50 flex items-center gap-3"
      >
        <div
          class="w-3 h-3 rounded-full"
          :class="getSessionColor(index)"
        />
        <div class="flex-1 min-w-0">
          <div class="text-sm text-white font-mono truncate">
            {{ session.sessionId.slice(0, 8) }}...
          </div>
        </div>
        <div class="text-sm text-slate-400">
          {{ session.completionPercent }}%
        </div>
        <div class="text-sm text-amber-400">
          {{ session.velocity }}/m
        </div>
      </div>
      <div
        v-if="sessions.length === 0"
        class="p-4 text-slate-400 text-center text-sm"
      >
        No active sessions
      </div>
    </div>
  </div>
</template>
