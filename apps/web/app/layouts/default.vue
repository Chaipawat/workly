<script setup lang="ts">
const navigationOpen = ref(false)
</script>

<template>
  <div class="app-shell">
    <AppSidebar :open="navigationOpen" @close="navigationOpen = false" />
    <div class="app-shell__main">
      <AppTopbar @open-navigation="navigationOpen = true" />
      <main id="main-content" class="page-content"><slot /></main>
    </div>
  </div>
</template>
