<script setup lang="ts">
import { FolderKanban, Plus } from '@lucide/vue'
</script>

<template>
  <section class="card state-card">
    <span class="state-card__icon"><FolderKanban :size="28" /></span>
    <h2>Your workspace is ready</h2>
    <p>Add registered teammates or create your first project to start seeing organization activity here.</p>
    <div class="state-card__actions"><NuxtLink class="button button--primary" to="/projects"><Plus :size="17" /> Create project</NuxtLink><NuxtLink class="button button--secondary" to="/people">Add members</NuxtLink></div>
  </section>
</template>
