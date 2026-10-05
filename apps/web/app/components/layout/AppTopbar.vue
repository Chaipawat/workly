<script setup lang="ts">
import { Bell, LogOut, Menu, Search } from '@lucide/vue'
import { getInitials } from '~/utils/auth'

defineEmits<{ openNavigation: [] }>()

const auth = useAuth()
const { user } = auth
const loggingOut = ref(false)

const signOut = async () => {
  loggingOut.value = true
  try {
    await auth.logout()
    await navigateTo('/login')
  }
  finally {
    loggingOut.value = false
  }
}
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
      <div class="user-menu">
        <AppAvatar :initials="getInitials(user?.displayName)" size="sm" tone="indigo" />
        <span class="user-menu__copy"><strong>{{ user?.displayName }}</strong><small>{{ user?.email }}</small></span>
        <button class="icon-button user-menu__logout" type="button" :disabled="loggingOut" aria-label="Sign out" @click="signOut"><LogOut :size="17" /></button>
      </div>
    </div>
  </header>
</template>
