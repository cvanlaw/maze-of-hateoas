<script setup lang="ts">
import { computed } from 'vue';
import type { Cell, SessionSnapshot, Position } from '../types';

const props = defineProps<{
  cells: Cell[][];
  width: number;
  height: number;
  sessions: SessionSnapshot[];
  heatmap?: Map<string, number>;
  showHeatmap?: boolean;
}>();

const cellSize = 24;
const wallWidth = 2;

const svgWidth = computed(() => props.width * cellSize + wallWidth);
const svgHeight = computed(() => props.height * cellSize + wallWidth);

const sessionColors = [
  '#3b82f6', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#ec4899', '#06b6d4', '#84cc16'
];

function getCellAt(x: number, y: number): Cell | undefined {
  return props.cells[y]?.[x];
}

function getSessionColor(index: number): string {
  return sessionColors[index % sessionColors.length];
}

function getHeatmapOpacity(x: number, y: number): number {
  if (!props.heatmap || !props.showHeatmap) return 0;
  const count = props.heatmap.get(`${x},${y}`) || 0;
  const maxCount = Math.max(...props.heatmap.values(), 1);
  return count / maxCount * 0.6;
}
</script>

<template>
  <svg
    :width="svgWidth"
    :height="svgHeight"
    class="bg-slate-800 rounded"
  >
    <!-- Grid cells and heatmap -->
    <g v-for="x in width" :key="`col-${x}`">
      <g v-for="y in height" :key="`cell-${x}-${y}`">
        <!-- Heatmap background -->
        <rect
          v-if="showHeatmap"
          :x="(x - 1) * cellSize + wallWidth"
          :y="(y - 1) * cellSize + wallWidth"
          :width="cellSize - wallWidth"
          :height="cellSize - wallWidth"
          fill="#f59e0b"
          :fill-opacity="getHeatmapOpacity(x - 1, y - 1)"
        />

        <!-- Walls -->
        <template v-if="getCellAt(x - 1, y - 1)">
          <!-- North wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasNorthWall"
            :x1="(x - 1) * cellSize"
            :y1="(y - 1) * cellSize"
            :x2="x * cellSize"
            :y2="(y - 1) * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
          <!-- South wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasSouthWall"
            :x1="(x - 1) * cellSize"
            :y1="y * cellSize"
            :x2="x * cellSize"
            :y2="y * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
          <!-- East wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasEastWall"
            :x1="x * cellSize"
            :y1="(y - 1) * cellSize"
            :x2="x * cellSize"
            :y2="y * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
          <!-- West wall -->
          <line
            v-if="getCellAt(x - 1, y - 1)?.hasWestWall"
            :x1="(x - 1) * cellSize"
            :y1="(y - 1) * cellSize"
            :x2="(x - 1) * cellSize"
            :y2="y * cellSize"
            stroke="#64748b"
            :stroke-width="wallWidth"
          />
        </template>
      </g>
    </g>

    <!-- Start marker -->
    <text
      :x="cellSize / 2"
      :y="cellSize / 2 + 4"
      text-anchor="middle"
      class="text-xs fill-green-400 font-bold"
    >
      S
    </text>

    <!-- End marker -->
    <text
      :x="(width - 0.5) * cellSize"
      :y="(height - 0.5) * cellSize + 4"
      text-anchor="middle"
      class="text-xs fill-red-400 font-bold"
    >
      E
    </text>

    <!-- Session markers -->
    <circle
      v-for="(session, index) in sessions"
      :key="session.sessionId"
      :cx="session.currentPosition.x * cellSize + cellSize / 2"
      :cy="session.currentPosition.y * cellSize + cellSize / 2"
      r="6"
      :fill="getSessionColor(index)"
      class="transition-all duration-300"
    />
  </svg>
</template>
