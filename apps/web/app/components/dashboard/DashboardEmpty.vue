<script setup lang="ts">
import { FolderKanban, Plus } from '@lucide/vue'
</script>

<template>
  <section class="card state-card">
    <span class="state-card__icon"><FolderKanban :size="28" /></span>
    <h2>Your workspace is ready</h2>
    <p>Invite teammates or create your first project to start seeing organization activity here.</p>
    <div class="state-card__actions"><button class="button button--primary" type="button"><Plus :size="17" /> Create project</button><button class="button button--secondary" type="button">Invite members</button></div>
  </section>
</template>
