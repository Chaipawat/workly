<script setup lang="ts">
import type { TaskStatus } from '~/types/dashboard'
import { getTaskTotal } from '~/utils/dashboard'

const props = defineProps<{ status: Record<TaskStatus, number> }>()
const items: { key: TaskStatus, label: string, tone: string }[] = [
  { key: 'todo', label: 'To do', tone: 'gray' },
  { key: 'in-progress', label: 'In progress', tone: 'blue' },
  { key: 'done', label: 'Done', tone: 'green' },
]
const total = computed(() => getTaskTotal(props.status))
</script>

<template>
  <section class="card panel task-status">
    <div class="panel__header"><div><h2>Task status</h2><p>Across active projects</p></div><strong class="panel-total">{{ total }}</strong></div>
    <div class="status-bar" aria-label="Task status distribution"><span v-for="item in items" :key="item.key" :class="`status-${item.tone}`" :style="{ width: `${(status[item.key] / Math.max(total, 1)) * 100}%` }" /></div>
    <div class="status-legend"><div v-for="item in items" :key="item.key"><span :class="`status-${item.tone}`" /><small>{{ item.label }}</small><strong>{{ status[item.key] }}</strong></div></div>
  </section>
</template>
