<script setup lang="ts">
import { ChevronsUpDown } from '@lucide/vue'

const { organizations, currentOrganization, currentOrganizationId, selectOrganization } = useWorkspace()

const selected = computed({
  get: () => currentOrganizationId.value ?? '',
  set: value => selectOrganization(value),
})
</script>

<template>
  <label class="org-switcher">
    <span class="org-switcher__mark" aria-hidden="true">{{ currentOrganization?.name.charAt(0).toUpperCase() || 'W' }}</span>
    <span class="org-switcher__copy"><strong>{{ currentOrganization?.name || 'Workspace' }}</strong><small>{{ currentOrganization?.role || 'Organization' }}</small></span>
    <select v-model="selected" aria-label="Switch organization">
      <option v-for="organization in organizations" :key="organization.id" :value="organization.id">{{ organization.name }}</option>
    </select>
    <ChevronsUpDown :size="16" aria-hidden="true" />
  </label>
</template>
