<script setup lang="ts">
import { ChevronsUpDown } from '@lucide/vue'
</script>

<template>
  <button class="org-switcher" type="button" aria-label="Switch organization">
    <span class="org-switcher__mark" aria-hidden="true">A</span>
    <span class="org-switcher__copy"><strong>Acme Studio</strong><small>Organization</small></span>
    <ChevronsUpDown :size="16" aria-hidden="true" />
  </button>
</template>
