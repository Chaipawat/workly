<script setup lang="ts">
import { CalendarDays, CheckSquare2, Palmtree } from '@lucide/vue'
import type { UpcomingItem } from '~/types/dashboard'
defineProps<{ items: UpcomingItem[] }>()
</script>

<template>
  <section class="card panel">
    <div class="panel__header"><div><h2>Upcoming</h2><p>From tasks and approved leave</p></div><CalendarDays :size="19" class="muted-icon" /></div>
    <div class="upcoming-list">
      <article v-for="item in items" :key="item.id" class="upcoming-row">
        <span class="upcoming-row__day">{{ item.day }}</span>
        <span class="upcoming-row__icon" :class="item.kind"><CheckSquare2 v-if="item.kind === 'task'" :size="16" /><Palmtree v-else :size="16" /></span>
        <div><h3>{{ item.title }}</h3><p>{{ item.detail }}</p></div>
      </article>
    </div>
  </section>
</template>
