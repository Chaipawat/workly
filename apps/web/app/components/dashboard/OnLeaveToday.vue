<script setup lang="ts">
import type { LeaveTodayItem } from '~/types/dashboard'
defineProps<{ people: LeaveTodayItem[] }>()
</script>

<template>
  <section class="card panel">
    <div class="panel__header">
      <div>
        <h2>On leave today</h2>
        <p>Approved absences</p>
      </div><span class="count-badge">{{ people.length }}</span>
    </div>
    <div class="people-list">
      <article v-for="person in people" :key="person.id" class="person-row">
        <AppAvatar :initials="person.initials" :tone="person.type === 'annual' ? 'blue' : 'rose'" />
        <div class="person-row__copy">
          <h3>{{ person.name }}</h3>
          <p>{{ person.role }}</p>
        </div>
        <div class="person-row__leave">
          <StatusBadge
            :label="person.type === 'annual' ? 'Annual leave' : 'Sick leave'"
            :tone="person.type === 'annual' ? 'blue' : 'red'" /><small>{{ person.returnLabel }}</small>
        </div>
      </article>
    </div>
  </section>
</template>
