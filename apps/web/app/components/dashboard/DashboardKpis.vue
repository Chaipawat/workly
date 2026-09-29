<script setup lang="ts">
import { Building2, FolderKanban, Palmtree, Users } from '@lucide/vue'
import type { DashboardMetric } from '~/types/dashboard'

defineProps<{ metrics: DashboardMetric[] }>()
const icons = { users: Users, departments: Building2, projects: FolderKanban, leave: Palmtree }
</script>

<template>
  <section class="kpi-grid" aria-label="Organization snapshot">
    <article v-for="metric in metrics" :key="metric.label" class="card kpi-card">
      <div class="kpi-card__icon" :class="`tone-${metric.tone}`"><component :is="icons[metric.icon]" :size="20" aria-hidden="true" /></div>
      <div><p>{{ metric.label }}</p><strong>{{ metric.value }}</strong><small>{{ metric.trend }}</small></div>
    </article>
  </section>
</template>
