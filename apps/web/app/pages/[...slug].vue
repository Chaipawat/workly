<script setup lang="ts">
import { ArrowLeft, Construction } from '@lucide/vue'

definePageMeta({
  validate: route => [
    '/people',
    '/teams',
    '/projects',
    '/tasks',
    '/leave',
    '/announcements',
    '/settings/organization',
  ].includes(route.path),
})

const route = useRoute()

const pageName = computed(() => {
  const segments = Array.isArray(route.params.slug)
    ? route.params.slug
    : [route.params.slug]

  const lastSegment = segments.filter(Boolean).at(-1) ?? 'page'

  return lastSegment
    .split('-')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ')
})

useHead(() => ({ title: `${pageName.value} | Workly` }))
</script>

<template>
  <section class="card state-card" aria-labelledby="coming-soon-title">
    <div class="state-card__icon" aria-hidden="true">
      <Construction :size="30" />
    </div>
    <p class="eyebrow">Coming soon</p>
    <h1 id="coming-soon-title">{{ pageName }}</h1>
    <p>This area is planned for a future Workly milestone.</p>
    <NuxtLink to="/dashboard" class="button button--primary">
      <ArrowLeft :size="17" />
      Back to dashboard
    </NuxtLink>
  </section>
</template>
