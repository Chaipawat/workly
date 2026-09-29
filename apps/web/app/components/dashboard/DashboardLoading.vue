<template>
  <div aria-live="polite" aria-busy="true" aria-label="Loading dashboard">
    <span class="sr-only">Loading dashboard</span>
    <div class="skeleton skeleton--title" />
    <div class="skeleton-grid"><div v-for="n in 4" :key="n" class="card skeleton skeleton--kpi" /></div>
    <div class="dashboard-grid"><div class="card skeleton skeleton--panel" /><div class="card skeleton skeleton--panel" /><div class="card skeleton skeleton--panel" /></div>
  </div>
</template>
