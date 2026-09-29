<script setup lang="ts">
import { Bell, ChevronDown, Menu, Search } from '@lucide/vue'

defineEmits<{ openNavigation: [] }>()
</script>

<template>
  <header class="topbar">
    <button class="icon-button topbar__menu" type="button" aria-label="Open navigation" aria-controls="primary-navigation" @click="$emit('openNavigation')"><Menu :size="21" /></button>
    <div class="topbar__mobile-brand">Workly</div>
    <label class="global-search">
      <Search :size="18" aria-hidden="true" />
      <span class="sr-only">Search workspace</span>
      <input type="search" placeholder="Search people, projects, and tasks" disabled aria-describedby="search-hint">
      <kbd>⌘ K</kbd>
    </label>
    <span id="search-hint" class="sr-only">Global search is planned for a later milestone.</span>
    <div class="topbar__actions">
      <button class="icon-button" type="button" aria-label="Notifications, coming in a later milestone" disabled><Bell :size="20" /></button>
      <button class="user-menu" type="button" aria-label="Open user menu">
        <AppAvatar initials="RT" size="sm" tone="indigo" />
        <span class="user-menu__copy"><strong>Ryu Tanaka</strong><small>Owner</small></span>
        <ChevronDown :size="16" aria-hidden="true" />
      </button>
    </div>
  </header>
</template>
