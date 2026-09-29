<script setup lang="ts">
import { ArrowRight } from '@lucide/vue'
import type { TeamMember } from '~/types/dashboard'
defineProps<{ members: TeamMember[] }>()
</script>

<template>
  <section class="card panel">
    <div class="panel__header"><div><h2>Team overview</h2><p>Availability at a glance</p></div><NuxtLink to="/people" class="text-link">People <ArrowRight :size="15" /></NuxtLink></div>
    <div class="team-grid">
      <article v-for="member in members" :key="member.id" class="team-member">
        <div class="avatar-presence"><AppAvatar :initials="member.initials" tone="slate" /><span :class="`presence-${member.status}`" :aria-label="member.status" /></div>
        <div><h3>{{ member.name }}</h3><p>{{ member.role }}</p></div>
      </article>
    </div>
  </section>
</template>
