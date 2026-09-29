<script setup lang="ts">
defineProps<{ label: string, tone: 'blue' | 'gray' | 'amber' | 'green' | 'red' }>()
</script>

<template>
  <span class="status-badge" :class="`status-badge--${tone}`">
    <span class="status-badge__dot" aria-hidden="true" />{{ label }}
  </span>
</template>
