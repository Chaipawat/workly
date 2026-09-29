<script setup lang="ts">
import type { ActivityItem } from '~/types/dashboard'
defineProps<{ activities: ActivityItem[] }>()
</script>

<template>
  <section class="card panel activity-panel">
    <div class="panel__header"><div><h2>Recent activity</h2><p>Updates you have permission to see</p></div></div>
    <ol class="activity-list">
      <li v-for="activity in activities" :key="activity.id">
        <AppAvatar :initials="activity.initials" size="sm" :tone="activity.tone" />
        <div><p><strong>{{ activity.actor }}</strong> {{ activity.action }} <b>{{ activity.target }}</b></p><small>{{ activity.time }}</small></div>
      </li>
    </ol>
  </section>
</template>
