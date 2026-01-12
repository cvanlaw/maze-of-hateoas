<script setup lang="ts">
defineProps<{
  label: string;
  value: string | number;
  color?: 'blue' | 'green' | 'amber' | 'purple';
}>();
</script>

<template>
  <div class="bg-slate-800 rounded-lg p-4 border border-slate-700">
    <div class="text-slate-400 text-sm mb-1">{{ label }}</div>
    <div
      class="text-2xl font-bold"
      :class="{
        'text-blue-400': color === 'blue',
        'text-green-400': color === 'green',
        'text-amber-400': color === 'amber',
        'text-purple-400': color === 'purple',
        'text-white': !color
      }"
    >
      {{ value }}
    </div>
  </div>
</template>
